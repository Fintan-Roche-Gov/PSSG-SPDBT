using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Resource.Repository.PersonLicApplication;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.BizLicApplication;
internal class BizLicApplicationRepository : IBizLicApplicationRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;

    public BizLicApplicationRepository(IDataverseContextFactory ctx, IMapper mapper)
    {
        _context = ctx.Create();
        _mapper = mapper;
    }

    public async Task<BizLicApplicationCmdResp> CreateBizLicApplicationAsync(CreateBizLicApplicationCmd cmd, CancellationToken ct)
    {
        if (cmd.ApplicationTypeCode == ApplicationTypeEnum.New)
            throw new ArgumentException("New application type is not supported for business licence.");

        if (cmd.OriginalApplicationId == null)
            throw new ArgumentException("For replace, renew or update, original licence id cannot be null.");

        Spd_Application? originalApp = _context.Spd_ApplicationSet
            .Where(a => a.Spd_ApplicationId == cmd.OriginalApplicationId)
            .FirstOrDefault();

        if (originalApp == null)
            throw new ArgumentException("Original business licence application was not found.");

        if (originalApp.Spd_ApplicantId == null)
            throw new ArgumentException("There is no account linked to the application found.");

        Spd_Application app = _mapper.Map<Spd_Application>(cmd);
        app.StatusCode = Spd_Application_StatusCode.Incomplete;
        app.Spd_BusinessType = originalApp.Spd_BusinessType;

        _context.AddObject(app);

        LinkLicence(cmd.OriginalLicenceId, app);

        Guid applicantId = originalApp.Spd_ApplicantId.Id;
        Account? biz = GetOrgById(applicantId);
        if (biz == null || biz.StateCode == Account_StateCode.Inactive)
            throw new ApiException(HttpStatusCode.NotFound);

        await SetInfoFromBiz(biz, app, cmd.ApplicantIsBizManager ?? false, ct);
        LinkTeam(DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        await SetApplicantSwlLicenceId(app, cmd.ApplicantSwlLicenceId, ct);
        LinkServiceType(cmd.ServiceTypeCode, app);
        LinkSubmittedByPortalUser(cmd.SubmittedByPortalUserId, app);

        if (cmd.CategoryCodes.Any(c => c == WorkerCategoryTypeEnum.PrivateInvestigator) &&
            cmd.PrivateInvestigatorSwlInfo?.ContactId != null &&
            cmd.PrivateInvestigatorSwlInfo?.LicenceId != null)
        {
            Spd_BusinessContact businessContact = await UpsertPrivateInvestigator(cmd.PrivateInvestigatorSwlInfo, app, ct);
            businessContact.Spd_OrganizationId = biz.ToEntityReference();
            _context.UpdateObject(businessContact);
        }
        else
        {
            DeletePrivateInvestigatorLink(app);
        }

        _context.SaveChanges();
        ProcessCategories(cmd.CategoryCodes, app);
        _context.SaveChanges();

        return await Task.FromResult(new BizLicApplicationCmdResp(app.Spd_ApplicationId ?? Guid.Empty, applicantId));
    }

    public async Task<BizLicApplicationCmdResp> SaveBizLicApplicationAsync(SaveBizLicApplicationCmd cmd, CancellationToken ct)
    {
        Spd_Application app;
        if (cmd.LicenceAppId != null)
        {
            app = _context.Spd_ApplicationSet
                .Where(a => a.Spd_ApplicationId == cmd.LicenceAppId)
                .Where(a => a.StateCode != Spd_Application_StateCode.Inactive)
                .FirstOrDefault() ?? throw new ArgumentException("Application Id was not found.");

            _context.LoadProperty(app, nameof(Spd_Application.Spd_Application_Spd_LicenceCategory));
            _mapper.Map(cmd, app);
            app.Spd_ApplicationId = cmd.LicenceAppId;
            _context.UpdateObject(app);
        }
        else
        {
            app = _mapper.Map<Spd_Application>(cmd);
            _context.AddObject(app);
        }

        _context.SaveChanges();

        Account? biz = GetOrgById(cmd.ApplicantId);
        if (biz == null || biz.StateCode == Account_StateCode.Inactive)
            throw new ApiException(HttpStatusCode.NotFound);

        await SetInfoFromBiz(biz, app, cmd.ApplicantIsBizManager ?? false, ct);
        LinkTeam(DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        LinkServiceType(cmd.ServiceTypeCode, app);

        if (cmd.HasExpiredLicence == true && cmd.ExpiredLicenceId != null)
            LinkLicence(cmd.ExpiredLicenceId, app);
        else
            app.Spd_CurrentExpiredLicenceId = null;

        await SetApplicantSwlLicenceId(app, cmd.ApplicantSwlLicenceId, ct);
        LinkSubmittedByPortalUser(cmd.SubmittedByPortalUserId, app);

        if (cmd.CategoryCodes.Any(c => c == WorkerCategoryTypeEnum.PrivateInvestigator) &&
            cmd.PrivateInvestigatorSwlInfo?.ContactId != null &&
            cmd.PrivateInvestigatorSwlInfo?.LicenceId != null)
        {
            Spd_BusinessContact businessContact = await UpsertPrivateInvestigator(cmd.PrivateInvestigatorSwlInfo, app, ct);
            businessContact.Spd_OrganizationId = biz.ToEntityReference();
            _context.UpdateObject(businessContact);
        }
        else
        {
            DeletePrivateInvestigatorLink(app);
        }

        _context.SaveChanges();
        ProcessCategories(cmd.CategoryCodes, app);
        _context.SaveChanges();

        return await Task.FromResult(new BizLicApplicationCmdResp(app.Spd_ApplicationId ?? Guid.Empty, cmd.ApplicantId));
    }

    public async Task<BizLicApplicationResp> GetBizLicApplicationAsync(Guid licenceApplicationId, CancellationToken ct)
    {
        Spd_Application? app = _context.Spd_ApplicationSet
            .Where(a => a.Spd_ApplicationId == licenceApplicationId)
            .FirstOrDefault();

        if (app == null)
            throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the application for application id = {licenceApplicationId} ");

        _context.LoadProperty(app, nameof(Spd_Application.Spd_Account_Spd_Application_ApplicantId));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Contact_Spd_Application_ApplicantId));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Application_Spd_LicenceCategory));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Licence_Spd_Application_CurrentExpired));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_BusinessContact_Spd_Application));
        _context.LoadProperty(app, nameof(Spd_Application.ReferencedSpd_BusinessApplication_Spd_WorkerApplication));

        BizLicApplicationResp response = _mapper.Map<BizLicApplicationResp>(app);

        Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.PositionDictionary.TryGetValue(PositionEnum.PrivateInvestigatorManager.ToString(), out Guid positionId);

        List<Spd_BusinessContact> bizContacts = _context.Spd_BusinessContactSet
            .Where(b => b.StateCode != Spd_BusinessContact_StateCode.Inactive)
            .ToList();

        foreach (Spd_BusinessContact item in bizContacts)
        {
            _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_Position_Spd_BusinessContact));
            _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
        }

        Spd_BusinessContact? bizContact = bizContacts
            .Where(b => b.Spd_Position_Spd_BusinessContact.Any(p => p.Spd_PositionId == positionId))
            .Where(b => b.Spd_BusinessContact_Spd_Application.Any(a => a.Spd_ApplicationId == app.Spd_ApplicationId))
            .FirstOrDefault();

        if (bizContact != null)
        {
            response.PrivateInvestigatorSwlInfo = new PrivateInvestigatorSwlContactInfo
            {
                ContactId = bizContact.Spd_ContactId?.Id,
                BizContactId = bizContact.Spd_BusinessContactId,
                GivenName = bizContact.Spd_FirstName,
                Surname = bizContact.Spd_Surname,
                MiddleName1 = bizContact.Spd_MiddleName1,
                MiddleName2 = bizContact.Spd_MiddleName2,
                EmailAddress = bizContact.Spd_Email,
                LicenceId = bizContact.Spd_SwLNumber?.Id
            };
        }

        return await Task.FromResult(response);
    }

    private async Task<Spd_BusinessContact> UpsertPrivateInvestigator(PrivateInvestigatorSwlContactInfo privateInvestigatorInfo, Spd_Application app, CancellationToken ct)
    {
        Utilities.Dataverse.EarlyBound.Contact? contact = _context.ContactSet.Where(c => c.ContactId == privateInvestigatorInfo.ContactId).FirstOrDefault();
        if (contact == null)
            throw new ArgumentException($"cannot find the contact with contactId : {privateInvestigatorInfo.ContactId}");

        privateInvestigatorInfo.Surname = contact.LastName;
        privateInvestigatorInfo.GivenName = contact.FirstName;
        privateInvestigatorInfo.MiddleName1 = contact.Spd_MiddleName1;
        privateInvestigatorInfo.MiddleName2 = contact.Spd_MiddleName2;

        Spd_BusinessContact? bizContact = null;
        Guid? bizContactId = privateInvestigatorInfo.BizContactId;

        if (bizContactId == null)
        {
            Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.PositionDictionary.TryGetValue(PositionEnum.PrivateInvestigatorManager.ToString(), out Guid positionId);
            List<Spd_BusinessContact> allBizContacts = _context.Spd_BusinessContactSet
                .Where(b => b.Spd_OrganizationId != null && b.Spd_OrganizationId.Id == app.Spd_OrganizationId.Id)
                .Where(b => b.StateCode == Spd_BusinessContact_StateCode.Active)
                .ToList();

            foreach (Spd_BusinessContact item in allBizContacts)
            {
                _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_Position_Spd_BusinessContact));
                _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
            }

            Spd_BusinessContact? existingBizContact = allBizContacts
                .Where(b => b.Spd_Position_Spd_BusinessContact.Any(p => p.Spd_PositionId == positionId))
                .Where(b => b.Spd_SwLNumber != null && b.Spd_SwLNumber.Id == privateInvestigatorInfo.LicenceId)
                .FirstOrDefault();

            if (existingBizContact == null)
            {
                DeletePrivateInvestigatorLink(app);

                bizContact = _mapper.Map<Spd_BusinessContact>(privateInvestigatorInfo);
                bizContact.Spd_BusinessContactId = Guid.NewGuid();
                bizContact.Spd_Role = Spd_BusinessContact_Spd_Role.Employee;
                _context.AddObject(bizContact);
                AddPrivateInvestigatorLink(bizContact, app);
            }
            else
            {
                bizContact = existingBizContact;
                AddPrivateInvestigatorLink(bizContact, app);
            }
        }
        else
        {
            bizContact = _context.Spd_BusinessContactSet.Where(b => b.Spd_BusinessContactId == bizContactId).FirstOrDefault();

            if (bizContact == null)
                throw new ArgumentException("business contact not found");

            _mapper.Map(privateInvestigatorInfo, bizContact);
            _context.UpdateObject(bizContact);

            _context.SaveChanges();
            AddPrivateInvestigatorLink(bizContact, app);
        }

        bizContact!.Spd_ContactId = contact.ToEntityReference();
        Spd_Licence licence = GetLicence(privateInvestigatorInfo.LicenceId.Value);
        bizContact.Spd_SwLNumber = licence.ToEntityReference();

        return await Task.FromResult(bizContact);
    }

    private async Task SetApplicantSwlLicenceId(Spd_Application app, Guid? applicantSwlLicenceId, CancellationToken ct)
    {
        if (applicantSwlLicenceId != null)
        {
            Spd_Licence? licence = _context.Spd_LicenceSet.Where(l => l.Spd_LicenceId == applicantSwlLicenceId).FirstOrDefault();
            if (licence != null)
            {
                app.Spd_ApplicantSWLNumberId = licence.ToEntityReference();
            }
        }
        else
        {
            app.Spd_ApplicantSWLNumberId = null;
        }

        await Task.CompletedTask;
    }

    private void AddPrivateInvestigatorLink(Spd_BusinessContact bizContact, Spd_Application app)
    {
        _context.LoadProperty(bizContact, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
        if (!bizContact.Spd_BusinessContact_Spd_Application.Any(a => a.Spd_ApplicationId == app.Spd_ApplicationId))
            _context.AddLink(bizContact, new Relationship(Spd_BusinessContact.Fields.Spd_BusinessContact_Spd_Application), app);

        Spd_Position? position = _context.Spd_PositionSet
            .Where(p => p.Spd_Name == PositionEnum.PrivateInvestigatorManager.ToString())
            .FirstOrDefault();

        if (position == null)
            return;

        _context.LoadProperty(bizContact, nameof(Spd_BusinessContact.Spd_Position_Spd_BusinessContact));
        if (!bizContact.Spd_Position_Spd_BusinessContact.Any(p => p.Spd_PositionId == position.Spd_PositionId))
            _context.AddLink(position, new Relationship(Spd_BusinessContact.Fields.Spd_Position_Spd_BusinessContact), bizContact);
    }

    private void DeletePrivateInvestigatorLink(Spd_Application app)
    {
        Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.PositionDictionary.TryGetValue(PositionEnum.PrivateInvestigatorManager.ToString(), out Guid positionId);

        List<Spd_BusinessContact> bizContacts = _context.Spd_BusinessContactSet
            .Where(b => b.Spd_OrganizationId != null && app.Spd_OrganizationId != null && b.Spd_OrganizationId.Id == app.Spd_OrganizationId.Id)
            .Where(b => b.StateCode == Spd_BusinessContact_StateCode.Active)
            .ToList();

        foreach (Spd_BusinessContact item in bizContacts)
        {
            _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_Position_Spd_BusinessContact));
            _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
        }

        Spd_BusinessContact? bizContact = bizContacts
            .Where(b => b.Spd_Position_Spd_BusinessContact.Any(p => p.Spd_PositionId == positionId))
            .Where(b => b.Spd_BusinessContact_Spd_Application.Any(a => a.Spd_ApplicationId == app.Spd_ApplicationId))
            .FirstOrDefault();

        if (bizContact == null)
            return;

        if (bizContact.Spd_BusinessContact_Spd_Application.Count() <= 1)
        {
            bizContact.StateCode = Spd_BusinessContact_StateCode.Inactive;
            bizContact.StatusCode = Spd_BusinessContact_StatusCode.Inactive;
            _context.UpdateObject(bizContact);
        }

        _context.DeleteLink(app, new Relationship(Spd_BusinessContact.Fields.Spd_BusinessContact_Spd_Application), bizContact);
        _context.SaveChanges();
    }

    //set biz manager info, applicant info, address and link biz to application
    private async Task SetInfoFromBiz(Account biz, Spd_Application app, bool applicantIsManager, CancellationToken ct)
    {
        app.Spd_AddressLine1 = biz.Address1_Line1;
        app.Spd_AddressLine2 = biz.Address1_Line2;
        app.Spd_City = biz.Address1_City;
        app.Spd_Province = biz.Address1_StateOrProvince;
        app.Spd_Country = biz.Address1_Country;
        app.Spd_PostalCode = biz.Address1_PostalCode;
        app.Spd_ResidentialAddress1 = biz.Address2_Line1;
        app.Spd_ResidentialAddress2 = biz.Address2_Line2;
        app.Spd_ResidentialCity = biz.Address2_City;
        app.Spd_ResidentialProvince = biz.Address2_StateOrProvince;
        app.Spd_ResidentialCountry = biz.Address2_Country;
        app.Spd_ResidentialPostalCode = biz.Address2_PostalCode;

        app.Spd_BusinessmanAgeRemail = biz.Spd_BusinessmanAgeRemail;
        app.Spd_BusinessManagerFirstName = biz.Spd_BusinessManagerFirstName;
        app.Spd_BusinessManagerMiddleName1 = biz.Spd_BusinessManagerMiddleName1;
        app.Spd_BusinessManagerMiddleName2 = biz.Spd_BusinessManagerMiddleName2;
        app.Spd_BusinessManagerPhone = biz.Spd_BusinessManagerPhone;
        app.Spd_BusinessManagerSurname = biz.Spd_BusinessManagerSurname;

        if (applicantIsManager)
        {
            app.Spd_EmailAddress1 = biz.Spd_BusinessmanAgeRemail;
            app.Spd_FirstName = biz.Spd_BusinessManagerFirstName;
            app.Spd_MiddleName1 = biz.Spd_BusinessManagerMiddleName1;
            app.Spd_MiddleName2 = biz.Spd_BusinessManagerMiddleName2;
            app.Spd_PhoneNumber = biz.Spd_BusinessManagerPhone;
            app.Spd_LastName = biz.Spd_BusinessManagerSurname;
        }

        app.Spd_ApplicantId = biz.ToEntityReference();
        app.Spd_OrganizationId = biz.ToEntityReference();
        _context.UpdateObject(app);
        _context.SaveChanges();

        await Task.CompletedTask;
    }

    private Spd_Licence GetLicence(Guid licenceId)
    {
        Spd_Licence? licence = _context.Spd_LicenceSet
            .Where(l => l.Spd_LicenceId == licenceId)
            .Where(l => l.StateCode == Spd_Licence_StateCode.Active)
            .FirstOrDefault();

        if (licence == null)
            throw new ArgumentException($"cannot find the licence with licenceId : {licenceId}");

        return licence;
    }

    private Account? GetOrgById(Guid orgId)
    {
        return _context.AccountSet
            .Where(a => a.AccountId == orgId)
            .FirstOrDefault();
    }

    private void LinkTeam(string teamGuidStr, Spd_Application app)
    {
        Guid teamGuid = Guid.Parse(teamGuidStr);
        Team? serviceTeam = _context.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
        if (serviceTeam != null)
            app.OwnerId = serviceTeam.ToEntityReference();
    }

    private void LinkServiceType(ServiceTypeEnum? serviceTypeCode, Spd_Application app)
    {
        if (serviceTypeCode == null)
            throw new ArgumentException("invalid LicenceApplication type");

        Guid? serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(serviceTypeCode.ToString());
        if (serviceTypeGuid == null)
            return;

        Spd_ServiceType? serviceType = _context.Spd_ServiceTypeSet
            .Where(s => s.Spd_ServiceTypeId == serviceTypeGuid)
            .FirstOrDefault();

        if (serviceType != null)
            app.Spd_ServiceTypeId = serviceType.ToEntityReference();
    }

    private void LinkLicence(Guid? licenceId, Spd_Application app)
    {
        if (licenceId == null)
            return;

        Spd_Licence? licence = _context.Spd_LicenceSet.Where(l => l.Spd_LicenceId == licenceId).FirstOrDefault();
        if (licence != null)
            app.Spd_CurrentExpiredLicenceId = licence.ToEntityReference();
    }

    private void LinkSubmittedByPortalUser(Guid? portalUserId, Spd_Application app)
    {
        if (portalUserId == null)
            return;

        Spd_PortalUser? portalUser = _context.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == portalUserId).FirstOrDefault();
        if (portalUser != null)
            app.Spd_SubmittedBy = portalUser.ToEntityReference();
    }

    private void ProcessCategories(IEnumerable<WorkerCategoryTypeEnum> categories, Spd_Application app)
    {
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Application_Spd_LicenceCategory));

        foreach (WorkerCategoryTypeEnum category in categories)
        {
            if (!Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LicenceCategoryDictionary.TryGetValue(category.ToString(), out Guid categoryId))
                throw new ArgumentException($"licence category not found for {category}");

            Spd_LicenceCategory? cat = _context.Spd_LicenceCategorySet.Where(c => c.Spd_LicenceCategoryId == categoryId).FirstOrDefault();
            if (cat == null)
                continue;

            if (!app.Spd_Application_Spd_LicenceCategory.Any(c => c.Spd_LicenceCategoryId == cat.Spd_LicenceCategoryId))
                _context.AddLink(app, new Relationship(Spd_Application.Fields.Spd_Application_Spd_LicenceCategory), cat);
        }

        foreach (Spd_LicenceCategory appCategory in app.Spd_Application_Spd_LicenceCategory.ToList())
        {
            string code = Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LookupLicenceCategoryKey(appCategory.Spd_LicenceCategoryId);
            if (!categories.Any(c => c.ToString() == code))
                _context.DeleteLink(app, new Relationship(Spd_Application.Fields.Spd_Application_Spd_LicenceCategory), appCategory);
        }
    }

    public async Task CancelDraftApplicationAsync(Guid applicationId, CancellationToken ct)
    {
        Spd_Application? app = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == applicationId).FirstOrDefault();
        if (app == null)
            throw new ArgumentException("Application not found");
        if (app.Spd_LicenceApplicationType == Spd_LicenceApplicationType.NewExpired)
            throw new ArgumentException("Canceling application with this type is not allowed.");
        if (app.StatusCode != Spd_Application_StatusCode.Draft && app.StatusCode != Spd_Application_StatusCode.Incomplete)
            throw new ArgumentException("Canceling application with this status is not allowed.");

        app.StateCode = Spd_Application_StateCode.Inactive;
        app.StatusCode = Spd_Application_StatusCode.Cancelled;
        _context.UpdateObject(app);
        _context.SaveChanges();
        await Task.CompletedTask;
    }
}

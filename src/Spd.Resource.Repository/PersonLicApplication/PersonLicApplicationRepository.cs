using AutoMapper;
using Spd.Resource.Repository.Alias;
using Spd.Resource.Repository.LicApp;
using Spd.Utilities.Shared.Exceptions;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dataverse;
using System.Net;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;

namespace Spd.Resource.Repository.PersonLicApplication;
internal class PersonLicApplicationRepository : IPersonLicApplicationRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;

    public PersonLicApplicationRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    //for unauth, create contact and application
    public async Task<LicenceApplicationCmdResp> CreateLicenceApplicationAsync(CreateLicenceApplicationCmd cmd, CancellationToken ct)
    {
        Spd_Application app = _mapper.Map<Spd_Application>(cmd);
        app.StatusCode = Spd_Application_StatusCode.Incomplete;
        _context.AddObject(app);
        SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
        ContactEntity? contact = _mapper.Map<ContactEntity>(cmd);
        if (cmd.ApplicationTypeCode == ApplicationTypeEnum.New) //spdbt-4370
        {
            if (cmd.ExpiredLicenceId == null)
            {
                contact = await _context.CreateContact(contact, null, _mapper.Map<IEnumerable<Spd_Alias>>(cmd.Aliases), ct);
                app.Spd_CurrentExpiredLicenceId = null;
            }
            else
            {
                var licence = _context.Spd_LicenceSet.Where(l => l.Spd_LicenceId == cmd.ExpiredLicenceId).FirstOrDefault();
                if (licence != null)
                {
                    app.Spd_CurrentExpiredLicenceId = licence.ToEntityReference();
                }
                else
                {
                    throw new ApiException(HttpStatusCode.BadRequest, "cannot find the expired licence. ");
                }

                if (licence.Spd_LicenceHolder == null)
                    throw new ApiException(HttpStatusCode.BadRequest, "cannot find the expired licence holder. ");

                ContactEntity? existingContact = _context.ContactSet.Where(c => c.ContactId == licence.Spd_LicenceHolder.Id).FirstOrDefault();
                if (existingContact == null)
                    throw new ApiException(HttpStatusCode.BadRequest, "cannot find the expired licence holder. ");

                if (contact.Spd_MentalHealthCondition == Spd_YesNo.No)
                    contact.Spd_MentalHealthCondition = null;
                contact = await _context.UpdateContact(existingContact, contact, null, _mapper.Map<IEnumerable<Spd_Alias>>(cmd.Aliases), ct);
            }
        }
        else
        {
            if (cmd.OriginalApplicationId != null)
            {
                Spd_Application? originApp = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == cmd.OriginalApplicationId).FirstOrDefault();
                if (originApp == null || originApp.Spd_ApplicantId == null)
                    throw new ArgumentException("for replace, renew or update, original application id cannot be null");

                //for replace, renew, update, "contact" is already exists, so, do update.
                ContactEntity? existingContact = _context.ContactSet.Where(c => c.ContactId == originApp.Spd_ApplicantId.Id).FirstOrDefault();
                if (existingContact == null)
                    throw new ArgumentException("for replace, renew or update, original application id cannot be null");

                //for spdbt-3706, do not update contact mental health condition from application when the value from app is No.
                if (contact.Spd_MentalHealthCondition == Spd_YesNo.No)
                    contact.Spd_MentalHealthCondition = null;
                contact = await _context.UpdateContact(existingContact, contact, null, _mapper.Map<IEnumerable<Spd_Alias>>(cmd.Aliases), ct);
            }
            else
            {
                throw new ArgumentException("for replace, renew or update, original application id cannot be null");
            }

            if (cmd.OriginalLicenceId != null)
            {
                SharedRepositoryFuncs.LinkLicence(_context, cmd.OriginalLicenceId, app);
            }
            else
            {
                throw new ArgumentException("for replace, renew or update, original licence id cannot be null");
            }
        }
        app.Spd_ApplicantId = contact?.ToEntityReference();
        SharedRepositoryFuncs.LinkTeam(_context, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        _context.SaveChanges();
        //Associate of 1:N navigation property with Create of Update is not supported in CRM, so have to save first.
        //then update category.
        if (cmd.ServiceTypeCode == ServiceTypeEnum.SecurityWorkerLicence)
        {
            SharedRepositoryFuncs.ProcessCategories(_context, cmd.CategoryCodes, app);
        }
        _context.SaveChanges();
        if (contact?.ContactId == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find contact for application.");

        return new LicenceApplicationCmdResp((Guid)app.Spd_ApplicationId, (Guid)contact.ContactId, null);
    }

    //for auth, do not need to create contact.
    public async Task<LicenceApplicationCmdResp> SaveLicenceApplicationAsync(SaveLicenceApplicationCmd cmd, CancellationToken ct)
    {
        Spd_Application? app;
        if (cmd.LicenceAppId != null)
        {
            app = _context.Spd_ApplicationSet
                .Where(a => a.Spd_ApplicationId == cmd.LicenceAppId)
                .FirstOrDefault();
            if (app == null)
                throw new ArgumentException("invalid app id");

            _context.LoadProperty(app, nameof(Spd_Application.Spd_Application_Spd_LicenceCategory));
            _mapper.Map<SaveLicenceApplicationCmd, Spd_Application>(cmd, app);
            app.Spd_ApplicationId = (Guid)(cmd.LicenceAppId);
            _context.UpdateObject(app);
        }
        else
        {
            app = _mapper.Map<Spd_Application>(cmd);
            _context.AddObject(app);
            var contact = _context.ContactSet.Where(l => l.ContactId == cmd.ApplicantId).FirstOrDefault();
            if (contact != null)
            {
                app.Spd_ApplicantId = contact.ToEntityReference();
            }
        }
        SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
        if (cmd.HasExpiredLicence == true && cmd.ExpiredLicenceId != null)
            SharedRepositoryFuncs.LinkLicence(_context, cmd.ExpiredLicenceId, app);
        else
            app.Spd_CurrentExpiredLicenceId = null;

        SharedRepositoryFuncs.LinkTeam(_context, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        _context.SaveChanges();
        //Associate of 1:N navigation property with Create of Update is not supported in CRM, so have to save first.
        //then update category.
        SharedRepositoryFuncs.ProcessCategories(_context, cmd.CategoryCodes, app);
        _context.SaveChanges();
        return new LicenceApplicationCmdResp((Guid)app.Spd_ApplicationId, cmd.ApplicantId, null);
    }
    public async Task<LicenceApplicationResp> GetLicenceApplicationAsync(Guid licenceApplicationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Spd_Application? app = _context.Spd_ApplicationSet
            .Where(a => a.Spd_ApplicationId == licenceApplicationId)
            .SingleOrDefault();

        if (app == null)
            throw new ArgumentException("invalid app id");

        _context.LoadProperty(app, nameof(Spd_Application.Spd_Spd_ServiceType_Spd_Application_ServiceTypeId));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Contact_Spd_Application_ApplicantId));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Application_Spd_LicenceCategory));
        _context.LoadProperty(app, nameof(Spd_Application.Spd_Licence_Spd_Application_CurrentExpired));

        LicenceApplicationResp appResp = _mapper.Map<LicenceApplicationResp>(app);
        if (app.Spd_Contact_Spd_Application_ApplicantId?.ContactId != null)
        {
            var aliases = SharedRepositoryFuncs.GetAliases((Guid)app.Spd_Contact_Spd_Application_ApplicantId.ContactId, _context);
            appResp.Aliases = _mapper.Map<AliasResp[]>(aliases);
        }

        return appResp;
    }

    public async Task<LicenceApplicationCmdResp> UpdateSwlSoleProprietorApplicationAsync(Guid swlAppId, Guid bizLicAppId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var swlApp = _context.Spd_ApplicationSet
            .Where(a => a.Spd_ApplicationId == swlAppId)
            .SingleOrDefault();
        if (swlApp == null) throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the swl application for {swlAppId}.");

        var bizLicApp = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == bizLicAppId).SingleOrDefault();
        if (bizLicApp == null) throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the business application for {bizLicAppId}.");

        if (bizLicApp.Spd_ApplicantId == null)
            throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the business for {bizLicAppId}.");

        var biz = _context.AccountSet.Where(a => a.AccountId == bizLicApp.Spd_ApplicantId.Id).SingleOrDefault();
        if (biz == null) throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the business for {bizLicApp.Spd_ApplicantId.Id}.");

        swlApp.Spd_BusinessLicenseId = bizLicApp.ToEntityReference();
        swlApp.Spd_SoleProprietorOrganizationId = biz.ToEntityReference();
        _context.UpdateObject(swlApp);
        _context.SaveChanges();

        return new LicenceApplicationCmdResp((Guid)swlApp.Spd_ApplicationId, swlApp.Spd_ApplicantId?.Id, swlApp.Spd_OrganizationId?.Id);
    }
}

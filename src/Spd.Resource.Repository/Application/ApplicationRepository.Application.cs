using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using EbContact = global::Spd.Utilities.Dataverse.EarlyBound.Contact;
using EbIncident = global::Spd.Utilities.Dataverse.EarlyBound.Incident;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Application;
internal partial class ApplicationRepository : IApplicationRepository
{
    public async Task<Guid?> AddApplicationAsync(ApplicationCreateCmd createApplicationCmd, CancellationToken ct)
    {
        Spd_Application? application = null;
        Account? org = _context.AccountSet.Where(a => a.AccountId == createApplicationCmd.OrgId).FirstOrDefault();
        if (org == null)
        {
            throw new InvalidOperationException($"cannot find org for {createApplicationCmd.OrgId}");
        }

        Spd_PortalUser? user = null;
        if (createApplicationCmd.CreatedByUserId != Guid.Empty)
        {
            user = _context.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == createApplicationCmd.CreatedByUserId).FirstOrDefault();
        }

        Team? serviceTeam = _context.TeamSet.Where(t => t.TeamId == Guid.Parse(DataverseConstants.Client_Service_Team_Guid)).FirstOrDefault();

        Spd_ServiceType? servicetype = null;
        if (createApplicationCmd.ServiceType != null)
        {
            Guid? serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(createApplicationCmd.ServiceType.ToString());
            if (serviceTypeGuid != null)
            {
                servicetype = _context.Spd_ServiceTypeSet.Where(s => s.Spd_ServiceTypeId == serviceTypeGuid).FirstOrDefault();
            }
        }

        Account? parentOrg = null;
        if (createApplicationCmd.ParentOrgId != null)
        {
            parentOrg = _context.AccountSet.Where(a => a.AccountId == createApplicationCmd.ParentOrgId).FirstOrDefault();
        }

        if (serviceTeam != null)
        {
            application = await CreateAppAsync(createApplicationCmd, org, user, serviceTeam, servicetype, parentOrg);
            _context.SaveChanges();
        }

        return await Task.FromResult(application?.Spd_ApplicationId);
    }

    public async Task<ApplicationListResp> QueryAsync(ApplicationListQry query, CancellationToken cancellationToken)
    {
        if (query == null || (query.FilterBy?.OrgId == null && query.FilterBy?.ParentOrgId == null))
            throw new ArgumentNullException("query.FilterBy.OrgId", "Must query applications by organization id.");

        IQueryable<Spd_Application> applications = _context.Spd_ApplicationSet;

        if (query.FilterBy?.OrgId != null)
            applications = applications.Where(a => a.Spd_OrganizationId != null && a.Spd_OrganizationId.Id == query.FilterBy.OrgId);

        if (query.FilterBy?.ParentOrgId != null)
            applications = applications.Where(a => a.Spd_ParentOrganizationId != null && a.Spd_ParentOrganizationId.Id == query.FilterBy.ParentOrgId);

        if (query.FilterBy?.ApplicationPortalStatus != null && query.FilterBy.ApplicationPortalStatus.Any())
        {
            var statuses = query.FilterBy.ApplicationPortalStatus.Select(s => Enum.Parse<Spd_PortalStatus>(s.ToString())).ToList();
            applications = applications.Where(a => a.Spd_PortalStatus != null && statuses.Contains(a.Spd_PortalStatus.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.FilterBy?.NameOrEmailOrAppIdContains))
        {
            string search = query.FilterBy.NameOrEmailOrAppIdContains;
            applications = applications.Where(a =>
                (a.Spd_FirstName != null && a.Spd_FirstName.Contains(search)) ||
                (a.Spd_LastName != null && a.Spd_LastName.Contains(search)) ||
                (a.Spd_EmailAddress1 != null && a.Spd_EmailAddress1.Contains(search)) ||
                (a.Spd_Name != null && a.Spd_Name.Contains(search)) ||
                (a.Spd_FullName != null && a.Spd_FullName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.FilterBy?.NameOrAppIdContains))
        {
            string search = query.FilterBy.NameOrAppIdContains;
            applications = applications.Where(a =>
                (a.Spd_FirstName != null && a.Spd_FirstName.Contains(search)) ||
                (a.Spd_LastName != null && a.Spd_LastName.Contains(search)) ||
                (a.Spd_Name != null && a.Spd_Name.Contains(search)) ||
                (a.Spd_FullName != null && a.Spd_FullName.Contains(search)));
        }

        if (query.FilterBy?.Paid != null)
        {
            if (query.FilterBy.Paid.Value)
                applications = applications.Where(a => a.Spd_PaidOn != null);
            else
                applications = applications.Where(a => a.StatusCode == Spd_Application_StatusCode.PaymentPending);
        }

        if (query.FilterBy?.PayerType != null)
        {
            var payer = Enum.Parse<Spd_PayerType>(query.FilterBy.PayerType.ToString());
            applications = applications.Where(a => a.Spd_Payer == payer);
        }

        if (query.FilterBy?.FromDateTime != null)
        {
            DateTime from = query.FilterBy.FromDateTime.Value.UtcDateTime;
            applications = applications.Where(a => a.CreatedOn >= from);
        }

        if (query.FilterBy?.ToDateTime != null)
        {
            DateTime to = query.FilterBy.ToDateTime.Value.AddDays(1).UtcDateTime;
            applications = applications.Where(a => a.CreatedOn < to);
        }

        if (!string.IsNullOrWhiteSpace(query.FilterBy?.DelegatePortalUserId))
            applications = applications.Where(a => a.Spd_DelegateId == query.FilterBy.DelegatePortalUserId);

        List<Guid?> serviceTypeGuid = IApplicationRepository.ScreeningServiceTypes.Select(c => DataverseContextLookupHelpers.GetServiceTypeGuid(c.ToString())).ToList();
        applications = applications.Where(a => a.Spd_ServiceTypeId != null && serviceTypeGuid.Contains(a.Spd_ServiceTypeId.Id));

        applications = query.SortBy switch
        {
            { SubmittedDateDesc: false } => applications.OrderBy(a => a.CreatedOn),
            { NameDesc: true } => applications.OrderByDescending(a => a.Spd_FullName),
            { NameDesc: false } => applications.OrderBy(a => a.Spd_FullName),
            { CompanyNameDesc: true } => applications.OrderByDescending(a => a.Spd_ContractedCompanyName),
            { CompanyNameDesc: false } => applications.OrderBy(a => a.Spd_ContractedCompanyName),
            { PaidAndSubmittedOnDesc: true } => applications.OrderByDescending(a => a.Spd_PaidOn).ThenByDescending(a => a.CreatedOn),
            _ => applications.OrderByDescending(a => a.CreatedOn)
        };

        List<Spd_Application> data = applications.ToList();
        int total = data.Count;

        if (query.Paging != null)
        {
            data = data.Skip(query.Paging.Page * query.Paging.PageSize)
                       .Take(query.Paging.PageSize)
                       .ToList();
        }

        var response = new ApplicationListResp
        {
            Applications = _mapper.Map<IEnumerable<ApplicationResult>>(data)
        };

        if (query.Paging != null)
        {
            response.Pagination = new PaginationResp
            {
                PageSize = query.Paging.PageSize,
                PageIndex = query.Paging.Page,
                Length = total
            };
        }

        return await Task.FromResult(response);
    }

    public async Task UpdateAsync(UpdateCmd cmd, CancellationToken ct)
    {
        Spd_Application? app = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == cmd.ApplicationId).FirstOrDefault();
        if (app == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid ApplicationId");

        if (cmd.Status != null)
        {
            app.StatusCode = Enum.Parse<Spd_Application_StatusCode>(cmd.Status.ToString());
            if (cmd.Status == ApplicationStatusEnum.Submitted || cmd.Status == ApplicationStatusEnum.Cancelled)
                app.StateCode = Spd_Application_StateCode.Inactive;
            else
                app.StateCode = Spd_Application_StateCode.Active;
        }
        if (cmd.HaveVerifiedIdentity != null)
        {
            app.Spd_IdentityConfirmed = cmd.HaveVerifiedIdentity;
        }

        _context.UpdateObject(app);
        _context.SaveChanges();
        return;
    }

    public async Task<bool> CheckApplicationDuplicateAsync(SearchApplicationQry searchApplicationQry, CancellationToken ct)
    {
        DateTime? dob = searchApplicationQry.DateOfBirth.HasValue
            ? new DateTime(searchApplicationQry.DateOfBirth.Value.Year, searchApplicationQry.DateOfBirth.Value.Month, searchApplicationQry.DateOfBirth.Value.Day)
            : null;

        var application = _context.Spd_ApplicationSet.Where(o =>
            o.Spd_OrganizationId != null && o.Spd_OrganizationId.Id == searchApplicationQry.OrgId &&
            o.Spd_FirstName == searchApplicationQry.GivenName &&
            o.Spd_LastName == searchApplicationQry.Surname &&
            o.Spd_DateOfBirth == dob &&
            o.StateCode != Spd_Application_StateCode.Inactive
        ).FirstOrDefault();

        return await Task.FromResult(application != null);
    }

    public async Task<ApplicantApplicationListResp> QueryApplicantApplicationListAsync(ApplicantApplicationListQry query, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"query applicant application with applicantId={query.ApplicantId}");

        var applications = _context.Spd_ApplicationSet
            .Where(r => r.Spd_ApplicantId != null && r.Spd_ApplicantId.Id == query.ApplicantId)
            .OrderByDescending(i => i.CreatedOn);

        List<Guid?> serviceTypeGuid = IApplicationRepository.ScreeningServiceTypes.Select(c => DataverseContextLookupHelpers.GetServiceTypeGuid(c.ToString())).ToList();

        var result = applications.ToList().Where(a => a.Spd_ServiceTypeId != null && serviceTypeGuid.Contains(a.Spd_ServiceTypeId.Id));
        var response = new ApplicantApplicationListResp();
        response.Applications = _mapper.Map<IEnumerable<ApplicationResult>>(result);
        return await Task.FromResult(response);
    }

    public async Task<ApplicationResult> QueryApplicationAsync(ApplicationQry query, CancellationToken ct)
    {
        var application = _context.Spd_ApplicationSet
            .Where(r => r.Spd_ApplicationId == query.ApplicationId)
            .FirstOrDefault();

        return await Task.FromResult(_mapper.Map<ApplicationResult>(application));
    }

    public async Task ProcessAppWithSharableClearanceAsync(ApplicationCreateCmd createApplicationCmd, CancellationToken ct)
    {
        if (!createApplicationCmd.SharedClearanceId.HasValue)
            throw new ArgumentException("SharedClearanceId cannot be null");

        Account? org = _context.AccountSet.Where(a => a.AccountId == createApplicationCmd.OrgId).FirstOrDefault();
        if (org == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find organization.");

        EbContact? contact = _context.ContactSet.Where(c => c.ContactId == createApplicationCmd.ContactId).FirstOrDefault();
        if (contact == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find contact.");

        _mapper.Map<ApplicationCreateCmd, EbContact>(createApplicationCmd, contact);
        _context.UpdateObject(contact);

        Spd_Clearance? clearance = _context.Spd_ClearanceSet
            .Where(c => c.Spd_ClearanceId == createApplicationCmd.SharedClearanceId)
            .FirstOrDefault();
        if (clearance == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find specified clearance.");

        EbIncident? caseIncident = null;
        if (clearance.Spd_CaseId != null)
        {
            caseIncident = _context.IncidentSet.Where(i => i.IncidentId == clearance.Spd_CaseId.Id).FirstOrDefault();
        }

        Team? team;
        Spd_ClearanceAccess_StatusCode status;
        if (caseIncident?.Spd_RiskLevel == Spd_RiskLevel.L2 || caseIncident?.Spd_RiskLevel == Spd_RiskLevel.L3)
        {
            Guid teamGuid = Guid.Parse(DataverseConstants.Screening_Risk_Assessment_Coordinator_Team_Guid);
            team = _context.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
            status = Spd_ClearanceAccess_StatusCode.Draft;
        }
        else
        {
            Guid teamGuid = Guid.Parse(DataverseConstants.Client_Service_Team_Guid);
            team = _context.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
            status = Spd_ClearanceAccess_StatusCode.Approved;
        }

        if (team == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find team.");

        Spd_ClearanceAccess clearanceAccess = new()
        {
            Spd_ClearanceAccessId = Guid.NewGuid(),
            StateCode = Spd_ClearanceAccess_StateCode.Active,
            StatusCode = status,
            Spd_IsSystemGenerated = true,
            Spd_OrganizationId = org.ToEntityReference(),
            Spd_ClearanceId = clearance.ToEntityReference(),
            OwnerId = team.ToEntityReference()
        };

        _context.AddObject(clearanceAccess);
        _context.SaveChanges();

        return;
    }

    private Spd_Alias? GetAlias(AliasCreateCmd aliasCreateCmd, EbContact contact)
    {
        var matchingAlias = _context.Spd_AliasSet.Where(o =>
           o.Spd_FirstName == aliasCreateCmd.GivenName &&
           o.Spd_MiddleName1 == aliasCreateCmd.MiddleName1 &&
           o.Spd_MiddleName2 == aliasCreateCmd.MiddleName2 &&
           o.Spd_Surname == aliasCreateCmd.Surname &&
           o.StateCode != Spd_Alias_StateCode.Inactive &&
           o.Spd_ContactId != null && o.Spd_ContactId.Id == contact.ContactId
       ).FirstOrDefault();
        return matchingAlias;
    }

    private EbContact? GetContact(ApplicationCreateCmd createApplicationCmd)
    {
        DateTime? dob = createApplicationCmd.DateOfBirth.HasValue
            ? new DateTime(createApplicationCmd.DateOfBirth.Value.Year, createApplicationCmd.DateOfBirth.Value.Month, createApplicationCmd.DateOfBirth.Value.Day)
            : null;

        var contacts = _context.ContactSet
            .Where(o =>
            o.FirstName == createApplicationCmd.GivenName &&
            o.LastName == createApplicationCmd.Surname &&
            o.Birthdate == dob &&
            o.StateCode != Contact_StateCode.Inactive);

        if (createApplicationCmd.DriversLicense == null || string.IsNullOrEmpty(createApplicationCmd.DriversLicense))
        {
            return contacts.FirstOrDefault();
        }
        else
        {
            contacts = contacts
            .Where(o => o.Spd_BcDriversLicense == null || o.Spd_BcDriversLicense == createApplicationCmd.DriversLicense);
            return contacts.FirstOrDefault();
        }
    }

    private static string GetAppFilterString(AppFilterBy appFilterBy)
    {
        //orgfilter
        string? orgFilter = null;
        if (appFilterBy.OrgId != null)
            orgFilter = $"_spd_organizationid_value eq {appFilterBy.OrgId}";

        //parent org filter
        string? parentOrgFilter = null;
        if (appFilterBy.ParentOrgId != null)
            parentOrgFilter = $"_spd_parentorganizationid_value eq {appFilterBy.ParentOrgId}";

        //portal status filter
        string? portalStatusFilter = null;
        if (appFilterBy.ApplicationPortalStatus != null && appFilterBy.ApplicationPortalStatus.Any())
        {
            List<string> strs = new();
            foreach (ApplicationPortalStatusEnum cd in appFilterBy.ApplicationPortalStatus)
            {
                var status = Enum.Parse<ApplicationPortalStatus>(cd.ToString());
                strs.Add($"spd_portalstatus eq {(int)status}");
            }
            portalStatusFilter = $"({string.Join(" or ", strs)})";
        }

        //name email appId 
        string? containsNameEmailAppId = null;
        if (!string.IsNullOrWhiteSpace(appFilterBy.NameOrEmailOrAppIdContains))
        {
            var safeValue = appFilterBy.NameOrEmailOrAppIdContains.Replace("'", "''");
            containsNameEmailAppId = $"(contains(spd_firstname,'{safeValue}') or contains(spd_lastname,'{safeValue}') or contains(spd_emailaddress1,'{safeValue}') or contains(spd_name,'{safeValue}') or contains(spd_fullname,'{safeValue}'))";
        }

        //name appId 
        string? containsNameAppId = null;
        if (!string.IsNullOrWhiteSpace(appFilterBy.NameOrAppIdContains))
        {
            var safeValue = appFilterBy.NameOrAppIdContains.Replace("'", "''");
            containsNameAppId = $"(contains(spd_firstname,'{safeValue}') or contains(spd_lastname,'{safeValue}') or contains(spd_name,'{safeValue}') or contains(spd_fullname,'{safeValue}'))";
        }

        //paid
        string? paid = null;
        if (appFilterBy.Paid != null)
        {
            if ((bool)appFilterBy.Paid)
                paid = $"spd_paidon ne null";
            else
                paid = $"statuscode eq {100000000}"; //status reason equals "waiting for payment"
        }

        //payer type
        string? payer = null;
        if (appFilterBy.PayerType != null)
        {
            int payerValue = (int)Enum.Parse<PayerPreferenceOptionSet>(appFilterBy.PayerType.ToString());
            payer = $"spd_payer eq {payerValue}";
        }

        //submitted from date
        string? submitFromDate = null;
        if (appFilterBy.FromDateTime != null)
        {
            var date = new Microsoft.OData.Edm.Date(((DateTimeOffset)appFilterBy.FromDateTime).Year, ((DateTimeOffset)appFilterBy.FromDateTime).Month, ((DateTimeOffset)appFilterBy.FromDateTime).Day);
            submitFromDate = $"createdon ge {date}";
        }

        //submitted to date
        string? submitToDate = null;
        if (appFilterBy.ToDateTime != null)
        {
            DateTimeOffset adjustToDateTime = appFilterBy.ToDateTime.Value.AddDays(1);
            var date = new Microsoft.OData.Edm.Date(adjustToDateTime.Year, adjustToDateTime.Month, adjustToDateTime.Day);
            submitToDate = $"createdon lt {date}";
        }

        //delegate portal user
        string? delegateFilters = null;
        if (appFilterBy.DelegatePortalUserId != null)
        {
            delegateFilters = $"spd_delegateid eq '{appFilterBy.DelegatePortalUserId}'";
        }

        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.CRRP_EMPLOYEE.ToString(), out Guid crrpEmployeeServiceTypeId);
        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.CRRP_VOLUNTEER.ToString(), out Guid crrpVolunteerServiceTypeId);
        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.PSSO.ToString(), out Guid pssoServiceTypeId);
        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.PSSO_VS.ToString(), out Guid pssoVsServiceTypeId);
        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.MCFD.ToString(), out Guid mcfdServiceTypeId);
        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.PE_CRC.ToString(), out Guid pecrcServiceTypeId);
        DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.PE_CRC_VS.ToString(), out Guid pecrcvsServiceTypeId);
        string? serviceTypeFilters = $"_spd_servicetypeid_value eq '{crrpEmployeeServiceTypeId}' or _spd_servicetypeid_value eq '{crrpVolunteerServiceTypeId}' or _spd_servicetypeid_value eq '{pssoServiceTypeId}' or _spd_servicetypeid_value eq '{pssoVsServiceTypeId}' or _spd_servicetypeid_value eq '{mcfdServiceTypeId}' or _spd_servicetypeid_value eq '{pecrcServiceTypeId}' or _spd_servicetypeid_value eq '{pecrcvsServiceTypeId}'";

        //get result filter string
        string result = string.Empty;
        if (orgFilter != null)
        {
            result += $"{orgFilter}";
        }
        if (parentOrgFilter != null)
        {
            if (result == String.Empty) result += $"{parentOrgFilter}";
            else result += $" and {parentOrgFilter}";
        }
        if (portalStatusFilter != null)
        {
            result += $" and {portalStatusFilter}";
        }
        if (containsNameEmailAppId != null)
        {
            result += $" and {containsNameEmailAppId}";
        }
        if (containsNameAppId != null)
        {
            result += $" and {containsNameAppId}";
        }
        if (paid != null)
        {
            result += $" and {paid}";
        }
        if (payer != null)
        {
            result += $" and {payer}";
        }
        if (submitFromDate != null)
        {
            result += $" and {submitFromDate}";
        }
        if (submitToDate != null)
        {
            result += $" and {submitToDate}";
        }
        result += $" and ({serviceTypeFilters})";

        //dynamics did something special for delegateid, so, this has to be at the end of the query.
        if (delegateFilters != null)
        {
            result += $" and {delegateFilters}"; //when dynamics detect delegatId here, it will do filtering on their side.
        }

        return result;
    }

    private static string GetAppSortString(AppSortBy? appSortBy)
    {
        if (appSortBy == null
            || (appSortBy.SubmittedDateDesc != null && (bool)appSortBy.SubmittedDateDesc))
            return "createdon desc";

        if (appSortBy.SubmittedDateDesc != null && !(bool)appSortBy.SubmittedDateDesc)
            return "createdon";

        if (appSortBy.NameDesc != null && (bool)appSortBy.NameDesc)
            return "spd_fullname desc";

        if (appSortBy.NameDesc != null && !(bool)appSortBy.NameDesc)
            return "spd_fullname";

        if (appSortBy.CompanyNameDesc != null && (bool)appSortBy.CompanyNameDesc)
            return "spd_contractedcompanyname desc";

        if (appSortBy.CompanyNameDesc != null && !(bool)appSortBy.CompanyNameDesc)
            return "spd_contractedcompanyname";

        if (appSortBy.PaidAndSubmittedOnDesc != null && (bool)appSortBy.PaidAndSubmittedOnDesc)
            return "spd_paidon, createdon desc";

        return "createdon desc";
    }

    //note: any change in this function, the operation number also needs to change in AddBulkAppsAsync
    private async Task<Spd_Application> CreateAppAsync(
        ApplicationCreateCmd createApplicationCmd,
        Account org,
        Spd_PortalUser? user,
        Team team,
        Spd_ServiceType? serviceType,
        Account? parentOrg = null)
    {
        Spd_Application app = new Spd_Application
        {
            Spd_ApplicationId = Guid.NewGuid(),
            Spd_OrganizationId = org.ToEntityReference(),
            Spd_SubmittedBy = user?.ToEntityReference(),
            OwnerId = team.ToEntityReference(),
            Spd_ServiceTypeId = serviceType?.ToEntityReference(),
            Spd_ParentOrganizationId = parentOrg?.ToEntityReference(),
            Spd_FirstName = createApplicationCmd.GivenName,
            Spd_MiddleName1 = createApplicationCmd.MiddleName1,
            Spd_MiddleName2 = createApplicationCmd.MiddleName2,
            Spd_LastName = createApplicationCmd.Surname,
            Spd_EmailAddress1 = createApplicationCmd.EmailAddress,
            Spd_EmployeeId = createApplicationCmd.EmployeeId,
            Spd_UploadId = createApplicationCmd.UploadId,
            Spd_DateOfBirth = createApplicationCmd.DateOfBirth.HasValue ? new DateTime(createApplicationCmd.DateOfBirth.Value.Year, createApplicationCmd.DateOfBirth.Value.Month, createApplicationCmd.DateOfBirth.Value.Day) : null,
            StateCode = Spd_Application_StateCode.Active,
            StatusCode = createApplicationCmd.HaveVerifiedIdentity == true ? Spd_Application_StatusCode.PaymentPending : Spd_Application_StatusCode.ApplicantVerification,
            Spd_IdentityConfirmed = createApplicationCmd.HaveVerifiedIdentity
        };

        _context.AddObject(app);

        EbContact? contact;
        if (createApplicationCmd.CreatedByApplicantBcscId != null)
        {
            contact = ProcessContactWithBcscApplicant(createApplicationCmd);
        }
        else
        {
            contact = AddContact(createApplicationCmd);
        }

        if (contact != null)
        {
            app.Spd_ApplicantId = contact.ToEntityReference();
            foreach (var item in createApplicationCmd.Aliases)
            {
                AddAlias(item, contact);
            }
        }

        return await Task.FromResult(app);
    }

    private EbContact ProcessContactWithBcscApplicant(ApplicationCreateCmd createApplicationCmd)
    {
        var identity = _context.Spd_IdentitySet
               .Where(i => i.Spd_UserGuid == createApplicationCmd.CreatedByApplicantBcscId)
               .Where(i => i.Spd_Type == Spd_IdentityProvider.BcServicesCard)
               .FirstOrDefault();

        if (identity == null)
        {
            identity = new Spd_Identity
            {
                Spd_IdentityId = Guid.NewGuid(),
                Spd_UserGuid = createApplicationCmd.CreatedByApplicantBcscId,
                Spd_Type = Spd_IdentityProvider.BcServicesCard
            };
            _context.AddObject(identity);
            var contact = AddContact(createApplicationCmd);
            identity.Spd_ContactId = contact.ToEntityReference();
            return contact;
        }
        else
        {
            EbContact? existingContact = null;
            if (identity.Spd_ContactId != null)
            {
                existingContact = _context.ContactSet.Where(c => c.ContactId == identity.Spd_ContactId.Id).FirstOrDefault();
            }

            if (existingContact != null)
            {
                if (!(string.Equals(existingContact.FirstName, createApplicationCmd.GivenName, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(existingContact.LastName, createApplicationCmd.Surname, StringComparison.InvariantCultureIgnoreCase)))
                {
                    AliasCreateCmd newAlias = new()
                    {
                        Surname = existingContact.LastName,
                        GivenName = existingContact.FirstName,
                    };
                    AddAlias(newAlias, existingContact);
                }
                _mapper.Map<ApplicationCreateCmd, EbContact>(createApplicationCmd, existingContact);
                _context.UpdateObject(existingContact);
                return existingContact;
            }
            else
            {
                var contact = AddContact(createApplicationCmd);
                identity.Spd_ContactId = contact.ToEntityReference();
                return contact;
            }
        }
    }

    private EbContact AddContact(ApplicationCreateCmd createApplicationCmd)
    {
        var contact = GetContact(createApplicationCmd);
        if (contact == null)
        {
            contact = _mapper.Map<EbContact>(createApplicationCmd);
            contact.ContactId = Guid.NewGuid();
            _context.AddObject(contact);
        }
        else
        {
            _mapper.Map<ApplicationCreateCmd, EbContact>(createApplicationCmd, contact);
            _context.UpdateObject(contact);
        }
        return contact;
    }

    private void AddAlias(AliasCreateCmd createAliasCmd, EbContact contact)
    {
        Spd_Alias? matchingAlias = GetAlias(createAliasCmd, contact);
        if (matchingAlias == null)
        {
            Spd_Alias alias = _mapper.Map<Spd_Alias>(createAliasCmd);
            alias.Spd_ContactId = contact.ToEntityReference();
            _context.AddObject(alias);
        }
    }
}


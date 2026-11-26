using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using Spd.Utilities.Shared.Tools;
using System.Net;

namespace Spd.Resource.Repository.Application;
internal partial class ApplicationRepository : IApplicationRepository
{
    public async Task<ClearanceAccessListResp> QueryAsync(ClearanceAccessListQry clearanceListQry, CancellationToken ct)
    {
        if (clearanceListQry == null || clearanceListQry.FilterBy?.OrgId == null)
            throw new ArgumentNullException("clearanceListQry.FilterBy.OrgId", "Must query clearances by organization id.");

        ct.ThrowIfCancellationRequested();
        ClearanceAccessStatusOptionSet status = Enum.Parse<ClearanceAccessStatusOptionSet>(clearanceListQry.FilterBy.ClearanceAccessStatus.ToString());
        var dataverseStatus = Enum.Parse<Spd_ClearanceAccess_StatusCode>(status.ToString());
        DateOnly currentDate = DateOnlyHelper.GetCurrentPCTDate();
        DateTime expiryCutoff = currentDate.AddDays(90).ToDateTime(TimeOnly.MinValue);

        IQueryable<Spd_ClearanceAccess> clearanceaccesses = _context.Spd_ClearanceAccessSet
            .Where(c => c.Spd_OrganizationId != null && c.Spd_OrganizationId.Id == clearanceListQry.FilterBy.OrgId)
            .Where(c => c.StatusCode == dataverseStatus)
            .Where(c => c.Spd_ExpiryDate != null && c.Spd_ExpiryDate <= expiryCutoff);

        if (!string.IsNullOrWhiteSpace(clearanceListQry.FilterBy.NameOrEmailContains))
        {
            string contains = clearanceListQry.FilterBy.NameOrEmailContains;
            clearanceaccesses = clearanceaccesses.Where(c =>
                (c.Spd_ApplicantFullName ?? string.Empty).Contains(contains) ||
                (c.Spd_EmailAddress1 ?? string.Empty).Contains(contains));
        }

        clearanceaccesses = GetClearanceSortBy(clearanceListQry.SortBy) switch
        {
            "spd_expirydate" => clearanceaccesses.OrderBy(c => c.Spd_ExpiryDate),
            "spd_expirydate desc" => clearanceaccesses.OrderByDescending(c => c.Spd_ExpiryDate),
            "spd_applicantfullname desc" => clearanceaccesses.OrderByDescending(c => c.Spd_ApplicantFullName),
            "spd_applicantfullname" => clearanceaccesses.OrderBy(c => c.Spd_ApplicantFullName),
            "spd_contractedcompanyname desc" => clearanceaccesses.OrderByDescending(c => c.Spd_ContractedCompanyName),
            "spd_contractedcompanyname" => clearanceaccesses.OrderBy(c => c.Spd_ContractedCompanyName),
            _ => clearanceaccesses.OrderByDescending(c => c.CreatedOn),
        };

        int totalCount = clearanceaccesses.Count();

        if (clearanceListQry.Paging != null)
        {
            int skip = clearanceListQry.Paging.Page * clearanceListQry.Paging.PageSize;
            clearanceaccesses = clearanceaccesses.Skip(skip).Take(clearanceListQry.Paging.PageSize);
        }

        var result = clearanceaccesses.ToList();

        var response = new ClearanceAccessListResp();
        response.Clearances = result.Select(s => new ClearanceAccessResp
        {
            Id = s.Spd_ClearanceAccessId ?? Guid.Empty,
            FirstName = s.Spd_ApplicantFirstName ?? string.Empty,
            LastName = s.Spd_ApplicantLastName ?? string.Empty,
            Email = s.Spd_EmailAddress1 ?? string.Empty,
            ExpiresOn = s.Spd_ExpiryDate,
            Facility = s.Spd_ContractedCompanyName ?? string.Empty,
            Status = s.StatusCode?.ToString() ?? string.Empty,
            ClearanceId = s.Spd_ClearanceId?.Id ?? Guid.Empty,
            ClearanceAccessNumber = s.Spd_ClearanceAccessNumber ?? string.Empty,
        });
        if (clearanceListQry.Paging != null)
        {
            response.Pagination = new PaginationResp();
            response.Pagination.PageSize = clearanceListQry.Paging.PageSize;
            response.Pagination.PageIndex = clearanceListQry.Paging.Page;
            response.Pagination.Length = totalCount;
        }

        return response;
    }

    public async Task<ClearanceListResp> QueryAsync(ClearanceQry clearanceQry, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ClearanceListResp resp = new();

        IQueryable<Spd_Clearance> clearances = _context.Spd_ClearanceSet
            .Where(c => c.StateCode != Spd_Clearance_StateCode.Inactive);

        if (clearanceQry.ClearanceId != null)
        {
            clearances = clearances.Where(c => c.Spd_ClearanceId == clearanceQry.ClearanceId);
        }

        if (clearanceQry.ServiceType != null)
        {
            var keyExisted = Spd.Utilities.Dataverse.DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(clearanceQry.ServiceType.ToString(), out Guid stGuid);
            if (!keyExisted)
                throw new ArgumentException("invalid service type");
            clearances = clearances.Where(c => c.Spd_ServiceType != null && c.Spd_ServiceType.Id == stGuid);
        }

        if (clearanceQry.ContactId != null)
            clearances = clearances.Where(c => c.Spd_ContactId != null && c.Spd_ContactId.Id == clearanceQry.ContactId);

        if (clearanceQry.FromDate != null)
            clearances = clearances.Where(c => c.Spd_ExpiryDate > clearanceQry.FromDate);

        if (clearanceQry.WorkWith != null)
        {
            if (clearanceQry.WorkWith == EmployeeInteractionTypeCode.Neither)
                clearances = clearances.Where(c => c.Spd_WorksWith == null);
            else
            {
                Spd_OrganizationClientBase workwith = Enum.Parse<Spd_OrganizationClientBase>(clearanceQry.WorkWith.ToString());
                clearances = clearances.Where(c => c.Spd_WorksWith == workwith);
            }
        }

        if (clearanceQry.Shareable != null)
        {
            clearances = (bool)clearanceQry.Shareable ? clearances.Where(c => c.Spd_Sharable == Spd_YesNo.Yes) :
                clearances.Where(c => c.Spd_Sharable == Spd_YesNo.No);
        }

        //spdbt-2629
        if (clearanceQry.IncludeServiceTypeEnum != null)
        {
            if (clearanceQry.IncludeServiceTypeEnum == ServiceTypeEnum.CRRP_EMPLOYEE || clearanceQry.IncludeServiceTypeEnum == ServiceTypeEnum.CRRP_VOLUNTEER)
            {
                Spd.Utilities.Dataverse.DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.CRRP_VOLUNTEER.ToString(), out Guid volunteerGuid);
                Spd.Utilities.Dataverse.DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(ServiceTypeEnum.CRRP_EMPLOYEE.ToString(), out Guid employeeGuid);
                clearances = clearances.Where(c =>
                    c.Spd_ServiceType != null &&
                    (c.Spd_ServiceType.Id == volunteerGuid || c.Spd_ServiceType.Id == employeeGuid));
            }
            else
            {
                var keyExisted = Spd.Utilities.Dataverse.DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(clearanceQry.IncludeServiceTypeEnum.ToString(), out Guid stGuid);
                if (!keyExisted)
                    throw new ArgumentException("invalid service type");
                clearances = clearances.Where(c => c.Spd_ServiceType != null && c.Spd_ServiceType.Id == stGuid);
            }
        }

        if (clearanceQry.IncludeWorkWith != null)
        {
            if (clearanceQry.IncludeWorkWith == EmployeeInteractionTypeCode.Children) //work with children
            {
                Spd_OrganizationClientBase child = Enum.Parse<Spd_OrganizationClientBase>(EmployeeInteractionTypeCode.Children.ToString());
                Spd_OrganizationClientBase childAdult = Enum.Parse<Spd_OrganizationClientBase>(EmployeeInteractionTypeCode.ChildrenAndAdults.ToString());
                clearances = clearances.Where(c => c.Spd_WorksWith == child || c.Spd_WorksWith == childAdult);
            }
            else if (clearanceQry.IncludeWorkWith == EmployeeInteractionTypeCode.Adults) //work with adult
            {
                Spd_OrganizationClientBase adult = Enum.Parse<Spd_OrganizationClientBase>(EmployeeInteractionTypeCode.Adults.ToString());
                Spd_OrganizationClientBase childAdult = Enum.Parse<Spd_OrganizationClientBase>(EmployeeInteractionTypeCode.ChildrenAndAdults.ToString());
                clearances = clearances.Where(c => c.Spd_WorksWith == adult || c.Spd_WorksWith == childAdult);
            }
            else //work with children and adult
            {
                Spd_OrganizationClientBase childAdult = Enum.Parse<Spd_OrganizationClientBase>(EmployeeInteractionTypeCode.ChildrenAndAdults.ToString());
                clearances = clearances.Where(c => c.Spd_WorksWith == childAdult);
            }
        }
        //spd

        List<Spd_Clearance> clearanceList = clearances.ToList();
        foreach (Spd_Clearance clearance in clearanceList)
        {
            _context.LoadProperty(clearance, nameof(Spd_Clearance.Spd_Incident_Spd_Clearance));
        }

        resp.Clearances = clearanceList.Select(c => new ClearanceResp
        {
            OrgId = c.Spd_Incident_Spd_Clearance?.Spd_OrganizationId?.Id ?? Guid.Empty,
            ApplicationId = c.Spd_Incident_Spd_Clearance?.Spd_ApplicationId?.Id ?? Guid.Empty,
            GrantedDate = c.Spd_DateGranted,
            ExpiryDate = c.Spd_ExpiryDate,
            WorkWith = c.Spd_WorksWith == null ? null : Enum.Parse<EmployeeInteractionTypeCode>(c.Spd_WorksWith.ToString()),
            ServiceType = Enum.Parse<ServiceTypeEnum>(Spd.Utilities.Dataverse.DataverseContextLookupHelpers.GetServiceTypeName(c.Spd_ServiceType?.Id)),
            ClearanceId = c.Spd_ClearanceId ?? Guid.Empty,
        });

        return resp;
    }

    public async System.Threading.Tasks.Task DeleteClearanceAccessAsync(ClearanceAccessDeleteCmd clearanceAccessDeleteCmd, CancellationToken cancellationToken)
    {
        var clearance = await GetClearanceAccessById(clearanceAccessDeleteCmd.ClearanceAccessId, clearanceAccessDeleteCmd.OrgId);

        if (clearance == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid OrgId or ClearanceAccessId");

        clearance.StateCode = Spd_ClearanceAccess_StateCode.Inactive;
        clearance.StatusCode = Spd_ClearanceAccess_StatusCode.Revoked;

        _context.UpdateObject(clearance);

        _context.SaveChanges();
    }

    private string GetClearanceSortBy(ClearanceAccessSortBy? clearanceSortBy)
    {
        if (clearanceSortBy == null
            || (clearanceSortBy.ExpiresOn != null && (bool)clearanceSortBy.ExpiresOn))
            return "spd_expirydate";

        if (clearanceSortBy.ExpiresOn != null && !(bool)clearanceSortBy.ExpiresOn)
            return "spd_expirydate desc";

        if (clearanceSortBy.NameDesc != null && (bool)clearanceSortBy.NameDesc)
            return "spd_applicantfullname desc";

        if (clearanceSortBy.NameDesc != null && !(bool)clearanceSortBy.NameDesc)
            return "spd_applicantfullname";

        if (clearanceSortBy.CompanyNameDesc != null && (bool)clearanceSortBy.CompanyNameDesc)
            return "spd_contractedcompanyname desc";

        if (clearanceSortBy.CompanyNameDesc != null && !(bool)clearanceSortBy.CompanyNameDesc)
            return "spd_contractedcompanyname";

        return "createdon desc";
    }

    private async Task<Spd_ClearanceAccess?> GetClearanceAccessById(Guid clearanceAccessId, Guid organizationId)
       => _context.Spd_ClearanceAccessSet
            .Where(a => a.Spd_ClearanceAccessId == clearanceAccessId && a.Spd_OrganizationId != null && a.Spd_OrganizationId.Id == organizationId)
            .SingleOrDefault();
}


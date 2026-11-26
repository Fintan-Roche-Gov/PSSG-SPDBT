using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Spd.Resource.Repository.Registration;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;

namespace Spd.Resource.Repository.Org
{
    internal class OrgRepository : IOrgRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _dynaContext;
        private readonly IMapper _mapper;

        public OrgRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper, ILogger<OrgRepository> logger)
        {
            _dynaContext = ctx;
            _mapper = mapper;
        }

        public async Task<OrgQryData?> QueryOrgAsync(OrgQry query, CancellationToken ct)
        {
            return query switch
            {
                OrgsQry q => await GetOrgsAsync(q, ct),
                OrgByIdentifierQry q => await GetOrgByIdentifierAsync(q, ct),
                _ => throw new NotSupportedException($"{query.GetType().Name} is not supported")
            };
        }

        public async Task<OrgManageResult?> ManageOrgAsync(OrgCmd cmd, CancellationToken ct)
        {
            return cmd switch
            {
                OrgUpdateCmd c => await OrgUpdateAsync(c, ct),
                OrgGuidUpdateCmd c => await OrgGuidUpdateAsync(c, ct),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        private async Task<OrgManageResult?> OrgUpdateAsync(OrgUpdateCmd updateOrgCmd, CancellationToken ct)
        {
            Account? org = _dynaContext.AccountSet.Where(p => p.AccountId == updateOrgCmd.Org.Id).FirstOrDefault();
            if (org == null)
            {
                throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the updating org with orgId = {updateOrgCmd.Org.Id}");
            }
            var response = _mapper.Map<OrgResult>(org);

            if (updateOrgCmd.Org.LicenseesNeedVulnerableSectorScreening == null)
            {
                var licenseeIsRequired = false;
                if (response.EmployeeOrganizationTypeCode != null)
                {
                    String[] employeeTypes = new string[] { EmployeeOrganizationTypeCode.Childcare.ToString(), EmployeeOrganizationTypeCode.Healthcare.ToString(), EmployeeOrganizationTypeCode.GovnBody.ToString(), EmployeeOrganizationTypeCode.ProvGovt.ToString() };
                    licenseeIsRequired = employeeTypes.Contains(response.EmployeeOrganizationTypeCode);
                }
                else if (response.VolunteerOrganizationTypeCode != null)
                {
                    String[] volunteerTypes = new string[] { VolunteerOrganizationTypeCode.Childcare.ToString(), VolunteerOrganizationTypeCode.Healthcare.ToString(), VolunteerOrganizationTypeCode.ProvGovt.ToString() };
                    licenseeIsRequired = volunteerTypes.Contains(response.VolunteerOrganizationTypeCode);
                }

                if (licenseeIsRequired == true)
                {
                    throw new ApiException(HttpStatusCode.BadRequest, "'Licensees Need Vulnerable Sector Screening' is required.");
                }
            }

            _mapper.Map(updateOrgCmd.Org, org);
            _dynaContext.SaveChanges();

            return new OrgManageResult(_mapper.Map<OrgResult>(org));
        }

        private async Task<OrgManageResult?> OrgGuidUpdateAsync(OrgGuidUpdateCmd updateOrgGuidCmd, CancellationToken ct)
        {
            Account? org = _dynaContext.AccountSet.Where(a => a.AccountId == updateOrgGuidCmd.OrgId).FirstOrDefault();
            if (org == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the organization.");

            if (updateOrgGuidCmd.OrgGuid != null)
            {
                if (org.Spd_OrgGuid == null)
                {
                    org.Spd_OrgGuid = updateOrgGuidCmd.OrgGuid;
                    _dynaContext.UpdateObject(org);
                    _dynaContext.SaveChanges();
                }
            }
            return new OrgManageResult(_mapper.Map<OrgResult>(org));
        }

        public async Task<bool> CheckDuplicateAsync(SearchOrgQry searchQry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string key;
            if (searchQry.RegistrationTypeCode == RegistrationTypeCode.MDRA)
            {
                key = "MetalDealerRecycler";
            }
            else if (searchQry.RegistrationTypeCode == RegistrationTypeCode.Employee)
            {
                key = $"{searchQry.RegistrationTypeCode}-{searchQry.EmployeeOrganizationTypeCode}";
            }
            else
            {
                key = $"{searchQry.RegistrationTypeCode}-{searchQry.VolunteerOrganizationTypeCode}";
            }
            Spd.Utilities.Dataverse.DynamicsContextLookupHelpers.OrganizationTypeGuidDictionary.TryGetValue(key, out Guid typeGuid);

            if (string.IsNullOrEmpty(searchQry.GenericEmail))
            {
                Account? org = _dynaContext.AccountSet.Where(a =>
                    a.Name == searchQry.OrganizationName &&
                    a.Address1_PostalCode == searchQry.MailingPostalCode &&
                    a.Spd_OrganizationTypeId != null && a.Spd_OrganizationTypeId.Id == typeGuid &&
                    a.StateCode != Account_StateCode.Inactive
                ).FirstOrDefault();
                return org != null;
            }
            else
            {
                // use email in the check
                Account? org = _dynaContext.AccountSet.Where(a =>
                    a.Name == searchQry.OrganizationName &&
                    a.Address1_PostalCode == searchQry.MailingPostalCode &&
                    a.EmailAddress1 == searchQry.GenericEmail &&
                    a.Spd_OrganizationTypeId != null && a.Spd_OrganizationTypeId.Id == typeGuid &&
                    a.StateCode != Account_StateCode.Inactive
                ).FirstOrDefault();
                return org != null;
            }
        }

        private async Task<OrgsQryResult> GetOrgsAsync(OrgsQry query, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            IQueryable<Account> accounts = _dynaContext.AccountSet;
            if (!query.IncludeInactive)
                accounts = accounts.Where(a => a.StateCode != Account_StateCode.Inactive);
            if (query.OrgGuid != null)
                accounts = accounts.Where(a => a.Spd_OrgGuid == query.OrgGuid.ToString());
            if (query.ParentOrgId != null)
                accounts = accounts.Where(a => a.ParentAccountId != null && a.ParentAccountId.Id == query.ParentOrgId);
            if (query.OrgCode != null)
                accounts = accounts.Where(a => a.Spd_OrgCode == query.OrgCode);
            if (query.IsAccreditSchool != null)
                accounts = accounts.Where(a => a.Spd_AccreditedSchool == (query.IsAccreditSchool.Value ? Spd_YesNo.Yes : Spd_YesNo.No));

            List<Account> accountList = accounts.ToList();

            foreach (var account in accountList)
            {
                ct.ThrowIfCancellationRequested();
                _dynaContext.LoadProperty(account, nameof(Account.Spd_Account_Spd_ServiceType));
            }

            if (query.ServiceTypes != null && query.ServiceTypes.Any())
            {
                IEnumerable<Guid> stIds = query.ServiceTypes.Select(t => Spd.Utilities.Dataverse.DynamicsContextLookupHelpers.ServiceTypeGuidDictionary.GetValueOrDefault(t.ToString()));
                var accountsList = accountList
                    .Where(a => stIds.Any(t => a.Spd_Account_Spd_ServiceType.Any(st => st.Spd_ServiceTypeId == t)))
                    .ToList();
                return new OrgsQryResult(_mapper.Map<IEnumerable<OrgResult>>(accountsList));
            }

            return new OrgsQryResult(_mapper.Map<IEnumerable<OrgResult>>(accountList));
        }

        private async Task<OrgQryData?> GetOrgByIdentifierAsync(OrgByIdentifierQry query, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            IQueryable<Account> accounts = _dynaContext.AccountSet.Where(a => a.StateCode != Account_StateCode.Inactive);

            if (query.OrgId != null)
                accounts = accounts.Where(a => a.AccountId == query.OrgId);
            if (query.AccessCode != null)
                accounts = accounts.Where(a => a.Spd_AccessCode == query.AccessCode);

            Account? org = accounts.FirstOrDefault();

            if (org == null) throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find org for {query.OrgId.ToString() ?? query.AccessCode}");

            _dynaContext.LoadProperty(org, nameof(Account.Spd_Account_Spd_ServiceType));
            var serviceTypes = org.Spd_Account_Spd_ServiceType.ToList();

            if (!serviceTypes.Any())
                throw new ApiException(HttpStatusCode.InternalServerError, $"organization {org.Name} does not have service type.");

            var response = _mapper.Map<OrgResult>(org);
            response.ServiceTypes = serviceTypes.Select(s => Enum.Parse<ServiceTypeEnum>(Spd.Utilities.Dataverse.DynamicsContextLookupHelpers.GetServiceTypeName(s.Spd_ServiceTypeId)));
            return new OrgQryResult(response);
        }
    }
}
using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;
namespace Spd.Resource.Repository.Registration
{
    internal class OrgRegistrationRepository : IOrgRegistrationRepository
    {
        private readonly DataverseContext _dynaContext;
        private readonly IMapper _mapper;
        public OrgRegistrationRepository(DataverseContext ctx, IMapper mapper, ILogger<OrgRegistrationRepository> logger)
        {
            _dynaContext = ctx;
            _mapper = mapper;
        }

        public async Task<OrgRegistrationQueryResult> Query(OrgRegistrationQuery query, CancellationToken ct)
        {
            IQueryable<Spd_OrgRegistration> orgRegs = _dynaContext.Spd_OrgRegistrationSet;

            if (!query.IncludeInactive)
                orgRegs = orgRegs.Where(o => o.StateCode != Spd_OrgRegistration_StateCode.Inactive);
            if (query.UserGuid != null)
                orgRegs = orgRegs.Where(o => o.Spd_PortalUserIdentityGuid == query.UserGuid.ToString());
            if (query.OrgGuid != null)
                orgRegs = orgRegs.Where(o => o.Spd_IdentityGuid == query.OrgGuid.ToString());
            if (query.RegistrationNumber != null)
                orgRegs = orgRegs.Where(o => o.Spd_RegistrationNumber == query.RegistrationNumber);

            IEnumerable<Spd_OrgRegistration> results = orgRegs.ToList();
            return new OrgRegistrationQueryResult(_mapper.Map<IEnumerable<OrgRegistrationResult>>(results));
        }

        public async Task<bool> AddRegistrationAsync(CreateOrganizationRegistrationCommand createRegistrationCmd, CancellationToken ct)
        {
            string key;
            if (createRegistrationCmd.OrgRegistration.RegistrationTypeCode == RegistrationTypeCode.Employee)
            {
                key = $"{createRegistrationCmd.OrgRegistration.RegistrationTypeCode}-{createRegistrationCmd.OrgRegistration.EmployeeOrganizationTypeCode}";
            }
            else
            {
                key = $"{createRegistrationCmd.OrgRegistration.RegistrationTypeCode}-{createRegistrationCmd.OrgRegistration.VolunteerOrganizationTypeCode}";
            }

            Spd_OrgRegistration orgregistration = _mapper.Map<Spd_OrgRegistration>(createRegistrationCmd.OrgRegistration);
            _dynaContext.AddObject(orgregistration);

            Guid teamGuid = Guid.Parse(DataverseConstants.Client_Service_Team_Guid);
            Team? serviceTeam = _dynaContext.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
            if (serviceTeam != null)
                orgregistration.OwnerId = serviceTeam.ToEntityReference();

            if (DynamicsContextLookupHelpers.OrganizationTypeGuidDictionary.TryGetValue(key, out Guid organizationTypeGuid))
            {
                Spd_OrganizationType? orgType = _dynaContext.Spd_OrganizationTypeSet
                    .Where(o => o.Spd_OrganizationTypeId == organizationTypeGuid)
                    .FirstOrDefault();

                if (orgType != null)
                    orgregistration.Spd_OrganizationTypeId = orgType.ToEntityReference();
            }

            _dynaContext.UpdateObject(orgregistration);
            _dynaContext.SaveChanges();

            Spd_OrgRegistration? orgReg = _dynaContext.Spd_OrgRegistrationSet.Where(o => o.Spd_OrgRegistrationId == orgregistration.Spd_OrgRegistrationId).FirstOrDefault();
            if (orgReg != null)
            {
                orgReg.Spd_PreregistrationLink = $"{createRegistrationCmd.HostUrl}org-registration/registration/{orgReg.Spd_RegistrationNumber}";
                _dynaContext.UpdateObject(orgReg);
                _dynaContext.SaveChanges();
            }

            return true;
        }

        public async Task<bool> CheckDuplicateAsync(SearchRegistrationQry searchQry, CancellationToken ct)
        {
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

            if (!DynamicsContextLookupHelpers.OrganizationTypeGuidDictionary.TryGetValue(key, out Guid typeGuid))
                return false;

            IQueryable<Spd_OrgRegistration> orgRegs = _dynaContext.Spd_OrgRegistrationSet
                .Where(o => o.Spd_OrganizationName == searchQry.OrganizationName)
                .Where(o => o.Spd_PostalCode == searchQry.MailingPostalCode)
                .Where(o => o.StateCode != Spd_OrgRegistration_StateCode.Inactive)
                .Where(o => o.Spd_OrganizationTypeId != null && o.Spd_OrganizationTypeId.Id == typeGuid);

            if (!String.IsNullOrEmpty(searchQry.GenericEmail))
                orgRegs = orgRegs.Where(o => o.Spd_Email == searchQry.GenericEmail);

            Spd_OrgRegistration? orgReg = orgRegs.FirstOrDefault();
            return orgReg != null;
        }
    }
}

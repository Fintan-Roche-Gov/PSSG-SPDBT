using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Exceptions;
using System.Net;

namespace Spd.Resource.Repository.MDRARegistration;
internal class MDRARegistrationRepository : IMDRARegistrationRepository
{
    protected readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;

    public MDRARegistrationRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper)
    {
        this._context = ctx;
        this._mapper = mapper;
    }

    public async Task<MDRARegistrationCmdResp> CreateMDRARegistrationAsync(CreateMDRARegistrationCmd cmd, CancellationToken ct)
    {
        Spd_OrgRegistration registration = _mapper.Map<Spd_OrgRegistration>(cmd);
        _context.AddObject(registration);
        LinkType(registration);
        LinkTeam(DataverseConstants.Licensing_Client_Service_Team_Guid, registration);
        if (cmd.ExpiredLicenceId != null)
        {
            LinkExpiredLicence(cmd.ExpiredLicenceId.Value, registration);
        }

        List<Spd_Address> addrs = _mapper.Map<List<Spd_Address>>(cmd);
        foreach (Spd_Address addr in addrs)
        {
            _context.AddObject(addr);
            addr.Spd_OrgRegistrationId = registration.ToEntityReference();
        }
        _context.SaveChanges();

        return new MDRARegistrationCmdResp(registration.Spd_OrgRegistrationId!.Value);
    }

    public async Task<MDRARegistrationResp?> GetMDRARegistrationAsync(Guid registrationId, CancellationToken ct)
    {
        Spd_OrgRegistration? registration = _context.Spd_OrgRegistrationSet
            .Where(a => a.Spd_OrgRegistrationId == registrationId)
            .SingleOrDefault();
        if (registration == null)
            return null;
        var resp = _mapper.Map<MDRARegistrationResp>(registration);
        var addrs = _context.Spd_AddressSet.Where(a => a.Spd_OrgRegistrationId != null && a.Spd_OrgRegistrationId.Id == registrationId && a.StateCode == Spd_Address_StateCode.Active)
            .ToList();
        _mapper.Map(addrs, resp);
        return resp;
    }

    private void LinkTeam(string teamGuidStr, Spd_OrgRegistration registration)
    {
        Guid teamGuid = Guid.Parse(teamGuidStr);
        Team? serviceTeam = _context.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
        registration.OwnerId = serviceTeam?.ToEntityReference();
    }

    private void LinkType(Spd_OrgRegistration registration)
    {
        Spd_OrganizationType? type = _context.Spd_OrganizationTypeSet.Where(t => t.Spd_Name == "MetalDealerRecycler").FirstOrDefault();
        registration.Spd_OrganizationTypeId = type?.ToEntityReference();
    }

    private void LinkExpiredLicence(Guid expiredLicenceId, Spd_OrgRegistration registration)
    {
        Guid? serviceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid("MDRA");
        Spd_Licence? licence = _context.Spd_LicenceSet
            .Where(l => l.Spd_LicenceId == expiredLicenceId)
            .Where(l => l.Spd_LicenceType != null && l.Spd_LicenceType.Id == serviceTypeId)
            .FirstOrDefault();
        if (licence == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Not Valid licenceId");
        registration.Spd_CurrentLicence = licence.ToEntityReference();
    }
}

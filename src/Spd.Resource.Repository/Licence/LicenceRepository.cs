using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using CrmContact = Spd.Utilities.Dataverse.EarlyBound.Contact;

namespace Spd.Resource.Repository.Licence;
internal class LicenceRepository : ILicenceRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;
    public LicenceRepository(IDataverseContextFactory ctx,
        IMapper mapper)
    {
        _context = ctx.Create();
        _mapper = mapper;
    }

    public async Task<LicenceBasicResp?> GetBasicAsync(Guid licenceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Spd_Licence? licence = _context.Spd_LicenceSet
            .Where(l => l.Spd_LicenceId == licenceId)
            .FirstOrDefault();

        return _mapper.Map<LicenceBasicResp?>(licence);
    }

    public async Task<LicenceResp?> GetAsync(Guid licenceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Spd_Licence? licence = _context.Spd_LicenceSet
            .Where(l => l.Spd_LicenceId == licenceId)
            .FirstOrDefault();

        if (licence != null)
            LoadLicenceDetails(licence, ct);

        return _mapper.Map<LicenceResp?>(licence);
    }

    public async Task<LicenceListResp> QueryAsync(LicenceQry qry, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        IQueryable<Spd_Licence> lics = _context.Spd_LicenceSet;

        if (!qry.IncludeInactive)
            lics = lics.Where(d => d.StateCode != Spd_Licence_StateCode.Inactive);

        if (qry.IncludeInactive)
            lics = lics.Where(d => d.StatusCode != Spd_Licence_StatusCode.Inactive);

        if (qry.LicenceId != null)
            lics = lics.Where(a => a.Spd_LicenceId == qry.LicenceId);

        if (qry.ContactId != null)
            lics = lics.Where(a => a.Spd_LicenceHolder != null && a.Spd_LicenceHolder.Id == qry.ContactId);

        if (qry.AccountId != null)
            lics = lics.Where(a => a.Spd_LicenceHolder != null && a.Spd_LicenceHolder.Id == qry.AccountId);

        if (qry.LicenceNumber != null)
            lics = lics.Where(a => a.Spd_LicenceNumber == qry.LicenceNumber);

        if (qry.Type != null)
        {
            Guid? serviceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(qry.Type.ToString());
            lics = serviceTypeId.HasValue
                ? lics.Where(l => l.Spd_LicenceType != null && l.Spd_LicenceType.Id == serviceTypeId)
                : lics.Where(l => l.Spd_LicenceType == null);
        }

        if (qry.IsExpired != null)
            lics = qry.IsExpired.Value
                ? lics.Where(l => l.StatusCode == Spd_Licence_StatusCode.Expired)
                : lics.Where(l => l.StatusCode != Spd_Licence_StatusCode.Expired);

        var result = lics.ToList();
        bool hasHolderFilter = qry.AccessCode != null || qry.LastName != null || qry.FirstName != null || qry.BizName != null;
        if (hasHolderFilter)
        {
            foreach (Spd_Licence licence in result)
                LoadLicenceHolder(licence, ct);
        }

        if (qry.AccessCode != null)
        {
            Guid? mdraServiceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.MDRA.ToString());
            Guid? bizServiceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SecurityBusinessLicence.ToString());
            result = result.Where(r =>
                ((r.Spd_LicenceType?.Id == mdraServiceTypeId || r.Spd_LicenceType?.Id == bizServiceTypeId)
                    && r.Spd_Account_Spd_Licence_LicenceHolder?.Spd_AccessCode == qry.AccessCode)
                || r.Spd_Contact_Spd_Licence_LicenceHolder?.Spd_AccessCode == qry.AccessCode).ToList();
        }

        if (qry.LastName != null || qry.FirstName != null)
        {
            result = result.Where(licence =>
                licence.Spd_Contact_Spd_Licence_LicenceHolder?.FirstName == qry.FirstName
                && licence.Spd_Contact_Spd_Licence_LicenceHolder?.LastName == qry.LastName).ToList();
        }

        if (qry.BizName != null)
        {
            result = result.Where(licence =>
                licence.Spd_Account_Spd_Licence_LicenceHolder?.Name?.StartsWith(qry.BizName) == true
                || licence.Spd_Account_Spd_Licence_LicenceHolder?.Spd_OrganizationLegalName?.StartsWith(qry.BizName) == true).ToList();
        }

        foreach (Spd_Licence licence in result)
            LoadLicenceDetails(licence, ct, loadHolder: !hasHolderFilter);

        return new LicenceListResp
        {
            Items = _mapper.Map<IEnumerable<LicenceResp>>(result)
        };
    }

    public async Task<LicenceResp> ManageAsync(UpdateLicenceCmd cmd, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Spd_Licence? lic = _context.Spd_LicenceSet
            .Where(i => i.Spd_LicenceId == cmd.LicenceID)
            .FirstOrDefault();
        if (lic == null)
            throw new ApiException(System.Net.HttpStatusCode.BadRequest, "Invalid licenceId");

        _mapper.Map<PermitLicence, Spd_Licence>(cmd.PermitLicence, lic);
        _context.UpdateObject(lic);
        _context.SaveChanges();

        LoadLicenceDetails(lic, ct);
        return _mapper.Map<LicenceResp>(lic);
    }

    private void LoadLicenceHolder(Spd_Licence licence, CancellationToken ct)
    {
        if (licence.Spd_LicenceHolder == null)
            return;

        if (licence.Spd_LicenceHolder.LogicalName == Account.EntityLogicalName)
        {
            ct.ThrowIfCancellationRequested();
            _context.LoadProperty(licence, nameof(Spd_Licence.Spd_Account_Spd_Licence_LicenceHolder));
        }
        else if (licence.Spd_LicenceHolder.LogicalName == CrmContact.EntityLogicalName)
        {
            ct.ThrowIfCancellationRequested();
            _context.LoadProperty(licence, nameof(Spd_Licence.Spd_Contact_Spd_Licence_LicenceHolder));
        }
    }

    private void LoadLicenceDetails(Spd_Licence licence, CancellationToken ct, bool loadHolder = true)
    {
        if (loadHolder)
            LoadLicenceHolder(licence, ct);

        if (licence.Spd_CaseId != null)
        {
            ct.ThrowIfCancellationRequested();
            _context.LoadProperty(licence, nameof(Spd_Licence.Spd_Incident_Spd_Licence));
        }

        ServiceTypeEnum? serviceType = SharedMappingFuncs.GetServiceType(licence.Spd_LicenceType?.Id);
        if (serviceType != ServiceTypeEnum.SpecialProvincialConstable)
        {
            ct.ThrowIfCancellationRequested();
            _context.LoadProperty(licence, nameof(Spd_Licence.Spd_Spd_Licence_Spd_CaseLicenceCategory_LicenceId));
        }

        ct.ThrowIfCancellationRequested();
        _context.LoadProperty(licence, nameof(Spd_Licence.Spd_Spd_Licence_Spd_LicenceCondition));

        if (serviceType == ServiceTypeEnum.GDSDTeamCertification)
        {
            ct.ThrowIfCancellationRequested();
            _context.LoadProperty(licence, nameof(Spd_Licence.Spd_Licence_Spd_DogTeam_LicenceId));
        }
    }
}


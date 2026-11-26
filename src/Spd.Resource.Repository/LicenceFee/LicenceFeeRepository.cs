using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.LicenceFee;

internal class LicenceFeeRepository : ILicenceFeeRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;

    public LicenceFeeRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<LicenceFeeListResp> QueryAsync(LicenceFeeQry qry, CancellationToken cancellationToken)
    {
        IEnumerable<Spd_LicenceFee> feeResult = _context.Spd_LicenceFeeSet.ToList();

        if (!qry.IncludeInactive)
            feeResult = feeResult.Where(d => d.StateCode != Spd_LicenceFee_StateCode.Inactive);

        if (qry.ServiceTypeEnum != null)
        {
            DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(qry.ServiceTypeEnum.ToString()!, out Guid stGuid);
            feeResult = feeResult.Where(f => f.Spd_ServiceTypeId != null && f.Spd_ServiceTypeId.Id == stGuid);
        }

        if (qry.LicenceTermEnum != null)
        {
            Spd_LicenceTerm term = Enum.Parse<Spd_LicenceTerm>(qry.LicenceTermEnum.ToString()!);
            feeResult = feeResult.Where(f => f.Spd_Term == term);
        }

        if (qry.ApplicationTypeEnum != null)
        {
            Spd_LicenceApplicationType type = Enum.Parse<Spd_LicenceApplicationType>(qry.ApplicationTypeEnum.ToString()!);
            feeResult = feeResult.Where(f => f.Spd_Type == type);
        }

        if (qry.BizTypeEnum != null)
        {
            Spd_BusinessType bizType = Enum.Parse<Spd_BusinessType>(qry.BizTypeEnum.ToString()!);
            feeResult = feeResult.Where(f => f.Spd_BusinessType == bizType);
        }

        if (qry.HasValidSwl90DayLicence != null)
        {
            Spd_YesNo hasValidSwl90Day = qry.HasValidSwl90DayLicence.Value ? Spd_YesNo.Yes : Spd_YesNo.No;
            feeResult = feeResult.Where(f => f.Spd_HasValidSwL90DayLicence == hasValidSwl90Day);
        }

        return new LicenceFeeListResp
        {
            LicenceFees = _mapper.Map<IEnumerable<LicenceFeeResp>>(feeResult.ToList())
        };
    }
}

using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.LicenceFee;

internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<Spd_LicenceFee, LicenceFeeResp>()
         .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => Enum.Parse<ServiceTypeEnum>(DataverseContextLookupHelpers.GetServiceTypeName(s.Spd_ServiceTypeId != null ? s.Spd_ServiceTypeId.Id : null))))
         .ForMember(d => d.BizTypeCode, opt => opt.MapFrom(s => s.Spd_BusinessType != null ? Enum.Parse<BizTypeEnum>(s.Spd_BusinessType.ToString()) : (BizTypeEnum?)null))
         .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s => s.Spd_Type != null ? Enum.Parse<ApplicationTypeEnum>(s.Spd_Type.ToString()) : (ApplicationTypeEnum?)null))
         .ForMember(d => d.LicenceTermCode, opt => opt.MapFrom(s => s.Spd_Term != null ? Enum.Parse<LicenceTermEnum>(s.Spd_Term.ToString()) : (LicenceTermEnum?)null))
         .ForMember(d => d.HasValidSwl90DayLicence, opt => opt.MapFrom(s => s.Spd_HasValidSwL90DayLicence == Spd_YesNo.Yes))
         .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Spd_Amount != null ? s.Spd_Amount.Value : (decimal?)null));
    }
}


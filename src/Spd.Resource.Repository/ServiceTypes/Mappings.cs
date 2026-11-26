using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.ServiceTypes;

internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<Spd_ServiceType, ServiceTypeResp>()
         .ForMember(d => d.ServiceTypeName, opt => opt.MapFrom(s => s.Spd_ServiceTypeName))
         .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_ServiceTypeId))
         .ForMember(d => d.ServiceCategoryCode, opt => opt.MapFrom(s => GetServiceTypeCategoryEnum(s.Spd_ServiceCategory)))
         .ForMember(d => d.ScreeningCost, opt => opt.MapFrom(s => s.Spd_ScreeningCost.Value));
    }

    internal static ServiceTypeCategoryEnum? GetServiceTypeCategoryEnum(Spd_ServiceCategory? serviceCategory)
    {
        if (serviceCategory == null)
            return null;
        return serviceCategory switch
        {
            Spd_ServiceCategory.Screening => ServiceTypeCategoryEnum.Screening,
            Spd_ServiceCategory.Licensing => ServiceTypeCategoryEnum.Licensing,
            _ => throw new ArgumentException("invalid int application type option set")
        };
    }
}


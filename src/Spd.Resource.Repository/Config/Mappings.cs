using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Config
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Bcgov_Config, ConfigItem>()
             .ForMember(d => d.Key, opt => opt.MapFrom(s => s.Bcgov_Key))
             .ForMember(d => d.Group, opt => opt.MapFrom(s => s.Bcgov_Group))
             .ForMember(d => d.Value, opt => opt.MapFrom(s => s.Bcgov_Value));
        }
    }
}

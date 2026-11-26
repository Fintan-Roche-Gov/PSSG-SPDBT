using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Alias;
internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<AliasResp, Spd_Alias>()
        .ForMember(d => d.Spd_AliasId, opt => opt.MapFrom(s => Guid.NewGuid()))
        .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
        .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.Surname))
        .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName1))
        .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => s.MiddleName2))
        .ForMember(d => d.Spd_Source, opt => opt.MapFrom(s => Spd_Alias_Spd_Source.UserEntered));

        _ = CreateMap<Spd_Alias, AliasResp>()
         .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_AliasId))
         .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.Spd_FirstName))
         .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.Spd_Surname))
         .ForMember(d => d.MiddleName1, opt => opt.MapFrom(s => s.Spd_MiddleName1))
         .ForMember(d => d.MiddleName2, opt => opt.MapFrom(s => s.Spd_MiddleName2));
    }
}

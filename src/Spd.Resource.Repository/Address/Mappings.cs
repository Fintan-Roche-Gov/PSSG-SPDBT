using AutoMapper;
using Spd.Resource.Repository.Biz;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Address;
internal class Mappings : Profile
{
    public Mappings()
    {
        CreateMap<Spd_Address, AddressResp>()
        .ForMember(d => d.AddressLine1, opt => opt.MapFrom(s => s.Spd_Address1))
        .ForMember(d => d.AddressLine2, opt => opt.MapFrom(s => s.Spd_Address2))
        .ForMember(d => d.City, opt => opt.MapFrom(s => s.Spd_City))
        .ForMember(d => d.Province, opt => opt.MapFrom(s => s.Spd_ProvinceState))
        .ForMember(d => d.PostalCode, opt => opt.MapFrom(s => s.Spd_PostalCode))
        .ForMember(d => d.Country, opt => opt.MapFrom(s => s.Spd_Country))
        .ForMember(d => d.BranchId, opt => opt.MapFrom(s => s.Spd_AddressId))
        .ForMember(d => d.BranchPhoneNumber, opt => opt.MapFrom(s => s.Spd_BranchPhone))
        .ForMember(d => d.BranchEmailAddr, opt => opt.MapFrom(s => s.Spd_BranchEmail));

        CreateMap<Spd.Resource.Repository.Biz.BranchAddr, Spd_Address>()
            .ForMember(d => d.Spd_Address1, opt => opt.MapFrom(s => s.AddressLine1))
            .ForMember(d => d.Spd_Address2, opt => opt.MapFrom(s => s.AddressLine2))
            .ForMember(d => d.Spd_City, opt => opt.MapFrom(s => s.City))
            .ForMember(d => d.Spd_ProvinceState, opt => opt.MapFrom(s => s.Province))
            .ForMember(d => d.Spd_PostalCode, opt => opt.MapFrom(s => s.PostalCode))
            .ForMember(d => d.Spd_Country, opt => opt.MapFrom(s => s.Country))
            .ForMember(d => d.Spd_BranchPhone, opt => opt.MapFrom(s => s.BranchPhoneNumber))
            .ForMember(d => d.Spd_BranchEmail, opt => opt.MapFrom(s => s.BranchEmailAddr));
    }
}

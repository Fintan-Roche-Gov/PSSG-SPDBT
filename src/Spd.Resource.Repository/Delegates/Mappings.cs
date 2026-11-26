using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Delegates
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<CreateDelegateCmd, Spd_Delegate>()
            .ForMember(d => d.Spd_DelegateId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Spd_Role, opt => opt.MapFrom(s => Enum.Parse<Spd_Delegate_Spd_Role>(s.PSSOUserRoleCode.ToString())));

            _ = CreateMap<Spd_Delegate, DelegateResp>()
            .ForMember(d => d.PSSOUserRoleCode, opt => opt.MapFrom(s => Enum.Parse<PSSOUserRoleEnum>(s.Spd_Role.ToString())))
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_DelegateId))
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.Spd_PortalUser_Spd_Delegate.Spd_FirstName))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.Spd_PortalUser_Spd_Delegate.Spd_Surname))
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Spd_PortalUser_Spd_Delegate.Spd_EmailAddress1))
            .ForMember(d => d.PortalUserId, opt => opt.MapFrom(s => s.Spd_PortalUserId.Id));
        }
    }
}

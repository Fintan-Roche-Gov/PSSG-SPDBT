using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.PortalUser
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Spd_PortalUser, PortalUserResp>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_PortalUserId))
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.Spd_FirstName))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.Spd_Surname))
            .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.Spd_EmailAddress1))
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.Spd_PhoneNumber))
            .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Spd_JobTitle))
            .ForMember(d => d.IdentityId, opt => opt.Ignore())
            .ForMember(d => d.IsPSA, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_PSa)))
            .ForMember(d => d.OrganizationId, opt => opt.Ignore())
            .ForMember(d => d.ContactRoleCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetContactRoleCode(s.Spd_Spd_Role_Spd_PortalUser)))
            .ForMember(d => d.IsFirstTimeLogin, opt => opt.MapFrom(s => s.Spd_LastLoggedIn == null))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.Spd_IdentityId != null));

            _ = CreateMap<CreatePortalUserCmd, Spd_PortalUser>()
            .ForMember(d => d.Spd_PortalUserId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.LastName))
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.FirstName))
            .ForMember(d => d.Spd_EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Spd_JobTitle, opt => opt.MapFrom(s => s.JobTitle))
            .ForMember(d => d.Spd_ServiceCategory, opt => opt.MapFrom(s =>
                s.PortalUserServiceCategory == PortalUserServiceCategoryEnum.Licensing ?
                Spd_ServiceCategory.Licensing : Spd_ServiceCategory.Screening));

            _ = CreateMap<CreatePortalUserCmd, Spd_PortalInvitation>()
            .ForMember(d => d.Spd_PortalInvitationId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Spd_OrganizationId, opt => opt.Ignore())
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.FirstName))
            .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.LastName))
            .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Spd_JobTitle, opt => opt.MapFrom(s => s.JobTitle))
            .ForMember(d => d.Spd_InvitationType, opt => opt.MapFrom(s => Spd_PortalInvitation_Spd_InvitationType.PortalUser));
        }
    }
}

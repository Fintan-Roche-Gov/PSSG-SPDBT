using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.User
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<User, Spd_PortalUser>()
            .ForMember(d => d.Spd_PortalUserId, opt => opt.Ignore())
            .ForMember(d => d.Spd_OrganizationId, opt => opt.Ignore())
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.FirstName))
            .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.LastName))
            .ForMember(d => d.Spd_EmailAddress1, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.Spd_JobTitle, opt => opt.MapFrom(s => s.JobTitle))
            .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Spd_ServiceCategory, opt => opt.MapFrom(s => Spd_ServiceCategory.Screening))
            .ReverseMap()
            .ForMember(d => d.ContactAuthorizationTypeCode, opt => opt.MapFrom(s => GetAuthorizationTypeCode(s.Spd_Spd_Role_Spd_PortalUser.ToList())))
            .ForMember(d => d.OrganizationId, opt => opt.MapFrom(s => s.Spd_OrganizationId != null ? s.Spd_OrganizationId.Id : (Guid?)null));

            _ = CreateMap<Spd_PortalUser, UserResult>()
            .IncludeBase<Spd_PortalUser, User>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_PortalUserId))
            .ForMember(d => d.OrgRegistrationId, opt => opt.MapFrom(s => s.Spd_OrgRegistrationId != null ? s.Spd_OrgRegistrationId.Id : (Guid?)null))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.Spd_IdentityId != null))
            .ForMember(d => d.IsFirstTimeLogin, opt => opt.MapFrom(s => !s.Spd_LastLoggedIn.HasValue))
            .ForMember(d => d.UserGuid, opt => opt.Ignore());

            _ = CreateMap<User, Spd_PortalInvitation>()
            .ForMember(d => d.Spd_PortalInvitationId, opt => opt.Ignore())
            .ForMember(d => d.Spd_OrganizationId, opt => opt.Ignore())
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.FirstName))
            .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.LastName))
            .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.Spd_JobTitle, opt => opt.MapFrom(s => s.JobTitle))
            .ForMember(d => d.Spd_InvitationType, opt => opt.MapFrom(s => Spd_PortalInvitation_Spd_InvitationType.PortalUser));
        }

        private ContactRoleCode GetAuthorizationTypeCode(ICollection<Spd_Role> roles)
        {
            Guid? roleId = roles?.FirstOrDefault()?.Spd_RoleId;
            if (roleId == null) return ContactRoleCode.Contact;
            return
                Enum.Parse<ContactRoleCode>(
                    global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.RoleGuidDictionary.FirstOrDefault(x => x.Value == roleId).Key);

        }
    }
}

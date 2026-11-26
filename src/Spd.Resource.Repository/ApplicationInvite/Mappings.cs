using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;

namespace Spd.Resource.Repository.ApplicationInvite
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<ApplicationInvite, Spd_PortalInvitation>()
            .ForMember(d => d.Spd_PortalInvitationId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.FirstName)))
            .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.LastName)))
            .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.Spd_JobTitle, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.JobTitle)))
            .ForMember(d => d.Spd_InvitationType, opt => opt.MapFrom(s => Spd_PortalInvitation_Spd_InvitationType.ScreeningRequest))
            .ForMember(d => d.Spd_ScreeningRequestType, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ScreenTypeEnum, Spd_ScreeningRequestType>(s.ScreeningType)))
            .ForMember(d => d.Spd_Views, opt => opt.MapFrom(s => 0))
            .ForMember(d => d.Spd_PayeeType, opt => opt.MapFrom(s => GetPayeeType(s.PayeeType)))
            .ReverseMap()
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.Spd_FirstName))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.Spd_Surname))
            .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Spd_JobTitle));

            _ = CreateMap<Spd_PortalInvitation, ApplicationInviteResult>()
            .IncludeBase<Spd_PortalInvitation, ApplicationInvite>()
            .ForMember(d => d.PayeeType, opt => opt.MapFrom(s => SharedMappingFuncs.GetPayeeType((int?)s.Spd_PayeeType)))
            .ForMember(d => d.OrgId, opt => opt.MapFrom(s => s.Spd_OrganizationId != null ? s.Spd_OrganizationId.Id : (Guid?)null))
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_PortalInvitationId ?? Guid.Empty))
            .ForMember(d => d.ErrorMsg, opt => opt.MapFrom(s => s.Spd_ErrorMessage))
            .ForMember(d => d.CreatedByUserId, opt => opt.MapFrom(s => s.Spd_InvitedBy != null ? s.Spd_InvitedBy.Id : (Guid?)null))
            .ForMember(d => d.CreatedOn, opt => opt.MapFrom(s => s.CreatedOn ?? DateTime.MinValue))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.StatusCode == null ? ApplicationInviteStatusEnum.Draft : Enum.Parse<ApplicationInviteStatusEnum>(s.StatusCode.ToString())))
            .ForMember(d => d.Viewed, opt => opt.MapFrom(s => s.Spd_Views != null && s.Spd_Views > 0));

            _ = CreateMap<Spd_PortalInvitation, AppInviteVerifyResp>()
            .ForMember(d => d.AppInviteId, opt => opt.MapFrom(s => s.Spd_PortalInvitationId ?? Guid.Empty))
            .ForMember(d => d.PayeeType, opt => opt.MapFrom(s => SharedMappingFuncs.GetPayeeType((int?)s.Spd_PayeeType)))
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Spd_Email))
            .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.Spd_FirstName))
            .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.Spd_Surname))
            .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Spd_JobTitle))
            .ForMember(d => d.ScreeningType, opt => opt.MapFrom(s => GetScreenType((int?)s.Spd_ScreeningRequestType)))
            .ForMember(d => d.ServiceType, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s.Spd_ServiceTypeId != null ? s.Spd_ServiceTypeId.Id : (Guid?)null)));
        }

        private static Spd_PayerType? GetPayeeType(PayerPreferenceTypeCode? code)
        {
            var payeeType = SharedMappingFuncs.GetPayeeTypeCode(code);
            return payeeType == null ? null : (Spd_PayerType?)payeeType;
        }

        private static ScreenTypeEnum? GetScreenType(int? code)
        {
            if (code == null) return null;
            return SharedMappingFuncs.GetEnum<Spd_ScreeningRequestType, ScreenTypeEnum>(code);
        }
    }
}

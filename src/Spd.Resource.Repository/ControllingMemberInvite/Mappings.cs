using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;

namespace Spd.Resource.Repository.ControllingMemberInvite
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<ControllingMemberInvite, Spd_PortalInvitation>()
            .ForMember(d => d.Spd_PortalInvitationId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.GivenName)))
            .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.Surname)))
            .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Spd_InvitationType, opt => opt.MapFrom(s => s.InviteTypeCode == ControllingMemberAppInviteTypeEnum.New ? Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc : Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrcUpdate))
            .ForMember(d => d.Spd_Views, opt => opt.MapFrom(s => 0))
            .ForMember(d => d.Spd_PayeeType, opt => opt.MapFrom(s => Spd_PayerType.Applicant))
            .ReverseMap()
            .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.Spd_FirstName))
            .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.Spd_Surname))
            .ForMember(d => d.InviteTypeCode, opt => opt.MapFrom(s => s.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc ? ControllingMemberAppInviteTypeEnum.New : ControllingMemberAppInviteTypeEnum.Update));

            _ = CreateMap<Spd_PortalInvitation, ControllingMemberInviteResp>()
            .IncludeBase<Spd_PortalInvitation, ControllingMemberInvite>()
            .ForMember(d => d.BizId, opt => opt.Ignore())
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_PortalInvitationId))
            .ForMember(d => d.ErrorMsg, opt => opt.MapFrom(s => s.Spd_ErrorMessage))
            .ForMember(d => d.CreatedByUserId, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.StatusCode == null ? ApplicationInviteStatusEnum.Draft : Enum.Parse<ApplicationInviteStatusEnum>(s.StatusCode.ToString())))
            .ForMember(d => d.Viewed, opt => opt.MapFrom(s => s.Spd_Views != null && s.Spd_Views > 0));

            _ = CreateMap<Spd_PortalInvitation, ControllingMemberInviteVerifyResp>()
            .ForMember(d => d.BizId, opt => opt.Ignore())
            .ForMember(d => d.BizContactId, opt => opt.Ignore())
            .ForMember(d => d.InviteId, opt => opt.MapFrom(s => s.Spd_PortalInvitationId))
            .ForMember(d => d.InviteTypeCode, opt => opt.MapFrom(s => s.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc ? ControllingMemberAppInviteTypeEnum.New : ControllingMemberAppInviteTypeEnum.Update));
        }
    }
}

using AutoMapper;
using Spd.Resource.Repository.BizLicApplication;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.BizContact
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<BizContact, Spd_BusinessContact>()
                .ForMember(d => d.Spd_Role, opt => opt.MapFrom(s => Enum.Parse<Spd_BusinessContact_Spd_Role>(s.BizContactRoleCode.ToString())))
                .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName1))
                .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
                .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.Surname))
                .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => s.MiddleName2))
                .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.EmailAddress))
                .ForMember(d => d.Spd_FullName, opt => opt.MapFrom(s => $"{s.Surname}, {s.GivenName}"))
                .ReverseMap();

            _ = CreateMap<Spd_BusinessContact, BizContactResp>()
                .IncludeBase<Spd_BusinessContact, BizContact>()
                .ForMember(d => d.BizContactId, opt => opt.MapFrom(s => s.Spd_BusinessContactId))
                .ForMember(d => d.BizContactRoleCode, opt => opt.MapFrom(s => s.Spd_Role != null ? Enum.Parse<BizContactRoleEnum>(s.Spd_Role.ToString()) : BizContactRoleEnum.ControllingMember))
                .ForMember(d => d.PositionCodes, opt => opt.MapFrom(s => GetPositions(s.Spd_Position_Spd_BusinessContact.ToArray())))
                .ForMember(d => d.ContactId, opt => opt.MapFrom(s => s.Spd_ContactId != null ? s.Spd_ContactId.Id : (Guid?)null))
                .ForMember(d => d.LicenceId, opt => opt.MapFrom(s => s.Spd_SwLNumber != null ? s.Spd_SwLNumber.Id : (Guid?)null))
                .ForMember(d => d.BizId, opt => opt.MapFrom(s => s.Spd_OrganizationId != null ? s.Spd_OrganizationId.Id : Guid.Empty))
                .ForMember(d => d.LatestControllingMemberInvitationId, opt => opt.MapFrom(s => GetLastestControllingMemberInvite(s.Spd_BusinessContact_Spd_PortalInvitation.ToList()).InviteId))
                .ForMember(d => d.LatestControllingMemberInvitationStatusEnum, opt => opt.MapFrom(s => GetLastestControllingMemberInvite(s.Spd_BusinessContact_Spd_PortalInvitation.ToList()).InviteStatus))
                .ForMember(d => d.LatestControllingMemberCrcAppId, opt => opt.MapFrom(s => GetLastestControllingMemberCrcApp(s.Spd_BusinessContact_Spd_Application.ToList()).AppId))
                .ForMember(d => d.LatestControllingMemberCrcAppPortalStatusEnum, opt => opt.MapFrom(s => GetLastestControllingMemberCrcApp(s.Spd_BusinessContact_Spd_Application.ToList()).PortalStatus));
        }

        private static (Guid? AppId, ApplicationPortalStatusEnum? PortalStatus) GetLastestControllingMemberCrcApp(IEnumerable<Spd_Application> apps)
        {
            Guid? cmServiceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SECURITY_BUSINESS_LICENCE_CONTROLLING_MEMBER_CRC.ToString());
            Spd_Application? app = apps
                .Where(a => a.Spd_ServiceTypeId != null && a.Spd_ServiceTypeId.Id == cmServiceTypeGuid)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefault();
            if (app == null) return (null, null);
            if (app.Spd_PortalStatus != Spd_PortalStatus.Draft &&
                app.Spd_PortalStatus != Spd_PortalStatus.VerifyIdentity &&
                app.Spd_PortalStatus != Spd_PortalStatus.Incomplete &&
                app.Spd_PortalStatus != Spd_PortalStatus.AwaitingPayment)
                return (null, null);
            else
            {
                if (app.Spd_PortalStatus == null) return (app.Spd_ApplicationId, null);
                else
                {
                    ApplicationPortalStatusEnum statusEnum = Enum.Parse<ApplicationPortalStatusEnum>(app.Spd_PortalStatus.ToString());
                    return (app.Spd_ApplicationId, statusEnum);
                }
            }
        }

        private static (Guid? InviteId, ApplicationInviteStatusEnum? InviteStatus) GetLastestControllingMemberInvite(IEnumerable<Spd_PortalInvitation> invites)
        {
            Spd_PortalInvitation? invite = invites.Where(i => i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc)
                .OrderByDescending(app => app.CreatedOn)
                .FirstOrDefault();
            if (invite == null) return (null, null);
            else
            {
                if (invite.StatusCode == null) return (invite.Spd_PortalInvitationId, null);
                else
                {
                    ApplicationInviteStatusEnum statusEnum = Enum.Parse<ApplicationInviteStatusEnum>(invite.StatusCode.ToString());
                    return (invite.Spd_PortalInvitationId, statusEnum);
                }
            }
        }

        private static IEnumerable<PositionEnum> GetPositions(Spd_Position[] positions)
        {
            List<PositionEnum> results = new List<PositionEnum>();
            foreach (Spd_Position position in positions)
            {
                string? posStr = position.Spd_Name;
                if (posStr != null)
                {
                    string normalized = posStr.Replace(" ", string.Empty).Replace("-", string.Empty);
                    if (Enum.TryParse<PositionEnum>(normalized, out var parsed))
                    {
                        results.Add(parsed);
                    }
                }
            }
            return results;
        }
    }
}

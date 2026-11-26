using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.ControllingMemberInvite
{
    internal class ControllingMemberInviteRepository : IControllingMemberInviteRepository
    {
        private readonly DataverseContext _dynaContext;
        private readonly IMapper _mapper;
        private readonly ITimeLimitedDataProtector _dataProtector;
        public ControllingMemberInviteRepository(DataverseContext ctx, IMapper mapper, ILogger<ControllingMemberInviteRepository> logger, IDataProtectionProvider dpProvider)
        {
            _dynaContext = ctx;
            _mapper = mapper;
            _dataProtector = dpProvider.CreateProtector(nameof(ControllingMemberInviteCreateCmd)).ToTimeLimitedDataProtector();
        }

        public async Task<IEnumerable<ControllingMemberInviteResp>> QueryAsync(ControllingMemberInviteQuery query, CancellationToken cancellationToken)
        {
            var invites = _dynaContext.Spd_PortalInvitationSet
                 .Where(i => i.StateCode == Spd_PortalInvitation_StateCode.Active)
                 .Where(i => i.Spd_InvitationType != null && (i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc || i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrcUpdate));

            invites = invites.Where(i => i.Spd_BusinessContact != null && i.Spd_BusinessContact.Id == query.BizContactId);
            if (!query.IncludeInactive)
                invites = invites.Where(a => a.StateCode != Spd_PortalInvitation_StateCode.Inactive);

            return _mapper.Map<IEnumerable<ControllingMemberInviteResp>>(invites.ToList());
        }

        public async System.Threading.Tasks.Task ManageAsync(ControllingMemberInviteCmd cmd, CancellationToken ct)
        {
            if (cmd is ControllingMemberInviteCreateCmd)
                await CreateControllingMemberInviteAsync((ControllingMemberInviteCreateCmd)cmd, ct);
            else if (cmd is ControllingMemberInviteUpdateCmd)
                await UpdateApplicationInvitesAsync((ControllingMemberInviteUpdateCmd)cmd, ct);
        }

        public async System.Threading.Tasks.Task CreateControllingMemberInviteAsync(ControllingMemberInviteCreateCmd createInviteCmd, CancellationToken ct)
        {
            Spd_PortalUser? user = null;
            if (createInviteCmd.CreatedByUserId != null)
                user = _dynaContext.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == createInviteCmd.CreatedByUserId).FirstOrDefault();
            Account? biz = _dynaContext.AccountSet.Where(a => a.AccountId == createInviteCmd.BizId).FirstOrDefault();
            Spd_BusinessContact? bizContact = _dynaContext.Spd_BusinessContactSet.Where(bc => bc.Spd_BusinessContactId == createInviteCmd.BizContactId).FirstOrDefault();
            Spd_PortalInvitation invitation = _mapper.Map<Spd_PortalInvitation>(createInviteCmd);
            var encryptedInviteId = WebUtility.UrlEncode(_dataProtector.Protect(invitation.Spd_PortalInvitationId.ToString(), DateTimeOffset.UtcNow.AddDays(SpdConstants.ApplicationInviteValidDays)));
            invitation.Spd_InvitationLink = $"{createInviteCmd.HostUrl}{SpdConstants.BizPortalControllingMemberInviteLink}{encryptedInviteId}";
            _dynaContext.AddObject(invitation);
            if (biz != null) invitation.Spd_OrganizationId = biz.ToEntityReference();
            if (user != null) invitation.Spd_InvitedBy = user.ToEntityReference();
            if (bizContact != null) invitation.Spd_BusinessContact = bizContact.ToEntityReference();
            Guid? serviceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SECURITY_BUSINESS_LICENCE_CONTROLLING_MEMBER_CRC.ToString());
            Spd_ServiceType? servicetype = serviceTypeId == null ? null : _dynaContext.Spd_ServiceTypeSet.Where(s => s.Spd_ServiceTypeId == serviceTypeId).FirstOrDefault();
            if (servicetype != null)
            {
                invitation.Spd_ServiceTypeId = servicetype.ToEntityReference();
            }
            _dynaContext.SaveChanges();
        }

        private async System.Threading.Tasks.Task UpdateApplicationInvitesAsync(ControllingMemberInviteUpdateCmd cmInviteUpdateCmd, CancellationToken cancellationToken)
        {
            Spd_PortalInvitation? invite = _dynaContext.Spd_PortalInvitationSet
                 .Where(i => i.StateCode == Spd_PortalInvitation_StateCode.Active)
                 .Where(i => i.Spd_InvitationType != null && (i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc || i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrcUpdate))
                 .Where(i => i.Spd_PortalInvitationId == cmInviteUpdateCmd.ControllingMemberInviteId)
                 .FirstOrDefault();
            if (invite == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Invalid invite id");

            // Inactivate the invite
            invite.StateCode = Spd_PortalInvitation_StateCode.Inactive;
            invite.StatusCode = cmInviteUpdateCmd.ApplicationInviteStatusEnum switch
            {
                ApplicationInviteStatusEnum.Cancelled => Spd_PortalInvitation_StatusCode.Cancelled,
                ApplicationInviteStatusEnum.Completed => Spd_PortalInvitation_StatusCode.Completed,
                ApplicationInviteStatusEnum.Expired => Spd_PortalInvitation_StatusCode.Expired,
                ApplicationInviteStatusEnum.Sent => Spd_PortalInvitation_StatusCode.Sent,
                _ => Spd_PortalInvitation_StatusCode.Draft,
            };
            _dynaContext.UpdateObject(invite);

            _dynaContext.SaveChanges();
        }

        public async Task<ControllingMemberInviteVerifyResp> VerifyControllingMemberInviteAsync(ControllingMemberInviteVerifyCmd verifyInviteCmd, CancellationToken ct)
        {
            Guid inviteId;
            try
            {
                string inviteIdStr = _dataProtector.Unprotect(WebUtility.UrlDecode(verifyInviteCmd.EncryptedInviteId));
                inviteId = Guid.Parse(inviteIdStr);
            }
            catch
            {
                throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");
            }
            var invite = _dynaContext.Spd_PortalInvitationSet
                .Where(i => i.Spd_PortalInvitationId == inviteId)
                .Where(i => i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrc || i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ControllingMemberCrcUpdate)
                .Where(i => i.StateCode != Spd_PortalInvitation_StateCode.Inactive)
                .FirstOrDefault();
            if (invite == null)
                throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");

            //set invite views
            invite.Spd_Views = (invite.Spd_Views ?? 0) + 1;
            _dynaContext.UpdateObject(invite);
            _dynaContext.SaveChanges();
            return _mapper.Map<ControllingMemberInviteVerifyResp>(invite);
        }
    }
}

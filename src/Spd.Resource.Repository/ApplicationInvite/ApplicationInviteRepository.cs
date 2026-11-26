using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.ApplicationInvite
{
    internal class ApplicationInviteRepository : IApplicationInviteRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _dynaContext;
        private readonly IMapper _mapper;
        private readonly ITimeLimitedDataProtector _dataProtector;
        public ApplicationInviteRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper, ILogger<ApplicationInviteRepository> logger, IDataProtectionProvider dpProvider)
        {
            _dynaContext = ctx;
            _mapper = mapper;
            _dataProtector = dpProvider.CreateProtector(nameof(ApplicationInvitesCreateCmd)).ToTimeLimitedDataProtector();
        }

        public async Task<ApplicationInviteListResp> QueryAsync(ApplicationInviteQuery query, CancellationToken cancellationToken)
        {
            if (query == null || (query.FilterBy?.OrgId == null && query.FilterBy.ParentOrgId == null && query.FilterBy.AppInviteId == null))
                throw new ArgumentNullException("query.FilterBy.OrgId", "Must query applications by organization id or service type or appInviteId.");

            IQueryable<Spd_PortalInvitation> invites = _dynaContext.Spd_PortalInvitationSet
                .Where(i => i.StateCode == Spd_PortalInvitation_StateCode.Active)
                .Where(i => i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ScreeningRequest);

            if (query.FilterBy.AppInviteId != null)
                invites = invites.Where(i => i.Spd_PortalInvitationId == query.FilterBy.AppInviteId);

            if (query.FilterBy.OrgId != null)
                invites = invites.Where(i => i.Spd_OrganizationId != null && i.Spd_OrganizationId.Id == query.FilterBy.OrgId);

            if (query.FilterBy.CreatedByUserId != null)
                invites = invites.Where(i => i.Spd_InvitedBy != null && i.Spd_InvitedBy.Id == query.FilterBy.CreatedByUserId);

            List<Spd_PortalInvitation> list = invites.ToList();
            foreach (Spd_PortalInvitation invite in list)
            {
                _dynaContext.LoadProperty(invite, nameof(Spd_PortalInvitation.Spd_OrgAgNIZATion_Spd_PortalInvitation));
            }

            if (query.FilterBy.ParentOrgId != null)
            {
                list = list.Where(i =>
                    i.Spd_OrgAgNIZATion_Spd_PortalInvitation != null &&
                    i.Spd_OrgAgNIZATion_Spd_PortalInvitation.ParentAccountId != null &&
                    i.Spd_OrgAgNIZATion_Spd_PortalInvitation.ParentAccountId.Id == query.FilterBy.ParentOrgId).ToList();
            }

            string? filterValue = query.FilterBy.EmailOrNameContains;
            if (!string.IsNullOrWhiteSpace(filterValue))
            {
                list = list.Where(i =>
                        (!string.IsNullOrWhiteSpace(i.Spd_FirstName) && i.Spd_FirstName.Contains(filterValue, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(i.Spd_Surname) && i.Spd_Surname.Contains(filterValue, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(i.Spd_Email) && i.Spd_Email.Contains(filterValue, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            list = query.SortBy?.SubmittedDateDesc == false
                ? list.OrderBy(a => a.CreatedOn).ToList()
                : list.OrderByDescending(a => a.CreatedOn).ToList();

            int totalCount = list.Count;

            if (query.Paging != null)
            {
                list = list
                    .Skip(query.Paging.Page * query.Paging.PageSize)
                    .Take(query.Paging.PageSize)
                    .ToList();
            }

            ApplicationInviteListResp response = new ApplicationInviteListResp
            {
                ApplicationInvites = _mapper.Map<IEnumerable<ApplicationInviteResult>>(list)
            };

            if (query.Paging != null)
            {
                response.Pagination = new PaginationResp
                {
                    PageSize = query.Paging.PageSize,
                    PageIndex = query.Paging.Page,
                    Length = totalCount
                };
            }

            return await Task.FromResult(response);
        }

        public async Task ManageAsync(ApplicationInviteCmd cmd, CancellationToken ct)
        {
            if (cmd is ApplicationInvitesCreateCmd)
                await AddApplicationInvitesAsync((ApplicationInvitesCreateCmd)cmd, ct);
            else if (cmd is ApplicationInviteUpdateCmd)
                await UpdateApplicationInvitesAsync((ApplicationInviteUpdateCmd)cmd, ct);
        }

        private async Task AddApplicationInvitesAsync(ApplicationInvitesCreateCmd createInviteCmd, CancellationToken ct)
        {
            Spd_PortalUser? user = _dynaContext.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == createInviteCmd.CreatedByUserId).FirstOrDefault();

            if (createInviteCmd.OrgId != SpdConstants.BcGovOrgId)
            {
                Account? org = _dynaContext.AccountSet.Where(a => a.AccountId == createInviteCmd.OrgId).FirstOrDefault();

                foreach (var item in createInviteCmd.ApplicationInvites)
                {
                    Spd_PortalInvitation invitation = _mapper.Map<Spd_PortalInvitation>(item);
                    var encryptedInviteId = WebUtility.UrlEncode(_dataProtector.Protect((invitation.Spd_PortalInvitationId ?? Guid.Empty).ToString(), DateTimeOffset.UtcNow.AddDays(SpdConstants.ApplicationInviteValidDays)));
                    invitation.Spd_InvitationLink = $"{createInviteCmd.HostUrl}{SpdConstants.CrrpApplicationInviteLink}{encryptedInviteId}";

                    if (org != null)
                        invitation.Spd_OrganizationId = org.ToEntityReference();
                    if (user != null)
                        invitation.Spd_InvitedBy = user.ToEntityReference();

                    Guid? serviceTypeGuid = Spd.Utilities.Dataverse.DataverseContextLookupHelpers.GetServiceTypeGuid(item.ServiceType.ToString());
                    if (serviceTypeGuid != null)
                    {
                        Spd_ServiceType? serviceType = _dynaContext.Spd_ServiceTypeSet.Where(s => s.Spd_ServiceTypeId == serviceTypeGuid).FirstOrDefault();
                        if (serviceType != null)
                            invitation.Spd_ServiceTypeId = serviceType.ToEntityReference();
                    }

                    _dynaContext.AddObject(invitation);

                    if (item.OriginalClearanceAccessId != null)
                    {
                        Spd_ClearanceAccess? access = _dynaContext.Spd_ClearanceAccessSet.Where(a => a.Spd_ClearanceAccessId == item.OriginalClearanceAccessId).FirstOrDefault();
                        if (access == null || access.Spd_OrganizationId == null || access.Spd_OrganizationId.Id != createInviteCmd.OrgId)
                            throw new ApiException(HttpStatusCode.BadRequest, "Invalid clearance access id.");

                        access.StateCode = Spd_ClearanceAccess_StateCode.Inactive;
                        access.StatusCode = Spd_ClearanceAccess_StatusCode.Revoked;
                        _dynaContext.UpdateObject(access);
                    }
                }
            }
            else
            {
                foreach (var item in createInviteCmd.ApplicationInvites)
                {
                    Account? org = _dynaContext.AccountSet.Where(a => a.AccountId == item.OrgId).FirstOrDefault();
                    Spd_PortalInvitation invitation = _mapper.Map<Spd_PortalInvitation>(item);
                    var encryptedInviteId = WebUtility.UrlEncode(_dataProtector.Protect((invitation.Spd_PortalInvitationId ?? Guid.Empty).ToString(), DateTimeOffset.UtcNow.AddDays(SpdConstants.ApplicationInviteValidDays)));
                    invitation.Spd_InvitationLink = $"{createInviteCmd.HostUrl}{SpdConstants.PssoApplicationInviteLink}{encryptedInviteId}";

                    if (org != null)
                        invitation.Spd_OrganizationId = org.ToEntityReference();
                    if (user != null)
                        invitation.Spd_InvitedBy = user.ToEntityReference();

                    Guid? serviceTypeGuid = Spd.Utilities.Dataverse.DataverseContextLookupHelpers.GetServiceTypeGuid(item.ServiceType.ToString());
                    if (serviceTypeGuid != null)
                    {
                        Spd_ServiceType? serviceType = _dynaContext.Spd_ServiceTypeSet.Where(s => s.Spd_ServiceTypeId == serviceTypeGuid).FirstOrDefault();
                        if (serviceType != null)
                            invitation.Spd_ServiceTypeId = serviceType.ToEntityReference();
                    }

                    _dynaContext.AddObject(invitation);
                }
            }

            _dynaContext.SaveChanges();
            await Task.CompletedTask;
        }

        private async Task UpdateApplicationInvitesAsync(ApplicationInviteUpdateCmd applicationInviteUpdateCmd, CancellationToken cancellationToken)
        {
            Spd_PortalInvitation? invite = await GetPortalInvitationById(applicationInviteUpdateCmd.OrgId, applicationInviteUpdateCmd.ApplicationInviteId);

            if (invite == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Invalid OrgId or ApplicationInviteId");

            invite.StateCode = Spd_PortalInvitation_StateCode.Inactive;
            invite.StatusCode = Enum.Parse<Spd_PortalInvitation_StatusCode>(applicationInviteUpdateCmd.ApplicationInviteStatusEnum.ToString());
            _dynaContext.UpdateObject(invite);

            _dynaContext.SaveChanges();
            await Task.CompletedTask;
        }

        public async Task<AppInviteVerifyResp> VerifyApplicationInvitesAsync(ApplicationInviteVerifyCmd verifyInviteCmd, CancellationToken ct)
        {
            Guid inviteId;
            try
            {
                string inviteIdStr = _dataProtector.Unprotect(WebUtility.UrlDecode(verifyInviteCmd.InviteEncryptedCode));
                inviteId = Guid.Parse(inviteIdStr);
            }
            catch
            {
                throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");
            }

            Spd_PortalInvitation? invite = _dynaContext.Spd_PortalInvitationSet
                .Where(i => i.Spd_PortalInvitationId == inviteId)
                .Where(i => i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ScreeningRequest)
                .Where(i => i.StateCode != Spd_PortalInvitation_StateCode.Inactive)
                .FirstOrDefault();

            if (invite == null)
                throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");

            _dynaContext.LoadProperty(invite, nameof(Spd_PortalInvitation.Spd_OrgAgNIZATion_Spd_PortalInvitation));

            invite.Spd_Views = (invite.Spd_Views ?? 0) + 1;
            _dynaContext.UpdateObject(invite);
            _dynaContext.SaveChanges();

            return await Task.FromResult(_mapper.Map<AppInviteVerifyResp>(invite));
        }

        private async Task<Spd_PortalInvitation?> GetPortalInvitationById(Guid organizationId, Guid portalInvitationId)
        {
            Spd_PortalInvitation? invite = _dynaContext.Spd_PortalInvitationSet
                .Where(a => a.Spd_PortalInvitationId == portalInvitationId && a.Spd_OrganizationId != null && a.Spd_OrganizationId.Id == organizationId)
                .SingleOrDefault();

            return await Task.FromResult(invite);
        }
    }
}

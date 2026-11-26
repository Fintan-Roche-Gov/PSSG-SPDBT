using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.PortalUser;
internal class PortalUserRepository : IPortalUserRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PortalUserRepository> _logger;
    private readonly ITimeLimitedDataProtector _dataProtector;

    public PortalUserRepository(DataverseContext ctx,
        IMapper mapper,
        ILogger<PortalUserRepository> logger,
        IDataProtectionProvider dpProvider)
    {
        _context = ctx;
        _mapper = mapper;
        _logger = logger;
        _dataProtector = dpProvider.CreateProtector(nameof(CreatePortalUserCmd)).ToTimeLimitedDataProtector();
    }

    public async Task<PortalUserQryResp> QueryAsync(PortalUserQry qry, CancellationToken cancellationToken)
    {
        return qry switch
        {
            PortalUserByIdQry q => await GetUserAsync(q.UserId, cancellationToken),
            PortalUserQry q => await ListUsersAsync(qry, cancellationToken)
        };
    }
    public async Task<PortalUserResp> ManageAsync(PortalUserCmd cmd, CancellationToken cancellationToken)
    {
        return cmd switch
        {
            UpdatePortalUserCmd c => await UpdatePortalUserAsync(c, cancellationToken),
            CreatePortalUserCmd c => await CreatePortalUserAsync(c, cancellationToken),
            PortalUserDeleteCmd c => await DeleteProtalUserAsync(c.Id, cancellationToken),
            BizPortalUserInvitationVerify v => await VerifyBizPortalUserInvitationAsync(v, cancellationToken),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }
    private async Task<PortalUserResp> VerifyBizPortalUserInvitationAsync(BizPortalUserInvitationVerify verify, CancellationToken ct)
    {
        Guid inviteId;
        try
        {
            string inviteIdStr = _dataProtector.Unprotect(WebUtility.UrlDecode(verify.InviteIdEncryptedCode));
            inviteId = Guid.Parse(inviteIdStr);
        }
        catch
        {
            throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");
        }

        var invite = _context.Spd_PortalInvitationSet
            .Where(i => i.Spd_PortalInvitationId == inviteId)
            .Where(i => i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.PortalUser)
            .Where(i => i.StateCode != Spd_PortalInvitation_StateCode.Inactive)
            .FirstOrDefault();


        if (invite == null)
            throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");
        Account? inviteOrg = invite.Spd_OrganizationId == null
            ? null
            : _context.AccountSet.Where(a => a.AccountId == invite.Spd_OrganizationId.Id).FirstOrDefault();
        if (!string.Equals(inviteOrg?.Spd_OrgGuid, verify.OrgGuid.ToString(), StringComparison.InvariantCultureIgnoreCase))
            throw new ApiException(HttpStatusCode.Accepted, "There is a mismatch between the invite organization and your bceid organization.");

        //verified, now add/link identity to user.
        Spd_Identity? identity = _context.Spd_IdentitySet
            .Where(i => i.Spd_UserGuid == verify.UserGuid.ToString() && i.Spd_OrgGuid == verify.OrgGuid.ToString())
            .Where(i => i.StateCode == Spd_Identity_StateCode.Active)
            .FirstOrDefault();
        if (identity == null)
        {
            identity = new Spd_Identity()
            {
                Spd_IdentityId = Guid.NewGuid(),
                Spd_OrgGuid = verify.OrgGuid.ToString(),
                Spd_UserGuid = verify.UserGuid.ToString(),
                Spd_Type = Spd_IdentityProvider.BusinessBceid
            };
            _context.AddObject(identity);
        }
        else //the user already has identity in the system, probably used by other org.
        {
            //check if current org already has the same user
            Spd_PortalUser? dupUser = _context.Spd_PortalUserSet
                .Where(u => u.Spd_ServiceCategory == Spd_ServiceCategory.Licensing)
                .Where(u => u.Spd_IdentityId != null && u.Spd_IdentityId.Id == identity.Spd_IdentityId)
                .Where(u => u.StateCode == Spd_PortalUser_StateCode.Active)
                .Where(u => u.Spd_OrganizationId != null && invite.Spd_OrganizationId != null && u.Spd_OrganizationId.Id == invite.Spd_OrganizationId.Id)
                .Where(u => u.Spd_PortalUserId != (invite.Spd_PortalUserId == null ? null : invite.Spd_PortalUserId.Id))
                .FirstOrDefault();
            if (dupUser != null)
                throw new ApiException(HttpStatusCode.Accepted, "Your BCeID is already associated to a user in this organization.");
        }

        Guid userId = invite.Spd_PortalUserId?.Id ?? Guid.Empty;
        Spd_PortalUser user = await GetUserById(userId, ct);
        user.Spd_IdentityId = identity?.ToEntityReference();
        _context.UpdateObject(user);

        //set invite views
        invite.Spd_Views = (invite.Spd_Views ?? 0) + 1;
        invite.StateCode = Spd_PortalInvitation_StateCode.Inactive;
        invite.StatusCode = Spd_PortalInvitation_StatusCode.Completed;
        _context.UpdateObject(invite);

        _context.SaveChanges();
        return new PortalUserResp()
        {
            OrganizationId = user.Spd_OrganizationId?.Id ?? Guid.Empty,
        };
    }
    private async Task<PortalUserListResp> ListUsersAsync(PortalUserQry qry, CancellationToken cancellationToken)
    {
        IQueryable<Spd_PortalUser> users = _context.Spd_PortalUserSet;
        IEnumerable<Spd_PortalUser> userList;

        if (!qry.IncludeInactive)
            users = users.Where(d => d.StateCode != Spd_PortalUser_StateCode.Inactive);
        if (qry.OrgId != null) users = users.Where(d => d.Spd_OrganizationId != null && d.Spd_OrganizationId.Id == qry.OrgId);
        if (qry.UserEmail != null) users = users.Where(d => d.Spd_EmailAddress1 == qry.UserEmail);
        if (qry.IdentityId != null) users = users.Where(d => d.Spd_IdentityId != null && d.Spd_IdentityId.Id == qry.IdentityId);
        if (qry.PortalUserServiceCategory == null || qry.PortalUserServiceCategory == PortalUserServiceCategoryEnum.Screening)
            users = users.Where(d => d.Spd_ServiceCategory == null || d.Spd_ServiceCategory == Spd_ServiceCategory.Screening);
        else
            users = users.Where(d => d.Spd_ServiceCategory == Spd_ServiceCategory.Licensing);
        if (qry.ContactRoleCode != null && qry.ContactRoleCode.Any())
        {
            IEnumerable<string> roleNames = qry.ContactRoleCode.Select(c => c.ToString());
            userList = users.AsEnumerable().Where(u =>
            {
                _context.LoadProperty(u, nameof(u.Spd_Spd_Role_Spd_PortalUser));
                return roleNames.Any(roleName => u.Spd_Spd_Role_Spd_PortalUser.Any(role => role.Spd_Name == roleName));
            });
        }
        else
        {
            userList = users.ToList();
        }
        IEnumerable<Spd_PortalUser> results = userList;
        if (qry.ParentOrgId != null)
        {
            results = userList.Where(u =>
            {
                if (u.Spd_OrganizationId == null) return false;
                Account? org = _context.AccountSet.Where(a => a.AccountId == u.Spd_OrganizationId.Id).FirstOrDefault();
                return org?.ParentAccountId?.Id == qry.ParentOrgId;
            });
        }
        if (qry.OrgIdOrParentOrgId != null)
        {
            results = userList.Where(u =>
            {
                bool orgMatch = u.Spd_OrganizationId?.Id == qry.OrgIdOrParentOrgId;
                Account? org = u.Spd_OrganizationId == null ? null : _context.AccountSet.Where(a => a.AccountId == u.Spd_OrganizationId.Id).FirstOrDefault();
                bool parentMatch = org?.ParentAccountId?.Id == qry.OrgIdOrParentOrgId;
                return orgMatch || parentMatch;
            });
        }
        return new PortalUserListResp
        {
            Items = _mapper.Map<IEnumerable<PortalUserResp>>(results)
        };
    }
    private async Task<PortalUserResp> DeleteProtalUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserById(userId, cancellationToken);
        if (user.Spd_IdentityId != null)
        {
            // Inactivate the user
            user.StateCode = Spd_PortalUser_StateCode.Inactive;
            user.StatusCode = Spd_PortalUser_StatusCode.Inactive;
            _context.UpdateObject(user);
        }
        else
        {

            var invition = GetPortalInvitationByUserId(userId);
            if (invition is not null) _context.DeleteObject(invition);

            // Delete user and invitation
            _context.DeleteObject(user);
        }

        _context.SaveChanges();
        return new PortalUserResp();
    }
    private Spd_PortalInvitation? GetPortalInvitationByUserId(Guid userId)
    {
        var spd_portalinvitation = _context.Spd_PortalInvitationSet
            .Where(a => a.Spd_PortalUserId != null && a.Spd_PortalUserId.Id == userId)
            .FirstOrDefault();

        return spd_portalinvitation;
    }
    private async Task<PortalUserResp> UpdatePortalUserAsync(UpdatePortalUserCmd c, CancellationToken ct)
    {
        Spd_PortalUser portalUser = await GetUserById(c.Id, ct);
        Account? org = null;
        Spd_Identity? identity = null;
        if (c.FirstName != null) portalUser.Spd_FirstName = c.FirstName;
        if (c.LastName != null) portalUser.Spd_Surname = c.LastName;
        if (c.EmailAddress != null) portalUser.Spd_EmailAddress1 = c.EmailAddress;
        if (c.PhoneNumber != null) portalUser.Spd_PhoneNumber = c.PhoneNumber;
        if (c.TermAgreeTime != null) portalUser.Spd_LastLoggedIn = c.TermAgreeTime.Value.UtcDateTime;
        _context.UpdateObject(portalUser);

        if (c.OrgId != null && portalUser.Spd_OrganizationId?.Id != c.OrgId)
        {
            org = _context.AccountSet.Where(a => a.AccountId == c.OrgId).FirstOrDefault();
            portalUser.Spd_OrganizationId = org?.ToEntityReference();
        }
        if (c.IdentityId != null && portalUser.Spd_IdentityId?.Id != c.IdentityId)
        {
            identity = _context.Spd_IdentitySet.Where(i => i.Spd_IdentityId == c.IdentityId).FirstOrDefault();
            portalUser.Spd_IdentityId = identity?.ToEntityReference();
        }

        _context.LoadProperty(portalUser, nameof(portalUser.Spd_Spd_Role_Spd_PortalUser));
        ContactRoleCode? currentContactRoleCode = SharedMappingFuncs.GetContactRoleCode(portalUser.Spd_Spd_Role_Spd_PortalUser);

        if (c.ContactRoleCode != null && currentContactRoleCode != c.ContactRoleCode)
        {
            Spd_Role? currentRole = portalUser.Spd_Spd_Role_Spd_PortalUser.FirstOrDefault();
            if (currentRole != null)
            {
                _context.DeleteLink(portalUser, new Relationship(Spd_PortalUser.Fields.Spd_Spd_Role_Spd_PortalUser), currentRole);
            }
            Spd_Role? role = LookupRole(c.ContactRoleCode.ToString());
            if (role != null)
            {
                _context.AddLink(portalUser, new Relationship(Spd_PortalUser.Fields.Spd_Spd_Role_Spd_PortalUser), role);
            }
        }
        _context.SaveChanges();

        portalUser = await GetUserById(c.Id, ct);
        return _mapper.Map<PortalUserResp>(portalUser);
    }
    private async Task<PortalUserResp> CreatePortalUserAsync(CreatePortalUserCmd c, CancellationToken ct)
    {
        Spd_PortalUser portaluser = _mapper.Map<Spd_PortalUser>(c);
        _context.AddObject(portaluser);
        Account? org = _context.AccountSet.Where(a => a.AccountId == c.OrgId).FirstOrDefault();
        if (org == null)
        {
            _logger.LogError($"no valid org is found by orgId {c.OrgId}");
            throw new ApiException(System.Net.HttpStatusCode.BadRequest, "The organization is not valid.");
        }
        portaluser.Spd_OrganizationId = org.ToEntityReference();

        Spd_Identity? identity = null;
        if (c.IdentityId != null)
        {
            identity = _context.Spd_IdentitySet.Where(i => i.Spd_IdentityId == c.IdentityId).FirstOrDefault();
            if (identity == null)
            {
                _logger.LogError($"no valid identity for {c.IdentityId}");
                throw new ApiException(System.Net.HttpStatusCode.BadRequest, "The identity is not valid.");
            }
        }
        if (identity != null)
            portaluser.Spd_IdentityId = identity.ToEntityReference();

        if (c.ContactRoleCode != null)
        {
            Spd_Role? role = LookupRole(c.ContactRoleCode.ToString());
            if (role != null)
            {
                _context.AddLink(portaluser, new Relationship(Spd_PortalUser.Fields.Spd_Spd_Role_Spd_PortalUser), role);
            }
        }

        // create portal invitation
        if (c.IdentityId == null)
        {
            Spd_PortalInvitation invitation = _mapper.Map<Spd_PortalInvitation>(c);
            var encryptedInviteId = WebUtility.UrlEncode(_dataProtector.Protect(invitation.Spd_PortalInvitationId.ToString(), DateTimeOffset.UtcNow.AddDays(SpdConstants.UserInviteValidDays)));
            invitation.Spd_InvitationLink = $"{c.HostUrl}{SpdConstants.BizPortalUserInviteLink}{encryptedInviteId}";
            _context.AddObject(invitation);
            invitation.Spd_OrganizationId = org.ToEntityReference();
            invitation.Spd_PortalUserId = portaluser.ToEntityReference();
            if (c.CreatedByUserId != null)
            {
                Spd_PortalUser invitedBy = await GetUserById((Guid)c.CreatedByUserId, ct);
                invitation.Spd_InvitedBy = invitedBy.ToEntityReference();
            }
        }
        _context.SaveChanges();

        PortalUserResp userResp = _mapper.Map<PortalUserResp>(portaluser);
        userResp.OrganizationId = c.OrgId;
        userResp.ContactRoleCode = c.ContactRoleCode;
        return userResp;
    }

    private async Task<Spd_PortalUser> GetUserById(Guid userId, CancellationToken ct)
    {
        var user = _context.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == userId).FirstOrDefault();

        if (user?.StateCode == Spd_PortalUser_StateCode.Inactive)
            throw new InactiveException(HttpStatusCode.BadRequest, $"User '{userId}' is inactive.");

        if (user != null)
            return user;

        throw new NotFoundException(HttpStatusCode.BadRequest, $"The user with userId '{userId}' cannot be found.");
    }
    private async Task<PortalUserResp> GetUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetUserById(userId, ct);
        var response = _mapper.Map<PortalUserResp>(user);
        return response;
    }

    private Spd_Role? LookupRole(string? roleKey)
    {
        if (string.IsNullOrEmpty(roleKey))
            return null;

        return _context.Spd_RoleSet.Where(r => r.Spd_Name == roleKey).FirstOrDefault();
    }
}
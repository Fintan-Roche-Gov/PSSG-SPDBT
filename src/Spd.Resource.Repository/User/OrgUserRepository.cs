using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.User
{
    internal class OrgUserRepository : IOrgUserRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _dynaContext;
        private readonly IMapper _mapper;
        private readonly ILogger<OrgUserRepository> _logger;
        private readonly ITimeLimitedDataProtector _dataProtector;

        public OrgUserRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper, ILogger<OrgUserRepository> logger, IDataProtectionProvider dpProvider)
        {
            _dynaContext = ctx;
            _mapper = mapper;
            _logger = logger;
            _dataProtector = dpProvider.CreateProtector(nameof(UserCreateCmd)).ToTimeLimitedDataProtector();
        }

        public async Task<OrgUserQryResult> QueryOrgUserAsync(OrgUserQry qry, CancellationToken ct)
        {
            return qry switch
            {
                OrgUserByIdQry q => await GetUserAsync(q.UserId, ct),
                OrgUsersSearch q => await SearchUsers(q.OrgId, q.IdentityId, ct),
                _ => throw new NotSupportedException($"{qry.GetType().Name} is not supported")
            };
        }

        public async Task<OrgUserManageResult> ManageOrgUserAsync(OrgUserCmd cmd, CancellationToken ct)
        {
            return cmd switch
            {
                UserCreateCmd c => await AddUserAsync(c, ct),
                UserUpdateCmd c => await UpdateUserAsync(c, ct),
                UserDeleteCmd c => await DeleteUserAsync(c.Id, ct),
                UserUpdateLoginCmd c => await UpdateUserLoginAsync(c.Id, ct),
                UserInvitationVerify v => await VerifyOrgUserInvitationAsync(v, ct),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        private Task<OrgUserManageResult> VerifyOrgUserInvitationAsync(UserInvitationVerify verify, CancellationToken ct)
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

            Spd_PortalInvitation? invite = _dynaContext.Spd_PortalInvitationSet
                .Where(i => i.Spd_PortalInvitationId == inviteId)
                .Where(i => i.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.PortalUser)
                .Where(i => i.StateCode != Spd_PortalInvitation_StateCode.Inactive)
                .FirstOrDefault();
            if (invite == null)
                throw new ApiException(HttpStatusCode.Accepted, "The invitation link is no longer valid.");

            Account? verifyOrg = _dynaContext.AccountSet
                .Where(a => a.Spd_OrgGuid == verify.OrgGuid.ToString())
                .FirstOrDefault();
            if (verifyOrg == null || invite.Spd_OrganizationId == null || invite.Spd_OrganizationId.Id != verifyOrg.AccountId)
                throw new ApiException(HttpStatusCode.Accepted, "There is a mismatch between the invite organization and your bceid organization.");

            Spd_Identity? identity = _dynaContext.Spd_IdentitySet
                .Where(i => i.Spd_UserGuid == verify.UserGuid.ToString() && i.Spd_OrgGuid == verify.OrgGuid.ToString())
                .Where(i => i.StateCode == Spd_Identity_StateCode.Active)
                .FirstOrDefault();
            if (identity == null)
            {
                identity = new Spd_Identity
                {
                    Spd_IdentityId = Guid.NewGuid(),
                    Spd_OrgGuid = verify.OrgGuid.ToString(),
                    Spd_UserGuid = verify.UserGuid.ToString(),
                    Spd_Type = Spd_IdentityProvider.BusinessBceid
                };
                _dynaContext.AddObject(identity);
            }
            else
            {
                Spd_PortalUser? dupUser = _dynaContext.Spd_PortalUserSet
                    .Where(u => u.Spd_ServiceCategory == Spd_ServiceCategory.Screening || u.Spd_ServiceCategory == null)
                    .Where(u => u.Spd_IdentityId != null && u.Spd_IdentityId.Id == identity.Spd_IdentityId)
                    .Where(u => u.StateCode == Spd_PortalUser_StateCode.Active)
                    .Where(u => u.Spd_OrganizationId != null && invite.Spd_OrganizationId != null && u.Spd_OrganizationId.Id == invite.Spd_OrganizationId.Id)
                    .Where(u => u.Spd_PortalUserId != invite.Spd_PortalUserId.Id)
                    .FirstOrDefault();
                if (dupUser != null)
                    throw new ApiException(HttpStatusCode.Accepted, "Your BCeID is already associated to a user in this organization.");
            }

            Guid userId = invite.Spd_PortalUserId?.Id ?? Guid.Empty;
            Spd_PortalUser user = GetUserById(userId, ct).GetAwaiter().GetResult();
            user.Spd_IdentityId = identity.ToEntityReference();
            _dynaContext.UpdateObject(user);

            invite.Spd_Views = (invite.Spd_Views ?? 0) + 1;
            invite.StateCode = Spd_PortalInvitation_StateCode.Inactive;
            invite.StatusCode = Spd_PortalInvitation_StatusCode.Completed;
            _dynaContext.UpdateObject(invite);

            _dynaContext.SaveChanges();
            return Task.FromResult(new OrgUserManageResult(new UserResult { OrganizationId = invite.Spd_OrganizationId?.Id }));
        }

        private Task<OrgUserManageResult> AddUserAsync(UserCreateCmd createUserCmd, CancellationToken ct)
        {
            if (createUserCmd.User.OrganizationId == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Organization cannot be null");

            Account? organization = _dynaContext.AccountSet.Where(a => a.AccountId == createUserCmd.User.OrganizationId).FirstOrDefault();
            if (organization == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot find organization");

            Spd_PortalUser user = MapToPortalUser(createUserCmd.User);
            user.Spd_PortalUserId = Guid.NewGuid();
            user.Spd_OrganizationId = organization.ToEntityReference();

            if (createUserCmd.IdentityId != null)
            {
                Spd_Identity? identity = _dynaContext.Spd_IdentitySet.Where(i => i.Spd_IdentityId == createUserCmd.IdentityId).FirstOrDefault();
                if (identity != null)
                {
                    user.Spd_IdentityId = identity.ToEntityReference();
                }
            }

            _dynaContext.AddObject(user);

            if (createUserCmd.User.OrganizationId == SpdConstants.BcGovOrgId)
            {
                _dynaContext.SaveChanges();
                return Task.FromResult(new OrgUserManageResult(MapToUserResult(user)));
            }

            if (DynamicsContextLookupHelpers.RoleGuidDictionary.TryGetValue(createUserCmd.User.ContactAuthorizationTypeCode.ToString(), out Guid roleId))
            {
                Spd_Role? role = _dynaContext.Spd_RoleSet.Where(r => r.Spd_RoleId == roleId).FirstOrDefault();
                if (role != null)
                {
                    _dynaContext.AddLink(role, new Relationship(Spd_Role.Fields.Spd_Spd_Role_Spd_PortalUser), user);
                }
            }

            if (createUserCmd.IdentityId == null)
            {
                Guid inviteId = Guid.NewGuid();
                Spd_PortalInvitation invitation = MapToPortalInvitation(createUserCmd.User);
                invitation.Spd_PortalInvitationId = inviteId;
                invitation.Spd_InvitationLink = $"{createUserCmd.HostUrl}{SpdConstants.UserInviteLink}{WebUtility.UrlEncode(_dataProtector.Protect(inviteId.ToString(), DateTimeOffset.UtcNow.AddDays(SpdConstants.UserInviteValidDays)))}";
                invitation.Spd_OrganizationId = organization.ToEntityReference();
                invitation.Spd_PortalUserId = user.ToEntityReference();

                if (createUserCmd.CreatedByUserId != null)
                {
                    Spd_PortalUser invitedBy = GetUserById((Guid)createUserCmd.CreatedByUserId, ct).GetAwaiter().GetResult();
                    invitation.Spd_InvitedBy = invitedBy.ToEntityReference();
                }

                _dynaContext.AddObject(invitation);
            }

            _dynaContext.SaveChanges();

            UserResult userResult = MapToUserResult(user);
            userResult.OrganizationId = createUserCmd.User.OrganizationId;
            userResult.ContactAuthorizationTypeCode = createUserCmd.User.ContactAuthorizationTypeCode;
            return Task.FromResult(new OrgUserManageResult(userResult));
        }

        private Task<OrgUserManageResult> UpdateUserAsync(UserUpdateCmd updateUserCmd, CancellationToken cancellationToken)
        {
            try
            {
                Spd_PortalUser user = GetUserById(updateUserCmd.Id, cancellationToken).GetAwaiter().GetResult();

                if (updateUserCmd.OnlyChangePhoneJob)
                {
                    user.Spd_PhoneNumber = updateUserCmd.User.PhoneNumber;
                    user.Spd_JobTitle = updateUserCmd.User.JobTitle;
                }
                else
                {
                    UpdatePortalUser(user, updateUserCmd.User);
                }

                _dynaContext.LoadProperty(user, nameof(Spd_PortalUser.Spd_Spd_Role_Spd_PortalUser));
                Spd_Role? existingRole = user.Spd_Spd_Role_Spd_PortalUser.FirstOrDefault();
                string expectedRole = updateUserCmd.User.ContactAuthorizationTypeCode.ToString();
                string? existingRoleName = existingRole?.Spd_RoleId != null
                    ? DynamicsContextLookupHelpers.RoleGuidDictionary.FirstOrDefault(x => x.Value == existingRole.Spd_RoleId.Value).Key
                    : null;

                if (existingRoleName != expectedRole)
                {
                    if (existingRole != null)
                    {
                        _dynaContext.DeleteLink(existingRole, new Relationship(Spd_Role.Fields.Spd_Spd_Role_Spd_PortalUser), user);
                    }

                    if (DynamicsContextLookupHelpers.RoleGuidDictionary.TryGetValue(expectedRole, out Guid newRoleId))
                    {
                        Spd_Role? newRole = _dynaContext.Spd_RoleSet.Where(r => r.Spd_RoleId == newRoleId).FirstOrDefault();
                        if (newRole != null)
                        {
                            _dynaContext.AddLink(newRole, new Relationship(Spd_Role.Fields.Spd_Spd_Role_Spd_PortalUser), user);
                        }
                    }
                }

                _dynaContext.UpdateObject(user);
                _dynaContext.SaveChanges();

                UserResult userResult = MapToUserResult(user);
                userResult.ContactAuthorizationTypeCode = updateUserCmd.User.ContactAuthorizationTypeCode;
                return Task.FromResult(new OrgUserManageResult(userResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user {updateUserCmd.Id}");
                throw;
            }
        }

        private async Task<OrgUserManageResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            Spd_PortalUser user = await GetUserById(userId, cancellationToken);
            if (user.Spd_IdentityId != null)
            {
                user.StateCode = Spd_PortalUser_StateCode.Inactive;
                user.StatusCode = Spd_PortalUser_StatusCode.Inactive;
                _dynaContext.UpdateObject(user);
            }
            else
            {
                Spd_PortalInvitation? invitation = GetPortalInvitationByUserId(userId);
                if (invitation != null)
                    _dynaContext.DeleteObject(invitation);

                _dynaContext.DeleteObject(user);
            }

            _dynaContext.SaveChanges();
            return new OrgUserManageResult();
        }

        private async Task<OrgUserResult> GetUserAsync(Guid userId, CancellationToken ct)
        {
            Spd_PortalUser user = await GetUserById(userId, ct);
            return new OrgUserResult(MapToUserResult(user));
        }

        private Task<OrgUsersResult> SearchUsers(Guid? organizationId, Guid? identityId, CancellationToken cancellationToken)
        {
            IQueryable<Spd_PortalUser> users = _dynaContext.Spd_PortalUserSet
                .Where(u => u.StateCode == Spd_PortalUser_StateCode.Active)
                .Where(u => u.Spd_ServiceCategory == Spd_ServiceCategory.Screening || u.Spd_ServiceCategory == null);

            if (organizationId != null)
                users = users.Where(u => u.Spd_OrganizationId != null && u.Spd_OrganizationId.Id == organizationId);
            if (identityId != null)
                users = users.Where(u => u.Spd_IdentityId != null && u.Spd_IdentityId.Id == identityId);

            List<Spd_PortalUser> userList = users.ToList();
            foreach (Spd_PortalUser user in userList)
            {
                _dynaContext.LoadProperty(user, nameof(Spd_PortalUser.Spd_Spd_Role_Spd_PortalUser));
                _dynaContext.LoadProperty(user, nameof(Spd_PortalUser.Spd_Spd_Identity_Spd_PortalUser));
            }

            return Task.FromResult(new OrgUsersResult(userList.Select(MapToUserResult)));
        }

        private async Task<OrgUserManageResult> UpdateUserLoginAsync(Guid userId, CancellationToken cancellationToken)
        {
            Spd_PortalUser user = await GetUserById(userId, cancellationToken);
            user.Spd_LastLoggedIn = DateTime.UtcNow;
            _dynaContext.UpdateObject(user);
            _dynaContext.SaveChanges();
            return new OrgUserManageResult();
        }

        private Spd_PortalInvitation? GetPortalInvitationByUserId(Guid userId)
        {
            return _dynaContext.Spd_PortalInvitationSet
                .Where(a => a.Spd_PortalUserId != null && a.Spd_PortalUserId.Id == userId)
                .FirstOrDefault();
        }

        private Task<Spd_PortalUser> GetUserById(Guid userId, CancellationToken ct)
        {
            Spd_PortalUser? user = _dynaContext.Spd_PortalUserSet
                .Where(a => a.Spd_PortalUserId == userId)
                .Where(u => u.Spd_ServiceCategory == Spd_ServiceCategory.Screening || u.Spd_ServiceCategory == null)
                .FirstOrDefault();

            if (user == null)
                throw new NotFoundException(HttpStatusCode.BadRequest, $"The user with userId '{userId}' cannot be found.");

            _dynaContext.LoadProperty(user, nameof(Spd_PortalUser.Spd_Spd_Role_Spd_PortalUser));
            _dynaContext.LoadProperty(user, nameof(Spd_PortalUser.Spd_Spd_Identity_Spd_PortalUser));

            if (user.StateCode == Spd_PortalUser_StateCode.Inactive)
                throw new InactiveException(HttpStatusCode.BadRequest, $"User '{userId}' is inactive.");

            return Task.FromResult(user);
        }

        private static Spd_PortalUser MapToPortalUser(User source)
        {
            return new Spd_PortalUser
            {
                Spd_FirstName = source.FirstName,
                Spd_Surname = source.LastName,
                Spd_EmailAddress1 = source.Email,
                Spd_JobTitle = source.JobTitle,
                Spd_PhoneNumber = source.PhoneNumber,
                Spd_ServiceCategory = Spd_ServiceCategory.Screening,
                StateCode = Spd_PortalUser_StateCode.Active,
                StatusCode = Spd_PortalUser_StatusCode.Active
            };
        }

        private static void UpdatePortalUser(Spd_PortalUser target, User source)
        {
            target.Spd_FirstName = source.FirstName;
            target.Spd_Surname = source.LastName;
            target.Spd_EmailAddress1 = source.Email;
            target.Spd_JobTitle = source.JobTitle;
            target.Spd_PhoneNumber = source.PhoneNumber;
            target.Spd_ServiceCategory = Spd_ServiceCategory.Screening;
        }

        private static Spd_PortalInvitation MapToPortalInvitation(User source)
        {
            return new Spd_PortalInvitation
            {
                Spd_FirstName = source.FirstName,
                Spd_Surname = source.LastName,
                Spd_Email = source.Email,
                Spd_JobTitle = source.JobTitle,
                Spd_InvitationType = Spd_PortalInvitation_Spd_InvitationType.PortalUser,
                StateCode = Spd_PortalInvitation_StateCode.Active,
                StatusCode = Spd_PortalInvitation_StatusCode.Sent
            };
        }

        private static UserResult MapToUserResult(Spd_PortalUser source)
        {
            Spd_Role? role = source.Spd_Spd_Role_Spd_PortalUser?.FirstOrDefault();
            ContactRoleCode roleCode = ContactRoleCode.Contact;
            if (role?.Spd_RoleId != null)
            {
                string? roleKey = DynamicsContextLookupHelpers.RoleGuidDictionary.FirstOrDefault(x => x.Value == role.Spd_RoleId.Value).Key;
                if (!string.IsNullOrWhiteSpace(roleKey) && Enum.TryParse(roleKey, out ContactRoleCode parsedRoleCode))
                {
                    roleCode = parsedRoleCode;
                }
            }

            Guid? userGuid = null;
            if (source.Spd_Spd_Identity_Spd_PortalUser != null && Guid.TryParse(source.Spd_Spd_Identity_Spd_PortalUser.Spd_UserGuid, out Guid parsedGuid))
            {
                userGuid = parsedGuid;
            }

            return new UserResult
            {
                Id = source.Spd_PortalUserId ?? Guid.Empty,
                OrgRegistrationId = source.Spd_OrgRegistrationId?.Id,
                OrganizationId = source.Spd_OrganizationId?.Id,
                LastName = source.Spd_Surname ?? string.Empty,
                FirstName = source.Spd_FirstName ?? string.Empty,
                Email = source.Spd_EmailAddress1 ?? string.Empty,
                JobTitle = source.Spd_JobTitle,
                PhoneNumber = source.Spd_PhoneNumber,
                ContactAuthorizationTypeCode = roleCode,
                IsActive = source.Spd_IdentityId != null,
                IsFirstTimeLogin = !source.Spd_LastLoggedIn.HasValue,
                UserGuid = userGuid
            };
        }
    }
}

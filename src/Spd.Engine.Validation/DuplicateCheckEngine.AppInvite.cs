using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Engine.Validation
{
    internal partial class DuplicateCheckEngine : IDuplicateCheckEngine
    {
        public async Task<AppInviteDuplicateCheckResponse> AppInviteDuplicateCheckAsync(AppInviteDuplicateCheckRequest appInviteRequest, CancellationToken ct)
        {
            List<AppInviteDuplicateCheckResult> results = new List<AppInviteDuplicateCheckResult>();
            foreach (var item in appInviteRequest.AppInviteChecks)
            {
                ct.ThrowIfCancellationRequested();
                //duplicated in portal invitation
                bool hasDuplicateInvitation = CheckInvitationDuplicate(item, appInviteRequest.OrgId, ct);
                if (hasDuplicateInvitation)
                {
                    AppInviteDuplicateCheckResult dupResp = _mapper.Map<AppInviteDuplicateCheckResult>(item);
                    dupResp.HasPotentialDuplicate = true;
                    results.Add(dupResp);
                }

                if (!hasDuplicateInvitation)
                {
                    //duplicated in application
                    bool hasDuplicateApplication = CheckInviteApplicationDuplicate(item, appInviteRequest.OrgId, ct);
                    if (hasDuplicateApplication)
                    {
                        AppInviteDuplicateCheckResult dupResp = _mapper.Map<AppInviteDuplicateCheckResult>(item);
                        dupResp.HasPotentialDuplicate = true;
                        results.Add(dupResp);
                    }
                }
            }

            AppInviteDuplicateCheckResponse resp = new AppInviteDuplicateCheckResponse(results);
            return resp;
        }

        private bool CheckInvitationDuplicate(AppInviteDuplicateCheck query, Guid orgId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _dataverseContext.Spd_PortalInvitationSet.Any(invitation =>
                invitation.Spd_OrganizationId != null &&
                invitation.Spd_OrganizationId.Id == orgId &&
                invitation.Spd_FirstName == query.FirstName &&
                invitation.Spd_Surname == query.LastName &&
                invitation.Spd_InvitationType == Spd_PortalInvitation_Spd_InvitationType.ScreeningRequest &&
                invitation.StateCode != Spd_PortalInvitation_StateCode.Inactive);
        }

        private bool CheckInviteApplicationDuplicate(AppInviteDuplicateCheck query, Guid orgId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _dataverseContext.Spd_ApplicationSet.Any(application =>
                application.Spd_OrganizationId != null &&
                application.Spd_OrganizationId.Id == orgId &&
                application.Spd_FirstName == query.FirstName &&
                application.Spd_LastName == query.LastName &&
                application.StateCode != Spd_Application_StateCode.Inactive);
        }

    }
}
using Spd.Utilities.Dataverse.EarlyBound;
using TaskEntity = Spd.Utilities.Dataverse.EarlyBound.Task;

namespace Spd.Utilities.Dataverse;

public static class DataverseContextQueryHelpers
{
    private static Spd.Utilities.Dataverse.EarlyBound.DataverseContext GetEarlyBoundContext(DataverseContext context)
        => context ?? throw new InvalidOperationException("EarlyBoundContext is not initialized.");

    public static System.Threading.Tasks.Task<Spd_Application?> GetApplicationById(this DataverseContext context, Guid appId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == appId).SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_PortalUser?> GetUserById(this DataverseContext context, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        var user = eb.Spd_PortalUserSet.Where(a => a.Spd_PortalUserId == userId).SingleOrDefault();
        if (user != null)
        {
            eb.LoadProperty(user, nameof(Spd_PortalUser.Spd_Spd_Role_Spd_PortalUser));
        }

        return System.Threading.Tasks.Task.FromResult(user);
    }

    public static System.Threading.Tasks.Task<Account?> GetOrgById(this DataverseContext context, Guid organizationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        var account = eb.AccountSet
            .Where(a => a.AccountId == organizationId)
            .SingleOrDefault();
        if (account != null)
        {
            eb.LoadProperty(account, nameof(Account.Spd_Account_Spd_ServiceType));
        }

        return System.Threading.Tasks.Task.FromResult(account);
    }

    public static System.Threading.Tasks.Task<Spd_OrgRegistration?> GetOrgRegistrationById(this DataverseContext context, Guid orgRegistrationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_OrgRegistrationSet
            .Where(a => a.Spd_OrgRegistrationId == orgRegistrationId)
            .SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_Clearance?> GetClearanceById(this DataverseContext context, Guid clearanceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_ClearanceSet.Where(a => a.Spd_ClearanceId == clearanceId)
            .Where(a => a.StateCode != Spd_Clearance_StateCode.Inactive)
            .SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_ClearanceAccess?> GetClearanceAccessById(this DataverseContext context, Guid clearanceAccessId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_ClearanceAccessSet.Where(a => a.Spd_ClearanceAccessId == clearanceAccessId)
            .Where(a => a.StateCode != Spd_ClearanceAccess_StateCode.Inactive)
            .SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Contact?> GetContactById(this DataverseContext context, Guid contactId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.ContactSet.Where(a => a.ContactId == contactId).SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_BusinessContact?> GetBizContactById(this DataverseContext context, Guid bizContactId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_BusinessContactSet.Where(a => a.Spd_BusinessContactId == bizContactId).SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<TaskEntity?> GetTaskById(this DataverseContext context, Guid taskId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.TaskSet.Where(a => a.ActivityId == taskId)
            .Where(a => a.StateCode != Task_StateCode.Completed)
            .SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_Identity?> GetIdentityById(this DataverseContext context, Guid identityId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_IdentitySet.Where(a => a.Spd_IdentityId == identityId).SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_Payment?> GetPaymentById(this DataverseContext context, Guid paymentId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_PaymentSet.Where(p => p.Spd_PaymentId == paymentId).SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_Delegate?> GetDelegateById(this DataverseContext context, Guid delegateId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_DelegateSet.Where(a => a.Spd_DelegateId == delegateId).SingleOrDefault());
    }

    public static System.Threading.Tasks.Task<Spd_EventQueue?> GetEventById(this DataverseContext context, Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var eb = GetEarlyBoundContext(context);
        return System.Threading.Tasks.Task.FromResult(eb.Spd_EventQueueSet
            .Where(l => l.Spd_EventQueueId == eventId)
            .FirstOrDefault());
    }
}

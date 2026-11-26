using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using IncidentEntity = Spd.Utilities.Dataverse.EarlyBound.Incident;
using TaskEntity = Spd.Utilities.Dataverse.EarlyBound.Task;

namespace Spd.Resource.Repository.Tasks;
internal class TaskRepository : ITaskRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;

    public TaskRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    public async Task<TaskResp> ManageAsync(TaskCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            CreateTaskCmd c => await CreateTaskAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    private async Task<TaskResp> CreateTaskAsync(CreateTaskCmd cmd, CancellationToken ct)
    {
        TaskEntity t = _mapper.Map<TaskEntity>(cmd);
        _context.AddObject(t);

        if (cmd.RegardingContactId != null)
        {
            ContactEntity? contact = _context.ContactSet.Where(c => c.ContactId == cmd.RegardingContactId).FirstOrDefault();
            if (contact == null)
            {
                throw new ArgumentException($"cannot find contact for contactid = {cmd.RegardingContactId}.");
            }
            t.RegardingObjectId = new EntityReference(ContactEntity.EntityLogicalName, contact.ContactId!.Value);
        }

        if (cmd.RegardingCaseId != null)
        {
            IncidentEntity? incident = _context.IncidentSet.Where(c => c.IncidentId == cmd.RegardingCaseId).FirstOrDefault();
            if (incident == null)
            {
                throw new ArgumentException($"cannot find incident for incidentid = {cmd.RegardingCaseId}.");
            }
            t.RegardingObjectId = new EntityReference(IncidentEntity.EntityLogicalName, incident.IncidentId!.Value);
        }

        if (cmd.RegardingAccountId != null)
        {
            Account? account = _context.AccountSet.Where(c => c.AccountId == cmd.RegardingAccountId).FirstOrDefault();
            if (account == null)
            {
                throw new ArgumentException($"cannot find account for accountid = {cmd.RegardingAccountId}.");
            }
            t.RegardingObjectId = new EntityReference(Account.EntityLogicalName, account.AccountId!.Value);
        }

        if (cmd.AssignedTeamId != null)
        {
            Team? serviceTeam = _context.TeamSet.Where(team => team.TeamId == cmd.AssignedTeamId).FirstOrDefault();
            if (serviceTeam != null)
            {
                t.OwnerId = new EntityReference(Team.EntityLogicalName, serviceTeam.TeamId!.Value);
            }
        }

        if (cmd.LicenceId != null)
        {
            Spd_Licence? lic = _context.Spd_LicenceSet.Where(licence => licence.Spd_LicenceId == cmd.LicenceId).FirstOrDefault();
            if (lic != null)
            {
                t.Spd_LicenceId = new EntityReference(Spd_Licence.EntityLogicalName, lic.Spd_LicenceId!.Value);
            }
        }
        _context.SaveChanges();
        return new TaskResp { TaskId = t.ActivityId ?? Guid.Empty };
    }
}

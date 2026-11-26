using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using EBIncident = global::Spd.Utilities.Dataverse.EarlyBound.Incident;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Incident;
internal class IncidentRepository : IIncidentRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;

    public IncidentRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
        IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    public async Task<IncidentListResp> QueryAsync(IncidentQry qry, CancellationToken ct)
    {
        IQueryable<EBIncident> incidents = _context.IncidentSet;

        if (!qry.IncludeInactive)
            incidents = incidents.Where(i => i.StateCode == Incident_StateCode.Active);

        if (qry.IncidentId != null)
            incidents = incidents.Where(i => i.IncidentId == qry.IncidentId);

        if (qry.ApplicationId != null)
            incidents = incidents.Where(i => i.Spd_ApplicationId != null && i.Spd_ApplicationId.Id == qry.ApplicationId);

        if (qry.CaseNumber != null)
            incidents = incidents.Where(i => i.TicketNumber == qry.CaseNumber);

        var result = incidents.ToList().OrderByDescending(a => a.CreatedOn).ToList();
        foreach (EBIncident item in result)
        {
            _context.LoadProperty(item, "spd_incident_spd_licencecondition");
        }

        return await Task.FromResult(new IncidentListResp
        {
            Items = _mapper.Map<IEnumerable<IncidentResp>>(result)
        });
    }

    public async Task<IncidentResp> ManageAsync(IncidentCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            UpdateIncidentCmd c => await UpdateIncidentAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    private async Task<IncidentResp> UpdateIncidentAsync(UpdateIncidentCmd cmd, CancellationToken ct)
    {
        var incident = _context.IncidentSet
            .Where(i => i.Spd_ApplicationId != null && i.Spd_ApplicationId.Id == cmd.ApplicationId)
            .OrderByDescending(i => i.CreatedOn)
            .FirstOrDefault();

        if (incident == null) { return null; }

        incident.StatusCode = Enum.Parse<Incident_StatusCode>(cmd.CaseStatus.ToString());
        incident.Spd_SubStatusReasonDetail = Enum.Parse<Spd_SubStatusReason>(cmd.CaseSubStatus.ToString());
        _context.UpdateObject(incident);
        _context.SaveChanges();
        return await Task.FromResult(_mapper.Map<IncidentResp>(incident));
    }

}


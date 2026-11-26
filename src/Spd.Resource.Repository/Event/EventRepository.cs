using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.Event;
internal class EventRepository : IEventRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<IEventRepository> _logger;

    public EventRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
        IMapper mapper,
        ILogger<IEventRepository> logger)
    {
        _context = ctx;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EventResp?> GetAsync(Guid eventId, CancellationToken ct)
    {
        Spd_EventQueue? eventQueue = _context.Spd_EventQueueSet
            .Where(e => e.Spd_EventQueueId == eventId)
            .FirstOrDefault();

        return _mapper.Map<EventResp>(eventQueue);
    }

    public async Task<IEnumerable<EventResp>?> QueryAsync(EventQuery qry, CancellationToken ct)
    {
        IQueryable<Spd_EventQueue> eventQueues = _context.Spd_EventQueueSet;

        if (qry.EventStatusReasonEnum != null)
        {
            int? status = (int?)SharedMappingFuncs.GetOptionset<EventStatusReasonEnum, EventStatusReasonOptionSet>(qry.EventStatusReasonEnum);
            eventQueues = eventQueues.Where(i => i.StatusCode != null && (int)i.StatusCode == status);
        }

        if (qry.CutOffDateTime != null)
            eventQueues = eventQueues.Where(e => e.Spd_EventDate < qry.CutOffDateTime);

        List<Spd_EventQueue> eventQueuesList = eventQueues.ToList();

        if (qry.EventTypeEnums != null && qry.EventTypeEnums.Any())
        {
            int?[] types = qry.EventTypeEnums
                .Select(e => (int?)SharedMappingFuncs.GetOptionset<EventTypeEnum, EventTypeOptionSet>(e))
                .ToArray();
            eventQueuesList = eventQueuesList
                .Where(e => e.Spd_EventType != null && types.Contains((int)e.Spd_EventType))
                .ToList();
        }

        return _mapper.Map<IEnumerable<EventResp>>(eventQueuesList);
    }

    public async Task<Unit> ManageAsync(EventUpdateCmd cmd, CancellationToken ct)
    {
        Spd_EventQueue? eventQueue = _context.Spd_EventQueueSet
            .Where(e => e.Spd_EventQueueId == cmd.Id)
            .FirstOrDefault();

        if (eventQueue != null)
        {
            _mapper.Map(cmd, eventQueue);
            _context.UpdateObject(eventQueue);
            _context.SaveChanges();
        }

        return Unit.Value;
    }

    public async Task<Unit> ManageInBatchAsync(List<EventUpdateCmd> cmds, CancellationToken ct)
    {
        try
        {
            foreach (var cmd in cmds)
            {
                Spd_EventQueue? eventQueue = _context.Spd_EventQueueSet
                    .Where(e => e.Spd_EventQueueId == cmd.Id)
                    .FirstOrDefault();

                if (eventQueue != null)
                {
                    _mapper.Map(cmd, eventQueue);
                    _context.UpdateObject(eventQueue);
                }
            }

            _context.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
        }

        return Unit.Value;
    }
}

using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Delegates;
internal class DelegateRepository : IDelegateRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;

    public DelegateRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
        IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    public async Task<DelegateListResp> QueryAsync(DelegateQry qry, CancellationToken ct)
    {
        IQueryable<Spd_Delegate> delegates = _context.Spd_DelegateSet
            .Where(d => d.StateCode != Spd_Delegate_StateCode.Inactive);

        if (qry.ApplicationId != null) delegates = delegates.Where(d => d.Spd_ApplicationId != null && d.Spd_ApplicationId.Id == qry.ApplicationId);
        if (qry.PortalUserId != null) delegates = delegates.Where(d => d.Spd_PortalUserId != null && d.Spd_PortalUserId.Id == qry.PortalUserId);

        var result = delegates.ToList();
        foreach (Spd_Delegate item in result)
        {
            _context.LoadProperty(item, nameof(Spd_Delegate.Spd_PortalUser_Spd_Delegate));
        }

        var response = new DelegateListResp
        {
            Items = _mapper.Map<IEnumerable<DelegateResp>>(result)
        };
        return await Task.FromResult(response);
    }

    public async Task<DelegateResp> ManageAsync(DelegateCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            CreateDelegateCmd c => await CreateDelegateAsync(c, ct),
            DeleteDelegateCmd c => await DeleteDelegateAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    private async Task<DelegateResp> CreateDelegateAsync(CreateDelegateCmd c, CancellationToken ct)
    {
        Spd_Application? app = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == c.ApplicationId).FirstOrDefault();
        if (app == null) throw new ArgumentException("cannot find application");

        Spd_PortalUser? user = c.PortalUserId != null
            ? _context.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == c.PortalUserId).FirstOrDefault()
            : null;

        Spd_Delegate d = _mapper.Map<Spd_Delegate>(c);
        d.Spd_ApplicationId = app.ToEntityReference();
        if (user != null) d.Spd_PortalUserId = user.ToEntityReference();

        _context.AddObject(d);
        _context.SaveChanges();

        if (user != null)
        {
            d.Spd_PortalUser_Spd_Delegate = user;
        }

        return await Task.FromResult(_mapper.Map<DelegateResp>(d));
    }

    private async Task<DelegateResp> DeleteDelegateAsync(DeleteDelegateCmd c, CancellationToken ct)
    {
        Spd_Delegate? d = _context.Spd_DelegateSet.Where(x => x.Spd_DelegateId == c.Id).FirstOrDefault();
        if (d == null) throw new ArgumentException("cannot find delegate");

        d.StateCode = Spd_Delegate_StateCode.Inactive;
        d.StatusCode = Spd_Delegate_StatusCode.Inactive;
        _context.UpdateObject(d);
        _context.SaveChanges();
        return await Task.FromResult(_mapper.Map<DelegateResp>(d));
    }
}



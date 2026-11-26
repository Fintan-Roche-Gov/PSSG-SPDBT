using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.ServiceTypes;

internal class ServiceTypeRepository(DataverseContext ctx, IMapper mapper, IDistributedCache cache) : IServiceTypeRepository
{
    private readonly DataverseContext _context = ctx;

    public async Task<ServiceTypeListResp> QueryAsync(ServiceTypeQry qry, CancellationToken cancellationToken)
    {
        if (qry.ServiceTypeCode == null && qry.ServiceTypeId == null)
        {
            throw new ArgumentException("must be at leaset one qry parameter.");
        }

        IEnumerable<Spd_ServiceType> servicetypes = await cache.GetAsync(
            "spd_servicetypes",
            ct => ValueTask.FromResult(_context.Spd_ServiceTypeSet.ToList()),
            TimeSpan.FromMinutes(60),
            cancellationToken) ?? [];

        if (qry.ServiceTypeId != null)
        {
            servicetypes = servicetypes.Where(s => s.Spd_ServiceTypeId == qry.ServiceTypeId);
        }
        if (qry.ServiceTypeCode != null && DataverseContextLookupHelpers.ServiceTypeGuidDictionary.TryGetValue(qry.ServiceTypeCode.ToString()!, out Guid guid))
        {
            servicetypes = servicetypes.Where(s => s.Spd_ServiceTypeId == guid);
        }
        var list = mapper.Map<IEnumerable<ServiceTypeResp>>(servicetypes);
        return new ServiceTypeListResp
        {
            Items = list
        };
    }
}

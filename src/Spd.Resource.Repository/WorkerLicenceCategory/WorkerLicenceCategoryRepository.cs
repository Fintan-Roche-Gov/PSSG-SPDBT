using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Spd.Utilities.Dataverse.EarlyBound;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.WorkerLicenceCategory;

internal class WorkerLicenceCategoryRepository(DataverseContext ctx, IMapper mapper, IDistributedCache cache) : IWorkerLicenceCategoryRepository
{
    private readonly DataverseContext _context = ctx;

    public async Task<WorkerLicenceCategoryListResp> QueryAsync(WorkerLicenceCategoryQry qry, CancellationToken cancellationToken)
    {
        if (qry.WorkerCategoryType == null && qry.WorkerCategoryTypeId == null)
        {
            throw new ArgumentException("must have at least one qry parameter.");
        }

        IEnumerable<Spd_LicenceCategory> categories = await cache.GetAsync(
            "spd_licencecategory",
            ct => ValueTask.FromResult(_context.Spd_LicenceCategorySet.ToList()),
            TimeSpan.FromMinutes(10),
            cancellationToken) ?? [];

        if (qry.WorkerCategoryTypeId != null)
        {
            categories = categories.Where(s => s.Spd_LicenceCategoryId == qry.WorkerCategoryTypeId);
        }
        if (qry.WorkerCategoryType != null && global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LicenceCategoryDictionary.TryGetValue(qry.WorkerCategoryType.ToString()!, out Guid guid))
        {
            categories = categories.Where(s => s.Spd_LicenceCategoryId == guid);
        }
        var list = mapper.Map<IEnumerable<WorkerLicenceCategoryResp>>(categories);
        return new WorkerLicenceCategoryListResp
        {
            Items = list
        };
    }
}
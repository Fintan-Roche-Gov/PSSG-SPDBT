using Spd.Utilities.Dataverse.EarlyBound;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Spd.Utilities.Dataverse;

public static class OrganizationServiceExtensions
{
    /// <summary>
    /// Convenience method: retrieves a single record and converts it directly to the given early-bound entity type.
    /// </summary>
    /// <param name="service"></param>
    /// <param name="id"></param>
    /// <param name="columnSet"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">The early-bound entity type to retrieve.</typeparam>
    /// <returns>The retrieved entity converted to the given early-bound entity type.</returns>
    public static async Task<T> RetrieveAsyncTyped<T>(
        this IOrganizationServiceAsync2 service,
        Guid id,
        ColumnSet columnSet,
        CancellationToken cancellationToken = default
    )
        where T : Entity
    {
        var result = await service.RetrieveAsync(typeof(T).Name, id, columnSet, cancellationToken);
        return result.ToEntity<T>();
    }

    /// <summary>
    /// Convenience method: retrieves multiple records and converts them directly to the given early-bound entity type.
    /// </summary>
    /// <param name="service"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">The early-bound entity type to retrieve.</typeparam>
    /// <returns>The retrieved entities converted to the given early-bound entity type.</returns>
    public static async Task<IEnumerable<T>> RetrieveMultipleAsyncTyped<T>(
        this IOrganizationServiceAsync2 service,
        QueryBase query,
        CancellationToken cancellationToken = default
    )
        where T : Entity
    {
        var result = await service.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => e.ToEntity<T>());
    }

    

}

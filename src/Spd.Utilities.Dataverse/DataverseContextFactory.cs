using Microsoft.Extensions.Options;
using Microsoft.OData.Client;
using XrmMergeOption = Microsoft.Xrm.Sdk.Client.MergeOption;
using Microsoft.PowerPlatform.Dataverse.Client;
using Spd.Utilities.Dataverse.EarlyBound;
using Microsoft.Xrm.Sdk;

namespace Spd.Utilities.Dataverse;

/// <summary>
/// Factory interface for creating Dataverse contexts.
/// </summary>
public interface IDataverseContextFactory
{
    /// <summary>
    /// Creates a context with AppendOnly merge option (default tracking).
    /// </summary>
    DataverseContext Create();

    /// <summary>
    /// Creates a context with OverwriteChanges merge option.
    /// </summary>
    DataverseContext CreateChangeOverwrite();

    /// <summary>
    /// Creates a context with NoTracking merge option (read-only).
    /// </summary>
    DataverseContext CreateReadOnly();
}

/// <summary>
/// Implementation of IDataverseContextFactory that creates DataverseContext instances.
/// </summary>
internal class DataverseContextFactory : IDataverseContextFactory
{
    private readonly IDataverseClientFactory _dataverseClientFactory;
    private readonly IOrganizationService _service;
    private readonly DataverseOptions _settings;

    public DataverseContextFactory(
        IDataverseClientFactory dataverseClientFactory,
        IOptions<DataverseOptions> dataverseOptions,
        IOrganizationService service)
    {
        _dataverseClientFactory = dataverseClientFactory;
        _settings = dataverseOptions.Value;
        _service = service;
    }

    public DataverseContext Create() => CreateContext(MergeOption.AppendOnly);

    public DataverseContext CreateChangeOverwrite() => CreateContext(MergeOption.OverwriteChanges);

    public DataverseContext CreateReadOnly() => CreateContext(MergeOption.NoTracking);

    private DataverseContext CreateContext(MergeOption mergeOption)
    {
        var ctx = new DataverseContext(_service);

        return ctx;
    }

    private static XrmMergeOption ToXrmMergeOption(MergeOption mergeOption) => mergeOption switch
    {
        MergeOption.AppendOnly => XrmMergeOption.AppendOnly,
        MergeOption.OverwriteChanges => XrmMergeOption.OverwriteChanges,
        MergeOption.PreserveChanges => XrmMergeOption.PreserveChanges,
        MergeOption.NoTracking => XrmMergeOption.NoTracking,
        _ => XrmMergeOption.AppendOnly,
    };
}

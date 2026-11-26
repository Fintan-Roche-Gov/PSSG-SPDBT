using Microsoft.Extensions.Options;

namespace Spd.Utilities.Dataverse;

public class DataverseOptionsValidator : IValidateOptions<DataverseOptions>
{
    public ValidateOptionsResult Validate(string? name, DataverseOptions options)
    {
        var errors = new List<string>();

        if (options.DynamicsApiEndpoint is null)
        {
            errors.Add($"{nameof(options.DynamicsApiEndpoint)} is required");
        }

        if (options.DynamicsApiBaseUri is null)
        {
            errors.Add($"{nameof(options.DynamicsApiBaseUri)} is required");
        }

        if (string.IsNullOrWhiteSpace(options.EntraId.TenantId))
        {
            errors.Add($"{nameof(options.EntraId.TenantId)} is required");
        }

        if (string.IsNullOrWhiteSpace(options.EntraId.ClientId))
        {
            errors.Add($"{nameof(options.EntraId.ClientId)} is required");
        }

        if (string.IsNullOrWhiteSpace(options.EntraId.ClientSecret))
        {
            errors.Add($"{nameof(options.EntraId.ClientSecret)} is required");
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}

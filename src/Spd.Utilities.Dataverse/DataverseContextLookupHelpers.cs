using System.Collections.Immutable;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Utilities.Dataverse;

public static class DataverseContextLookupHelpers
{
    public static readonly ImmutableDictionary<string, Guid> ServiceTypeGuidDictionary = new Dictionary<string, Guid>()
    {
        {"PSSO", Guid.Parse("f093141b-1e9d-ed11-b83d-00505683fbf4")},
        {"CRRP_EMPLOYEE", Guid.Parse("f193141b-1e9d-ed11-b83d-00505683fbf4")},
        {"CRRP_VOLUNTEER", Guid.Parse("a2834126-1e9d-ed11-b83d-00505683fbf4")},
        {"MCFD", Guid.Parse("a3834126-1e9d-ed11-b83d-00505683fbf4")},
        {"PE_CRC", Guid.Parse("a5d6ca3b-1e9d-ed11-b83d-00505683fbf4")},
        {"PE_CRC_VS", Guid.Parse("61a2ecee-58ae-ed11-b83e-00505683fbf4")},
        {"SecurityWorkerLicence", Guid.Parse("451101f8-58ae-ed11-b83e-00505683fbf4")},
        {"PSSO_VS", Guid.Parse("8c653cc7-64b9-ed11-b83e-00505683fbf4")},
        {"SecurityBusinessLicence", Guid.Parse("86aa9004-4c32-ee11-b845-00505683fbf4")},
        {"ArmouredVehiclePermit", Guid.Parse("b0572417-4c32-ee11-b845-00505683fbf4")},
        {"BodyArmourPermit", Guid.Parse("f504b223-4c32-ee11-b845-00505683fbf4")},
        {"MDRA", Guid.Parse("b212c347-4c32-ee11-b845-00505683fbf4")},
        {"SECURITY_BUSINESS_LICENCE_CONTROLLING_MEMBER_CRC", Guid.Parse("9c7cf246-c942-ee11-b845-00505683fbf4")},
        {"GDSDTeamCertification", Guid.Parse("36f14f8a-a3da-ef11-b856-00505683fbf4")},
        {"DogTrainerCertification", Guid.Parse("785f0797-a3da-ef11-b856-00505683fbf4")},
        {"RetiredServiceDogCertification", Guid.Parse("77d1a69e-a3da-ef11-b856-00505683fbf4")},
        {"SpecialProvincialConstable", Guid.Parse("f4a79298-e467-f011-b85a-00505683fbf4")}
    }.ToImmutableDictionary();

    public static Spd_ServiceType? LookupServiceType(this DataverseContext context, string? key)
    {
        if (key == null)
        {
            return null;
        }

        var keyExisted = ServiceTypeGuidDictionary.TryGetValue(key, out Guid guid);
        if (!keyExisted)
        {
            return null;
        }

        return context.Spd_ServiceTypeSet
            .Where(s => s.Spd_ServiceTypeId == guid)
            .Where(s => s.StateCode != Spd_ServiceType_StateCode.Inactive)
            .FirstOrDefault();
    }

    public static string GetServiceTypeName(Guid? serviceTypeId)
        => ServiceTypeGuidDictionary.FirstOrDefault(s => s.Value == serviceTypeId).Key;

    public static Guid? GetServiceTypeGuid(string serviceTypeName)
        => ServiceTypeGuidDictionary.TryGetValue(serviceTypeName, out Guid guid) ? guid : null;
}

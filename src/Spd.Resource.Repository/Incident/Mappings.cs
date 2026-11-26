using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Incident
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<global::Spd.Utilities.Dataverse.EarlyBound.Incident, IncidentResp>()
                .ConvertUsing(src => new IncidentResp
                {
                    ApplicationId = src.Spd_ApplicationId != null ? src.Spd_ApplicationId.Id : Guid.Empty,
                    IncidentId = src.IncidentId ?? Guid.Empty,
                    OrgId = src.Spd_OrganizationId != null ? src.Spd_OrganizationId.Id : null,
                    Title = src.Title ?? string.Empty,
                    ApproverName = src.Spd_DSGApproverName,
                    ApproverTitle = null,
                    Conditions = Enumerable.Empty<Condition>()
                });

            _ = CreateMap<Spd_LicenceCondition, Condition>()
                .ConvertUsing(src => new Condition
                {
                    Id = src.Spd_LicenceConditionId ?? Guid.Empty,
                    Name = src.Spd_ConditionName ?? string.Empty
                });
        }
    }
}

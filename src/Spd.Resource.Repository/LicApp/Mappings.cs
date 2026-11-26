using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.LicApp;

internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<Spd_Application, LicenceAppListResp>()
          .ForMember(d => d.LicenceAppId, opt => opt.MapFrom(s => s.Spd_ApplicationId))
          .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s.Spd_ServiceTypeId != null ? s.Spd_ServiceTypeId.Id : (Guid?)null)))
          .ForMember(d => d.CreatedOn, opt => opt.MapFrom(s => s.CreatedOn))
          .ForMember(d => d.SubmittedOn, opt => opt.MapFrom(s => s.Spd_SubmittedOn))
          .ForMember(d => d.UpdatedOn, opt => opt.MapFrom(s => s.ModifiedOn))
          .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceApplicationTypeEnum((int?)s.Spd_LicenceApplicationType)))
          .ForMember(d => d.CaseNumber, opt => opt.MapFrom(s => s.Spd_Name))
          .ForMember(d => d.ApplicationPortalStatusCode, opt => opt.MapFrom(s => s.Spd_PortalStatus == null ? null : ((ApplicationPortalStatus)(int)s.Spd_PortalStatus.Value).ToString()));

    }


}


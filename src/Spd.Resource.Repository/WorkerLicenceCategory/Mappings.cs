using AutoMapper;
using Spd.Resource.Repository.PersonLicApplication;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.WorkerLicenceCategory;

internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<Spd_LicenceCategory, WorkerLicenceCategoryResp>()
         .ForMember(d => d.WorkerCategoryTypeName, opt => opt.MapFrom(s => s.Spd_CategoryName))
         .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_LicenceCategoryId))
         .ForMember(d => d.WorkerCategoryTypeCode, opt => opt.MapFrom(s => GeWorkerCategoryTypeCode(s.Spd_LicenceCategoryId)));
    }

    internal static WorkerCategoryTypeEnum GeWorkerCategoryTypeCode(Guid? workerCatgoryId)
    {
        string key = global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LicenceCategoryDictionary.FirstOrDefault(s => s.Value == workerCatgoryId).Key;
        return Enum.Parse<WorkerCategoryTypeEnum>(key);
    }
}


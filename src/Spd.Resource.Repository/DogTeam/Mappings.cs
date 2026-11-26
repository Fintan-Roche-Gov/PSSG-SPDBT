using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.DogTeam
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Spd_DogTeam, DogTeamResp>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_DogTeamId))
            .ForMember(d => d.DogId, opt => opt.Ignore())
            .ForMember(d => d.HandlerId, opt => opt.Ignore())
            .ForMember(d => d.DogBreed, opt => opt.Ignore())
            .ForMember(d => d.MicrochipNumber, opt => opt.Ignore())
            .ForMember(d => d.DogColorAndMarkings, opt => opt.Ignore())
            .ForMember(d => d.DogDateOfBirth, opt => opt.Ignore())
            .ForMember(d => d.DogGender, opt => opt.Ignore())
            .ForMember(d => d.DogName, opt => opt.Ignore())
            .ForMember(d => d.IsDogTrainedByAccreditedSchool, opt => opt.Ignore())
            .ForMember(d => d.HandlerFirstName, opt => opt.Ignore())
            .ForMember(d => d.HandlerMiddleName1, opt => opt.Ignore())
            .ForMember(d => d.HandlerLastName, opt => opt.Ignore());
        }
    }
}

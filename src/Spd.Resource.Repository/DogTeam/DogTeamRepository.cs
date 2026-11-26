using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.DogTeam;
internal class DogTeamRepository : IDogTeamRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;

    public DogTeamRepository(DataverseContext ctx,
        IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    public async Task<DogTeamResp> GetAsync(Guid id, CancellationToken ct)
    {
        Spd_DogTeam? dogteam = _context.Spd_DogTeamSet
            .Where(d => d.Spd_DogTeamId == id)
            .FirstOrDefault();

        if (dogteam == null) return null;

        DogTeamResp response = _mapper.Map<DogTeamResp>(dogteam);

        if (dogteam.Spd_DogId != null)
        {
            Spd_Dog? dog = _context.Spd_DogSet.Where(d => d.Spd_DogId == dogteam.Spd_DogId.Id).FirstOrDefault();
            if (dog != null)
            {
                response.DogBreed = dog.Spd_Breed;
                response.MicrochipNumber = dog.Spd_MicrochipNumber;
                response.DogColorAndMarkings = dog.Spd_ColourAndMarkings;
                response.DogDateOfBirth = dog.Spd_DateOfBirth.HasValue ? DateOnly.FromDateTime(dog.Spd_DateOfBirth.Value) : null;
                response.DogGender = SharedMappingFuncs.GetEnum<Spd_Sex, GenderEnum>((int?)dog.Spd_Gender);
                response.DogName = dog.Spd_DogName;
                response.IsDogTrainedByAccreditedSchool = SharedMappingFuncs.GetBool((int?)dog.Spd_DogStrainingAccredited);
            }
        }

        if (dogteam.Spd_ContactId != null)
        {
            ContactEntity? contact = _context.ContactSet.Where(c => c.ContactId == dogteam.Spd_ContactId.Id).FirstOrDefault();
            if (contact != null)
            {
                response.HandlerFirstName = contact.FirstName;
                response.HandlerMiddleName1 = contact.Spd_MiddleName1;
                response.HandlerLastName = contact.LastName;
            }
        }

        return response;
    }

}


using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;

namespace Spd.Resource.Repository.Identity
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Spd_Identity, Identity>()
             .ForMember(d => d.IdentityNumber, opt => opt.MapFrom(s => s.Spd_IdentityNumber))
             .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_IdentityId))
             .ForMember(d => d.OrgId, opt => opt.MapFrom(s => s.Spd_OrganizationId))
             .ForMember(d => d.ContactId, opt => opt.Ignore())
             .ForMember(d => d.LastName, opt => opt.Ignore())
             .ForMember(d => d.FirstName, opt => opt.Ignore())
             .ForMember(d => d.MiddleName1, opt => opt.Ignore())
             .ForMember(d => d.MiddleName2, opt => opt.Ignore())
             .ForMember(d => d.EmailAddress, opt => opt.Ignore())
             .ForMember(d => d.BirthDate, opt => opt.Ignore())
             .ForMember(d => d.Gender, opt => opt.Ignore())
             .ForMember(d => d.Sub, opt => opt.MapFrom(s => s.Spd_UserGuid));

            _ = CreateMap<CreateIdentityCmd, Spd_Identity>()
             .ForMember(d => d.Spd_IdentityId, opt => opt.MapFrom(s => Guid.NewGuid()))
             .ForMember(d => d.Spd_UserGuid, opt => opt.MapFrom(s => s.UserGuid))
             .ForMember(d => d.Spd_OrgGuid, opt => opt.MapFrom(s => s.OrgGuid))
             .ForMember(d => d.Spd_Type, opt => opt.MapFrom(s => Enum.Parse<Spd_IdentityProvider>(s.IdentityProviderType.ToString())));

            _ = CreateMap<Spd_Identity, IdentityCmdResult>()
             .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_IdentityId));
        }

        internal static DateOnly? GetDateOnly(DateTime? date)
        {
            if (date == null) return null;
            return new DateOnly(date.Value.Year, date.Value.Month, date.Value.Day);
        }
        internal static GenderEnum? GetGenderEnum(Spd_Sex? optionset)
        {
            if (optionset == null) return null;
            return Enum.Parse<GenderEnum>(optionset.ToString());
        }
    }
}

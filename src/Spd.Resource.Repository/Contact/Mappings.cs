using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
namespace Spd.Resource.Repository.Contact
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<ContactEntity, ContactResp>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.ContactId))
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.FirstName))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.LastName))
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FullName))
            .ForMember(d => d.MiddleName1, opt => opt.MapFrom(s => s.Spd_MiddleName1))
            .ForMember(d => d.MiddleName2, opt => opt.MapFrom(s => s.Spd_MiddleName2))
            .ForMember(d => d.BirthDate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateOnly(s.Birthdate)))
            .ForMember(d => d.Gender, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_Sex, GenderEnum>((int?)s.Spd_Sex)))
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.EmailAddress1))
            .ForMember(d => d.BirthPlace, opt => opt.MapFrom(s => s.Spd_Birthplace))
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.Telephone1))
            .ForMember(d => d.ResidentialAddress, opt => opt.MapFrom(s => GetResidentialAddress(s)))
            .ForMember(d => d.MailingAddress, opt => opt.MapFrom(s => GetMailingAddress(s)))
            .ForMember(d => d.Aliases, opt => opt.MapFrom(s => GetUserEnteredAliases(s.Spd_Contact_Alias)))
            .ForMember(d => d.HasCriminalHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_SelfDisclosure)))
            .ForMember(d => d.CriminalChargeDescription, opt => opt.MapFrom(s => s.Spd_SelfDisclosureDetails))
            .ForMember(d => d.OtherOfficerRole, opt => opt.MapFrom(s => s.Spd_PeaceOfficerOther))
            .ForMember(d => d.IsPoliceOrPeaceOfficer, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_PeaceOfficer)))
            .ForMember(d => d.PoliceOfficerRoleCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_PoliceBackgroundRole, PoliceOfficerRoleEnum>((int?)s.Spd_PeaceOfficerStatus)))
            .ForMember(d => d.IsTreatedForMHC, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_MentalHealthCondition)))
            .ForMember(d => d.LicensingTermAgreedDateTime, opt => opt.MapFrom(s => s.Spd_LastLoggedInLicensingPortal))
            .ForMember(d => d.LastestScreeningLogin, opt => opt.MapFrom(s => s.Spd_LastLoggedInScreeningPortal))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.StateCode == Contact_StateCode.Active))
            .ForMember(d => d.HairColourCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetHairColorEnum((int?)s.Spd_HairColour)))
            .ForMember(d => d.EyeColourCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEyeColorEnum((int?)s.Spd_EyeColour)))
            .ForMember(d => d.Height, opt => opt.MapFrom(s => SharedMappingFuncs.GetHeightNumber(s.Spd_Height)))
            .ForMember(d => d.HeightUnitCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetHeightUnitCode(s.Spd_Height)))
            .ForMember(d => d.Weight, opt => opt.MapFrom(s => SharedMappingFuncs.GetWeightNumber(s.Spd_Weight)))
            .ForMember(d => d.WeightUnitCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetWeightUnitCode(s.Spd_Weight)));

            _ = CreateMap<ContactCmd, ContactEntity>()
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.FirstName)))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.LastName)))
            .ForMember(d => d.Birthdate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateFromDateOnly(s.BirthDate)))
            .ForMember(d => d.Spd_Sex, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<GenderEnum, Spd_Sex>(s.Gender)))
            .ForMember(d => d.EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName1)))
            .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName2)))
            .ForMember(d => d.Telephone1, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Address1_Line1, opt => opt.MapFrom(s => s.MailingAddress == null ? null : s.MailingAddress.AddressLine1))
            .ForMember(d => d.Address1_Line2, opt => opt.MapFrom(s => s.MailingAddress == null ? null : s.MailingAddress.AddressLine2))
            .ForMember(d => d.Address1_City, opt => opt.MapFrom(s => s.MailingAddress == null ? null : s.MailingAddress.City))
            .ForMember(d => d.Address1_Country, opt => opt.MapFrom(s => s.MailingAddress == null ? null : s.MailingAddress.Country))
            .ForMember(d => d.Address1_StateOrProvince, opt => opt.MapFrom(s => s.MailingAddress == null ? null : s.MailingAddress.Province))
            .ForMember(d => d.Address1_PostalCode, opt => opt.MapFrom(s => s.MailingAddress == null ? null : s.MailingAddress.PostalCode))
            .ForMember(d => d.Address2_Line1, opt => opt.MapFrom(s => s.ResidentialAddress == null ? null : s.ResidentialAddress.AddressLine1))
            .ForMember(d => d.Address2_Line2, opt => opt.MapFrom(s => s.ResidentialAddress == null ? null : s.ResidentialAddress.AddressLine2))
            .ForMember(d => d.Address2_City, opt => opt.MapFrom(s => s.ResidentialAddress == null ? null : s.ResidentialAddress.City))
            .ForMember(d => d.Address2_Country, opt => opt.MapFrom(s => s.ResidentialAddress == null ? null : s.ResidentialAddress.Country))
            .ForMember(d => d.Address2_StateOrProvince, opt => opt.MapFrom(s => s.ResidentialAddress == null ? null : s.ResidentialAddress.Province))
            .ForMember(d => d.Address2_PostalCode, opt => opt.MapFrom(s => s.ResidentialAddress == null ? null : s.ResidentialAddress.PostalCode))
            .ForMember(d => d.Spd_SelfDisclosure, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasCriminalHistory) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasCriminalHistory)))
            .ForMember(d => d.Spd_SelfDisclosureDetails, opt => opt.MapFrom(s => s.CriminalChargeDescription))
            .ForMember(d => d.Spd_PeaceOfficer, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsPoliceOrPeaceOfficer) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsPoliceOrPeaceOfficer)))
            .ForMember(d => d.Spd_PeaceOfficerStatus, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<PoliceOfficerRoleEnum, Spd_PoliceBackgroundRole>(s.PoliceOfficerRoleCode)))
            .ForMember(d => d.Spd_PeaceOfficerOther, opt => opt.MapFrom(s => s.OtherOfficerRole))
            .ForMember(d => d.Spd_MentalHealthCondition, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsTreatedForMHC) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsTreatedForMHC)))
            .ForMember(d => d.Spd_LastLoggedInLicensingPortal, opt => opt.Ignore())
            .ForMember(d => d.Spd_LastLoggedInScreeningPortal, opt => opt.Ignore())
            .ForMember(d => d.Spd_HairColour, opt => opt.MapFrom(s => SharedMappingFuncs.GetHairColor(s.HairColourCode)))
            .ForMember(d => d.Spd_EyeColour, opt => opt.MapFrom(s => SharedMappingFuncs.GetEyeColor(s.EyeColourCode)))
            .ForMember(d => d.Spd_Height, opt => opt.MapFrom(s => SharedMappingFuncs.GetHeightStr(s.Height, s.HeightUnitCode)))
            .ForMember(d => d.Spd_Weight, opt => opt.MapFrom(s => SharedMappingFuncs.GetWeightStr(s.Weight, s.WeightUnitCode)));

            _ = CreateMap<CreateContactCmd, ContactEntity>()
            .ForMember(d => d.ContactId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .IncludeBase<ContactCmd, ContactEntity>();

            _ = CreateMap<UpdateContactCmd, ContactEntity>()
            .ForMember(d => d.ContactId, opt => opt.MapFrom(s => s.Id))
            .IncludeBase<ContactCmd, ContactEntity>();

            _ = CreateMap<Spd_Licence, LicenceInfo>()
             .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_LicenceId))
             .ForMember(d => d.LicenceNumber, opt => opt.MapFrom(s => s.Spd_LicenceNumber))
             .ForMember(d => d.ExpiryDate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateOnlyFromDateTimeOffset(s.Spd_ExpiryDate)));
        }

        private static ResidentialAddr? GetResidentialAddress(ContactEntity contact)
        {
            if (string.IsNullOrWhiteSpace(contact.Address2_Line1) && string.IsNullOrEmpty(contact.Address2_Line2)) return null;
            ResidentialAddr addr = new();
            addr.AddressLine1 = contact.Address2_Line1;
            addr.AddressLine2 = contact.Address2_Line2;
            addr.City = contact.Address2_City;
            addr.Country = contact.Address2_Country;
            addr.Province = contact.Address2_StateOrProvince;
            addr.PostalCode = contact.Address2_PostalCode;
            return addr;
        }

        private static MailingAddr? GetMailingAddress(ContactEntity contact)
        {
            if (string.IsNullOrWhiteSpace(contact.Address1_Line1) && string.IsNullOrEmpty(contact.Address1_Line2)) return null;
            MailingAddr addr = new();
            addr.AddressLine1 = contact.Address1_Line1;
            addr.AddressLine2 = contact.Address1_Line2;
            addr.City = contact.Address1_City;
            addr.Country = contact.Address1_Country;
            addr.Province = contact.Address1_StateOrProvince;
            addr.PostalCode = contact.Address1_PostalCode;
            return addr;
        }

        private static IEnumerable<Spd_Alias> GetUserEnteredAliases(IEnumerable<Spd_Alias> aliases) => aliases
            .Where(a => a.StateCode == Spd_Alias_StateCode.Active &&
                a.Spd_Source == Spd_Alias_Spd_Source.UserEntered);
    }
}

using AutoMapper;
using Spd.Resource.Repository.GDSDApp;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;

namespace Spd.Resource.Repository.RetiredDogApp;

internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<RetiredDogApp, Spd_Application>()
            .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Spd_LicenceApplicationType, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<ApplicationTypeEnum, Spd_LicenceApplicationType>(s.ApplicationTypeCode)))
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
            .ForMember(d => d.Spd_LastName, opt => opt.MapFrom(s => s.Surname))
            .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName))
            .ForMember(d => d.Spd_DateOfBirth, opt => opt.MapFrom(s => ToDateTime(s.DateOfBirth)))
            .ForMember(d => d.Spd_EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Spd_Origin, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<ApplicationOriginTypeEnum, Incident_CaseOriginCode>(s.ApplicationOriginTypeCode)))
            .ForMember(d => d.StateCode, opt => opt.MapFrom(_ => Spd_Application_StateCode.Active))
            .ForMember(d => d.Spd_NameOfApplicantOrLegalGuardian, opt => opt.MapFrom(s => s.ApplicantOrLegalGuardianName))
            .ForMember(d => d.Spd_AddressLine1, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.AddressLine1)))
            .ForMember(d => d.Spd_AddressLine2, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.AddressLine2)))
            .ForMember(d => d.Spd_City, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.City)))
            .ForMember(d => d.Spd_PostalCode, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.PostalCode)))
            .ForMember(d => d.Spd_Province, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.Province)))
            .ForMember(d => d.Spd_Country, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.Country)))
            .ForMember(d => d.Spd_SubmittedOn, opt => opt.Ignore())
            .ForMember(d => d.Spd_PortalModifiedOn, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Spd_LicenceTerm, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<LicenceTermEnum, Spd_LicenceTerm>(s.LicenceTermCode)))
            .ForMember(d => d.Spd_Declaration, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
            .ForMember(d => d.Spd_Consent, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
            .ForMember(d => d.Spd_DeclarationDate, opt => opt.MapFrom(s => GetDeclarationDate(s.AgreeToCompleteAndAccurate)))
            .ForMember(d => d.Spd_IdentityConfirmed, opt => opt.MapFrom(s =>
                s.ApplicationTypeCode.HasValue && SharedMappingFuncs.GetIdentityConfirmed(s.ApplicationOriginTypeCode, s.ApplicationTypeCode.Value)))
            .ForMember(d => d.Spd_DogsRetirementDate, opt => opt.MapFrom(s => ToDateTime(s.DogRetiredDate)))
            .ForMember(d => d.Spd_GDSDCertificateNumber, opt => opt.MapFrom(s => s.CurrentGDSDCertificateNumber))
            .ForMember(d => d.Spd_DogType, opt => opt.MapFrom(_ => Spd_DogType.ServiceDog))
            .ForMember(d => d.Spd_WillDogLiveWithHandlerInRetirement, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.ConfirmDogLiveWithYouAfterRetire)))
            .AfterMap((s, d, context) =>
            {
                if (s.DogInfo != null)
                    context.Mapper.Map(s.DogInfo, d);
            });

        _ = CreateMap<CreateRetiredDogAppCmd, Spd_Application>()
            .IncludeBase<RetiredDogApp, Spd_Application>()
            .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Spd_SubmittedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));

        _ = CreateMap<SaveRetiredDogAppCmd, Spd_Application>()
            .IncludeBase<RetiredDogApp, Spd_Application>()
            .ForMember(d => d.Spd_ApplicationId, opt => opt.Ignore())
            .ForMember(d => d.StatusCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<ApplicationStatusEnum, Spd_Application_StatusCode>(s.ApplicationStatusEnum)));

        _ = CreateMap<RetiredDogApp, ContactEntity>()
            .ForMember(d => d.ContactId, opt => opt.Condition((_, dest, _) => dest.ContactId == null))
            .ForMember(d => d.ContactId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.GivenName)))
            .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName)))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.Surname)))
            .ForMember(d => d.EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Birthdate, opt => opt.MapFrom(s => ToDateTime(s.DateOfBirth)))
            .ForMember(d => d.Telephone1, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Address1_Line1, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.AddressLine1)))
            .ForMember(d => d.Address1_Line2, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.AddressLine2)))
            .ForMember(d => d.Address1_City, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.City)))
            .ForMember(d => d.Address1_PostalCode, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.PostalCode)))
            .ForMember(d => d.Address1_StateOrProvince, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.Province)))
            .ForMember(d => d.Address1_Country, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress == null ? null : s.MailingAddress.Country)));

        _ = CreateMap<CreateRetiredDogAppCmd, ContactEntity>()
            .IncludeBase<RetiredDogApp, ContactEntity>();

        _ = CreateMap<SaveRetiredDogAppCmd, ContactEntity>()
            .IncludeBase<RetiredDogApp, ContactEntity>();

        _ = CreateMap<DogInfo, Spd_Application>()
            .ForMember(d => d.Spd_DogName, opt => opt.MapFrom(s => s.DogName))
            .ForMember(d => d.Spd_DogsDateOfBirth, opt => opt.MapFrom(s => ToDateTime(s.DogDateOfBirth)))
            .ForMember(d => d.Spd_DogBreed, opt => opt.MapFrom(s => s.DogBreed))
            .ForMember(d => d.Spd_DogColourAndMarkings, opt => opt.MapFrom(s => s.DogColorAndMarkings))
            .ForMember(d => d.Spd_DogsGender, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<GenderEnum, Spd_Sex>(s.DogGender)))
            .ForMember(d => d.Spd_MicrochipNumber, opt => opt.MapFrom(s => s.MicrochipNumber));

        _ = CreateMap<Spd_Application, DogInfo>()
            .ForMember(d => d.DogName, opt => opt.MapFrom(s => s.Spd_DogName))
            .ForMember(d => d.DogDateOfBirth, opt => opt.MapFrom(s => ToDateOnly(s.Spd_DogsDateOfBirth)))
            .ForMember(d => d.DogBreed, opt => opt.MapFrom(s => s.Spd_DogBreed))
            .ForMember(d => d.DogColorAndMarkings, opt => opt.MapFrom(s => s.Spd_DogColourAndMarkings))
            .ForMember(d => d.DogGender, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetEnum<Spd.Utilities.Dynamics.GenderOptionSet, GenderEnum>((int?)s.Spd_DogsGender)))
            .ForMember(d => d.MicrochipNumber, opt => opt.MapFrom(s => s.Spd_MicrochipNumber));

        _ = CreateMap<Spd_Application, RetiredDogApp>()
            .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetServiceType(s.Spd_ServiceTypeId == null ? null : s.Spd_ServiceTypeId.Id)))
            .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetLicenceApplicationTypeEnum((int?)s.Spd_LicenceApplicationType)))
            .ForMember(d => d.LicenceTermCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetLicenceTermEnum((int?)s.Spd_LicenceTerm)))
            .ForMember(d => d.ApplicationOriginTypeCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetEnum<Spd.Utilities.Dynamics.ApplicationOriginOptionSet, ApplicationOriginTypeEnum>((int?)s.Spd_Origin)))
            .ForMember(d => d.AgreeToCompleteAndAccurate, opt => opt.MapFrom(s => s.Spd_Declaration))
            .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.Spd_LastName))
            .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.Spd_FirstName))
            .ForMember(d => d.MiddleName, opt => opt.MapFrom(s => s.Spd_MiddleName1))
            .ForMember(d => d.MailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)))
            .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => ToDateOnly(s.Spd_DateOfBirth)))
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.Spd_PhoneNumber))
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Spd_EmailAddress1))
            .ForMember(d => d.ApplicantOrLegalGuardianName, opt => opt.MapFrom(s => s.Spd_NameOfApplicantOrLegalGuardian))
            .ForMember(d => d.DogInfo, opt => opt.MapFrom(s => s))
            .ForMember(d => d.CurrentGDSDCertificateNumber, opt => opt.MapFrom(s => s.Spd_GDSDCertificateNumber))
            .ForMember(d => d.DogRetiredDate, opt => opt.MapFrom(s => ToDateOnly(s.Spd_DogsRetirementDate)))
            .ForMember(d => d.ConfirmDogLiveWithYouAfterRetire, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetBool((int?)s.Spd_WillDogLiveWithHandlerInRetirement)));

        _ = CreateMap<Spd_Application, RetiredDogAppResp>()
            .IncludeBase<Spd_Application, RetiredDogApp>()
            .ForMember(d => d.ApplicationPortalStatus, opt => opt.MapFrom(s => GetPortalStatus(s.Spd_PortalStatus)))
            .ForMember(d => d.CaseNumber, opt => opt.MapFrom(s => s.Spd_Name))
            .ForMember(d => d.LicenceAppId, opt => opt.MapFrom(s => s.Spd_ApplicationId))
            .ForMember(d => d.ApplicantId, opt => opt.MapFrom(s => s.Spd_ApplicantId == null ? Guid.Empty : s.Spd_ApplicantId.Id));
    }

    private static DateTime? ToDateTime(DateOnly? date)
    {
        return date.HasValue
            ? DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
    }

    private static DateOnly? ToDateOnly(DateTime? date)
    {
        return date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
    }

    private static string? GetAddressPart(string? value)
    {
        return value == null ? null : StringHelper.SanitizeEmpty(value);
    }

    private static DateTime? GetDeclarationDate(bool? agree)
    {
        return SharedMappingFuncs.GetDeclarationDate(agree)?.UtcDateTime;
    }

    private static ApplicationPortalStatusEnum? GetPortalStatus(Spd_PortalStatus? portalStatus)
    {
        return portalStatus.HasValue
            ? Enum.Parse<ApplicationPortalStatusEnum>(portalStatus.Value.ToString())
            : null;
    }
}

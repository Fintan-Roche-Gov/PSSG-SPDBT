using AutoMapper;
using Spd.Resource.Repository.Alias;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;
using ContactDynamics = Spd.Utilities.Dataverse.EarlyBound.Contact;
namespace Spd.Resource.Repository.ControllingMemberCrcApplication;
internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<ControllingMemberCrcApplication, ContactDynamics>()
        .ForMember(d => d.FirstName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.GivenName)))
        .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName1)))
        .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName2)))
        .ForMember(d => d.LastName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.Surname)))
        .ForMember(d => d.EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
        .ForMember(d => d.Spd_Sex, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<GenderEnum, Spd_Sex>(s.GenderCode)))
        .ForMember(d => d.GenderCode, opt => opt.Ignore())
        .ForMember(d => d.Birthdate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateFromDateOnly(s.DateOfBirth)))
        .ForMember(d => d.Telephone1, opt => opt.MapFrom(s => s.PhoneNumber))
        .ForMember(d => d.Spd_BcDriversLicense, opt => opt.MapFrom(s => s.BcDriversLicenceNumber))
        .ForMember(d => d.Spd_Birthplace, opt => opt.Ignore())
        .ForMember(d => d.Address2_Line1, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : StringHelper.SanitizeEmpty(s.ResidentialAddressData.AddressLine1)))
        .ForMember(d => d.Address2_Line2, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : StringHelper.SanitizeEmpty(s.ResidentialAddressData.AddressLine2)))
        .ForMember(d => d.Address2_City, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : StringHelper.SanitizeEmpty(s.ResidentialAddressData.City)))
        .ForMember(d => d.Address2_PostalCode, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : StringHelper.SanitizeEmpty(s.ResidentialAddressData.PostalCode)))
        .ForMember(d => d.Address2_StateOrProvince, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : StringHelper.SanitizeEmpty(s.ResidentialAddressData.Province)))
        .ForMember(d => d.Address2_Country, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : StringHelper.SanitizeEmpty(s.ResidentialAddressData.Country)))
        .ForMember(d => d.Spd_SelfDisclosure, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasCriminalHistory) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasCriminalHistory)))
        .ForMember(d => d.Spd_SelfDisclosureDetails, opt => opt.MapFrom(s => s.CriminalHistoryDetail))
        .ForMember(d => d.Spd_PeaceOfficer, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsPoliceOrPeaceOfficer) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsPoliceOrPeaceOfficer)))
        .ForMember(d => d.Spd_PeaceOfficerStatus, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<PoliceOfficerRoleEnum, Spd_PoliceBackgroundRole>(s.PoliceOfficerRoleCode)))
        .ForMember(d => d.Spd_PeaceOfficerOther, opt => opt.MapFrom(s => s.OtherOfficerRole))
        .ForMember(d => d.Spd_MentalHealthCondition, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsTreatedForMHC) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsTreatedForMHC)))
        .ReverseMap()
        .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateOnly(s.Birthdate)))
        .ForMember(d => d.GenderCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_Sex, GenderEnum>((int?)s.Spd_Sex)))
        .ForMember(d => d.ResidentialAddressData, opt => opt.MapFrom(s => GetResidentialAddressData(s)))
        .ForMember(d => d.HasCriminalHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_SelfDisclosure)))
        .ForMember(d => d.IsPoliceOrPeaceOfficer, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_PeaceOfficer)))
        .ForMember(d => d.PoliceOfficerRoleCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_PoliceBackgroundRole, PoliceOfficerRoleEnum>((int?)s.Spd_PeaceOfficerStatus)))
        .ForMember(d => d.IsTreatedForMHC, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_MentalHealthCondition)))
        .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.FirstName))
        .ForMember(d => d.MiddleName1, opt => opt.MapFrom(s => s.Spd_MiddleName1))
        .ForMember(d => d.MiddleName2, opt => opt.MapFrom(s => s.Spd_MiddleName2))
        .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.LastName))
        .ForMember(d => d.BcDriversLicenceNumber, opt => opt.Ignore());
        _ = CreateMap<ContactDynamics, ControllingMemberCrcApplicationResp>()
        .IncludeBase<ContactDynamics, ControllingMemberCrcApplication>();

        _ = CreateMap<CreateControllingMemberCrcAppCmd, ContactDynamics>()
        .IncludeBase<ControllingMemberCrcApplication, ContactDynamics>()
        .ForMember(d => d.ContactId, opt => opt.MapFrom(s => Guid.NewGuid()));

        _ = CreateMap<ControllingMemberCrcApplication, Spd_Application>()
         .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => Guid.NewGuid()))
         .ForMember(d => d.Spd_LicenceApplicationType, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationTypeEnum, Spd_LicenceApplicationType>(s.ApplicationTypeCode)))
         .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
         .ForMember(d => d.Spd_LastName, opt => opt.MapFrom(s => s.Surname))
         .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName1))
         .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => s.MiddleName2))
         .ForMember(d => d.Spd_Origin, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationOriginTypeEnum, Incident_CaseOriginCode>(s.ApplicationOriginTypeCode)))
         .ForMember(d => d.Spd_Payer, opt => opt.MapFrom(s => Spd_PayerType.Organization))
         .ForMember(d => d.Spd_DateOfBirth, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateFromDateOnly(s.DateOfBirth)))
         .ForMember(d => d.Spd_Sex, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<GenderEnum, Spd_Sex>(s.GenderCode)))
         .ForMember(d => d.Spd_CriminalHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasCriminalHistory) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasCriminalHistory)))
         .ForMember(d => d.Spd_CourtJudgementHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasCourtJudgement) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasCourtJudgement)))
         .ForMember(d => d.Spd_BcDriversLicense, opt => opt.MapFrom(s => s.BcDriversLicenceNumber))
         .ForMember(d => d.Spd_EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
         .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber))
         .ForMember(d => d.Spd_ResidentialAddress1, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : s.ResidentialAddressData.AddressLine1))
         .ForMember(d => d.Spd_ResidentialAddress2, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : s.ResidentialAddressData.AddressLine2))
         .ForMember(d => d.Spd_ResidentialCity, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : s.ResidentialAddressData.City))
         .ForMember(d => d.Spd_ResidentialCountry, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : s.ResidentialAddressData.Country))
         .ForMember(d => d.Spd_ResidentialProvince, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : s.ResidentialAddressData.Province))
         .ForMember(d => d.Spd_ResidentialPostalCode, opt => opt.MapFrom(s => s.ResidentialAddressData == null ? null : s.ResidentialAddressData.PostalCode))
         .ForMember(d => d.Spd_PoliceBackgroundRole, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<PoliceOfficerRoleEnum, Spd_PoliceBackgroundRole>(s.PoliceOfficerRoleCode)))
         .ForMember(d => d.Spd_PoliceBackgroundOther, opt => opt.MapFrom(s => s.OtherOfficerRole))
         .ForMember(d => d.Spd_MentalHealthCondition, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsTreatedForMHC) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsTreatedForMHC)))
         .ForMember(d => d.Spd_BankruptcyHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasBankruptcyHistory) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasBankruptcyHistory)))
         .ForMember(d => d.Spd_BankruptcyHistoryDetails, opt => opt.MapFrom(s => s.BankruptcyHistoryDetail))
         .ForMember(d => d.Spd_CanadianCitizen, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsCanadianCitizen) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsCanadianCitizen)))
         .ForMember(d => d.Spd_HasPreviousNames, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasPreviousName) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasPreviousName)))
         .ForMember(d => d.Spd_HasDriversLicence, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasBcDriversLicence) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasBcDriversLicence)))
         .ForMember(d => d.StateCode, opt => opt.MapFrom(s => Spd_Application_StateCode.Active))
         .ForMember(d => d.Spd_SubmittedOn, opt => opt.Ignore())
         .ForMember(d => d.Spd_LicenceFee, opt => opt.MapFrom(s => new Microsoft.Xrm.Sdk.Money(0)))
         .ForMember(d => d.Spd_Declaration, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
         .ForMember(d => d.Spd_DeclarationDate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDeclarationDate(s.AgreeToCompleteAndAccurate)))
         .ForMember(d => d.Spd_Consent, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
         .ForMember(d => d.Spd_UploadedDocuments, opt => opt.MapFrom(s => SharedMappingFuncs.GetUploadedDocumentOptionSets(s.UploadedDocumentEnums)))
         .ForMember(d => d.Spd_CriminalChargesConvictionsDetails, opt => opt.MapFrom(s => s.CriminalHistoryDetail))
         .ForMember(d => d.Spd_PortalModifiedOn, opt => opt.MapFrom(s => DateTimeOffset.UtcNow))
         .ForMember(d => d.Spd_IdentityConfirmed, opt => opt.MapFrom(s => SharedMappingFuncs.GetIdentityConfirmed(s.ApplicationOriginTypeCode, s.ApplicationTypeCode)))
         .ReverseMap()
         .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Spd_EmailAddress1))
         .ForMember(d => d.ApplicationOriginTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Incident_CaseOriginCode, ApplicationOriginTypeEnum>((int?)s.Spd_Origin)))
         .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateOnly(s.Spd_DateOfBirth)))
         .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.Spd_FirstName))
         .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.Spd_LastName))
         .ForMember(d => d.MiddleName1, opt => opt.MapFrom(s => s.Spd_MiddleName1))
         .ForMember(d => d.MiddleName2, opt => opt.MapFrom(s => s.Spd_MiddleName2))
         .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s.Spd_ServiceTypeId != null ? s.Spd_ServiceTypeId.Id : (Guid?)null)))
         .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceApplicationTypeEnum((int?)s.Spd_LicenceApplicationType)))
         .ForMember(d => d.GenderCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_Sex, GenderEnum>((int?)s.Spd_Sex)))
         .ForMember(d => d.HasCriminalHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_CriminalHistory)))
         .ForMember(d => d.ResidentialAddressData, opt => opt.MapFrom(s => GetResidentialAddressData(s)))
         .ForMember(d => d.IsTreatedForMHC, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_MentalHealthCondition)))
         .ForMember(d => d.IsCanadianCitizen, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_CanadianCitizen)))
         .ForMember(d => d.HasBcDriversLicence, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_HasDriversLicence)))
         .ForMember(d => d.HasPreviousName, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_HasPreviousNames)))
         .ForMember(d => d.PoliceOfficerRoleCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_PoliceBackgroundRole, PoliceOfficerRoleEnum>((int?)s.Spd_PoliceBackgroundRole)))
         .ForMember(d => d.HasBankruptcyHistory, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_BankruptcyHistory)))
         .ForMember(d => d.HasCourtJudgement, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_CourtJudgementHistory)))
         .ForMember(d => d.UploadedDocumentEnums, opt => opt.MapFrom(s => SharedMappingFuncs.GetUploadedDocumentEnums(s.Spd_UploadedDocuments.ToString())));

        _ = CreateMap<CreateControllingMemberCrcAppCmd, Spd_Application>()
          .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => Guid.NewGuid()))
          .ForMember(d => d.Spd_SubmittedOn, opt => opt.MapFrom(s => DateTimeOffset.UtcNow))
          .IncludeBase<ControllingMemberCrcApplication, Spd_Application>();

        _ = CreateMap<Spd_Application, ControllingMemberCrcApplicationResp>()
          .ForMember(d => d.ContactId, opt => opt.MapFrom(s => s.Spd_ApplicantId != null ? s.Spd_ApplicantId.Id : Guid.Empty))
          .ForMember(d => d.ControllingMemberAppId, opt => opt.MapFrom(s => s.Spd_ApplicationId))
          .ForMember(d => d.CaseNumber, opt => opt.MapFrom(s => s.Spd_Name))
          .ForMember(d => d.OrganizationId, opt => opt.MapFrom(s => s.Spd_OrganizationId != null ? s.Spd_OrganizationId.Id : (Guid?)null))
          .IncludeBase<Spd_Application, ControllingMemberCrcApplication>();

        _ = CreateMap<SaveControllingMemberCrcAppCmd, Spd_Application>()
            .ForMember(d => d.StatusCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationStatusEnum, Spd_Application_StatusCode>(s.ApplicationStatusEnum)))
            .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => s.ControllingMemberAppId ?? Guid.NewGuid()))
            .IncludeBase<ControllingMemberCrcApplication, Spd_Application>();

        _ = CreateMap<SaveControllingMemberCrcAppCmd, ContactDynamics>()
            .ForMember(d => d.ContactId, opt => opt.MapFrom(s => s.ContactId == Guid.Empty ? Guid.NewGuid() : s.ContactId))
          .IncludeBase<ControllingMemberCrcApplication, ContactDynamics>();

        _ = CreateMap<AliasResp, Spd_Alias>()
          .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
          .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.Surname))
          .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName1))
          .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => s.MiddleName2))
          .ForMember(d => d.Spd_Source, opt => opt.MapFrom(s => Spd_Alias_Spd_Source.UserEntered))
          .ReverseMap();

    }
    private static ResidentialAddr? GetResidentialAddressData(ContactDynamics c)
    {
        ResidentialAddr address = new();
        address.AddressLine1 = c.Address2_Line1;
        address.AddressLine2 = c.Address2_Line2;
        address.City = c.Address2_City;
        address.Province = c.Address2_StateOrProvince;
        address.Country = c.Address2_Country;
        address.PostalCode = c.Address2_PostalCode;
        return address;
    }
    private static ResidentialAddr? GetResidentialAddressData(Spd_Application app)
    {
        ResidentialAddr address = new();
        address.AddressLine1 = app.Spd_ResidentialAddress1;
        address.AddressLine2 = app.Spd_ResidentialAddress2;
        address.City = app.Spd_ResidentialCity;
        address.Province = app.Spd_ResidentialProvince;
        address.Country = app.Spd_ResidentialCountry;
        address.PostalCode = app.Spd_ResidentialPostalCode;
        return address;
    }
}

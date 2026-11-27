using AutoMapper;
using Microsoft.Dynamics.CRM.Microsoft.Dynamics.CRM;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;

namespace Spd.Resource.Repository.GDSDApp;
internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<GDSDApp, Spd_Application>()
            .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Spd_LicenceApplicationType, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<ApplicationTypeEnum, Spd_LicenceApplicationType>(s.ApplicationTypeCode)))
            .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
            .ForMember(d => d.Spd_LastName, opt => opt.MapFrom(s => s.Surname))
            .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName))
            .ForMember(d => d.Spd_DateOfBirth, opt => opt.MapFrom(s => GetDataverseDate(s.DateOfBirth)))
            .ForMember(d => d.Spd_EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Spd_Origin, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<ApplicationOriginTypeEnum, Incident_CaseOriginCode>(s.ApplicationOriginTypeCode)))
            .ForMember(d => d.StateCode, opt => opt.MapFrom(_ => Spd_Application_StateCode.Active))
            .ForMember(d => d.Spd_NameOfApplicantOrLegalGuardian, opt => opt.MapFrom(s => s.ApplicantOrLegalGuardianName))
            .ForMember(d => d.Spd_DogStrainingAccredited, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsDogTrainedByAccreditedSchool)))
            .ForMember(d => d.Spd_AddressLine1, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.AddressLine1)))
            .ForMember(d => d.Spd_AddressLine2, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.AddressLine2)))
            .ForMember(d => d.Spd_City, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.City)))
            .ForMember(d => d.Spd_PostalCode, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.PostalCode)))
            .ForMember(d => d.Spd_Province, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.Province)))
            .ForMember(d => d.Spd_Country, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.Country)))
            .ForMember(d => d.Spd_SubmittedOn, opt => opt.Ignore())
            .ForMember(d => d.Spd_PortalModifiedOn, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Spd_LicenceTerm, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<LicenceTermEnum, Spd_LicenceTerm>(s.LicenceTermCode)))
            .ForMember(d => d.Spd_Declaration, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
            .ForMember(d => d.Spd_Consent, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
            .ForMember(d => d.Spd_DeclarationDate, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetDeclarationDate(s.AgreeToCompleteAndAccurate).HasValue
                    ? SharedMappingFuncs.GetDeclarationDate(s.AgreeToCompleteAndAccurate)!.Value.UtcDateTime
                    : (DateTime?)null))
            .ForMember(d => d.Spd_IdentityConfirmed, opt => opt.MapFrom(s =>
                s.ApplicationTypeCode.HasValue && SharedMappingFuncs.GetIdentityConfirmed(s.ApplicationOriginTypeCode, s.ApplicationTypeCode.Value)))
            .AfterMap((s, d, context) =>
            {
                if (s.DogInfo != null)
                    context.Mapper.Map(s.DogInfo, d);
            });

        _ = CreateMap<CreateGDSDAppCmd, Spd_Application>()
            .IncludeBase<GDSDApp, Spd_Application>()
            .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Spd_SubmittedOn, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Spd_DogsRequiredForDisabilityOrImpairment, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsAssistanceStillRequired)));

        _ = CreateMap<SaveGDSDAppCmd, Spd_Application>()
            .IncludeBase<GDSDApp, Spd_Application>()
            .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => s.LicenceAppId ?? Guid.NewGuid()))
            .ForMember(d => d.StatusCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<ApplicationStatusEnum, Spd_Application_StatusCode>(s.ApplicationStatusEnum)));

        _ = CreateMap<GDSDApp, Contact>()
            .ForMember(d => d.ContactId, opt => opt.Condition((_, dest, _) => dest.ContactId == null))
            .ForMember(d => d.ContactId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.GivenName)))
            .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName)))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.Surname)))
            .ForMember(d => d.EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
            .ForMember(d => d.Birthdate, opt => opt.MapFrom(s => GetDataverseDate(s.DateOfBirth)))
            .ForMember(d => d.Telephone1, opt => opt.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.Address1_Line1, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.AddressLine1)))
            .ForMember(d => d.Address1_Line2, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.AddressLine2)))
            .ForMember(d => d.Address1_City, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.City)))
            .ForMember(d => d.Address1_PostalCode, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.PostalCode)))
            .ForMember(d => d.Address1_StateOrProvince, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.Province)))
            .ForMember(d => d.Address1_Country, opt => opt.MapFrom(s => GetAddressPart(s.MailingAddress?.Country)));

        _ = CreateMap<CreateGDSDAppCmd, Contact>()
            .IncludeBase<GDSDApp, Contact>();

        _ = CreateMap<SaveGDSDAppCmd, Contact>()
            .IncludeBase<GDSDApp, Contact>();

        _ = CreateMap<AccreditedSchoolQuestions, Spd_Application>()
            .ForMember(d => d.Spd_DogType, opt => opt.MapFrom(s => (Spd_DogType?)GetDogTypeOptionSet(s.IsGuideDog)))
            .ForMember(d => d.Spd_DogsAssistanceInDailyLiving, opt => opt.MapFrom(s =>
                s.IsGuideDog == true ? null : s.ServiceDogTasks))
            .ForMember(d => d.Spd_DogSpayedOrNeutered, opt => opt.MapFrom(_ => Spd_YesNo.Yes));

        _ = CreateMap<NonAccreditedSchoolQuestions, Spd_Application>()
            .ForMember(d => d.Spd_DogType, opt => opt.MapFrom(_ => Spd_DogType.ServiceDog))
            .ForMember(d => d.Spd_DogsInoculationsUpToDate, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.AreInoculationsUpToDate)))
            .ForMember(d => d.Spd_DogSpayedOrNeutered, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.IsDogSterilized)))
            .ForMember(d => d.Spd_DoctorIsProvidingGDSDMedicalForm, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.DoctorIsProvidingNeedDogMedicalForm)));

        _ = CreateMap<DogInfo, Spd_Application>()
            .ForMember(d => d.Spd_DogName, opt => opt.MapFrom(s => s.DogName))
            .ForMember(d => d.Spd_DogsDateOfBirth, opt => opt.MapFrom(s => GetDataverseDate(s.DogDateOfBirth)))
            .ForMember(d => d.Spd_DogBreed, opt => opt.MapFrom(s => s.DogBreed))
            .ForMember(d => d.Spd_DogColourAndMarkings, opt => opt.MapFrom(s => s.DogColorAndMarkings))
            .ForMember(d => d.Spd_DogsGender, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetOptionset<GenderEnum, Spd_Sex>(s.DogGender)))
            .ForMember(d => d.Spd_MicrochipNumber, opt => opt.MapFrom(s => s.MicrochipNumber));

        _ = CreateMap<GraduationInfo, Spd_DogTrainingSchool>()
            .ForMember(d => d.Spd_TrainingSchoolName, opt => opt.MapFrom(s => s.AccreditedSchoolName))
            .ForMember(d => d.Spd_ContactSurname, opt => opt.MapFrom(s => s.SchoolContactSurname))
            .ForMember(d => d.Spd_ContactFirstname, opt => opt.MapFrom(s => s.SchoolContactGivenName))
            .ForMember(d => d.Spd_ContactPhone, opt => opt.MapFrom(s => s.SchoolContactPhoneNumber))
            .ForMember(d => d.Spd_ContactEmail, opt => opt.MapFrom(s => s.SchoolContactEmailAddress));

        _ = CreateMap<TrainingSchoolInfo, Spd_DogTrainingSchool>()
            .ForMember(d => d.Spd_TrainingSchoolName, opt => opt.MapFrom(s => s.TrainingBizName))
            .ForMember(d => d.Spd_ContactFirstname, opt => opt.MapFrom(s => s.ContactGivenName))
            .ForMember(d => d.Spd_ContactSurname, opt => opt.MapFrom(s => s.ContactSurname))
            .ForMember(d => d.Spd_ContactEmail, opt => opt.MapFrom(s => s.ContactEmailAddress))
            .ForMember(d => d.Spd_ContactPhone, opt => opt.MapFrom(s => s.ContactPhoneNumber))
            .ForMember(d => d.Spd_TrainingHours, opt => opt.MapFrom(s => s.TotalTrainingHours))
            .ForMember(d => d.Spd_StartDate, opt => opt.MapFrom(s => GetDataverseDate(s.TrainingStartDate)))
            .ForMember(d => d.Spd_EndDate, opt => opt.MapFrom(s => GetDataverseDate(s.TrainingEndDate)))
            .ForMember(d => d.Spd_NameAndOrTypeOfTrainingProgram, opt => opt.MapFrom(s => s.TrainingName))
            .ForMember(d => d.Spd_TrainingDetails, opt => opt.MapFrom(s => s.WhatLearned))
            .ForMember(d => d.Spd_TrainingSchoolType, opt => opt.MapFrom(_ => Spd_DogTrainingSchoolType.UnaccreditedSchool))
            .ForMember(d => d.Spd_Street1, opt => opt.MapFrom(s => GetAddressPart(s.TrainingBizMailingAddress?.AddressLine1)))
            .ForMember(d => d.Spd_Street2, opt => opt.MapFrom(s => GetAddressPart(s.TrainingBizMailingAddress?.AddressLine2)))
            .ForMember(d => d.Spd_City, opt => opt.MapFrom(s => GetAddressPart(s.TrainingBizMailingAddress?.City)))
            .ForMember(d => d.Spd_PostalZipCode, opt => opt.MapFrom(s => GetAddressPart(s.TrainingBizMailingAddress?.PostalCode)))
            .ForMember(d => d.Spd_ProvinceState, opt => opt.MapFrom(s => GetAddressPart(s.TrainingBizMailingAddress?.Province)))
            .ForMember(d => d.Spd_Country, opt => opt.MapFrom(s => GetAddressPart(s.TrainingBizMailingAddress?.Country)));

        _ = CreateMap<OtherTraining, Spd_DogTrainingSchool>()
            .ForMember(d => d.Spd_TrainingDetails, opt => opt.MapFrom(s => s.TrainingDetail))
            .ForMember(d => d.Spd_TrainingSchoolType, opt => opt.MapFrom(_ => Spd_DogTrainingSchoolType.Other))
            .ForMember(d => d.Spd_USeaPersonalDogTrainer, opt => opt.MapFrom(s =>
                (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.UsePersonalDogTrainer)))
            .ForMember(d => d.Spd_PersonalDogTrainerCredentials, opt => opt.MapFrom(s => s.DogTrainerCredential))
            .ForMember(d => d.Spd_HowMuchTimeWasSpentTraining, opt => opt.MapFrom(s => s.TrainingTime))
            .ForMember(d => d.Spd_ContactSurname, opt => opt.MapFrom(s => s.TrainerSurname))
            .ForMember(d => d.Spd_ContactFirstname, opt => opt.MapFrom(s => s.TrainerGivenName))
            .ForMember(d => d.Spd_ContactEmail, opt => opt.MapFrom(s => s.TrainerEmailAddress))
            .ForMember(d => d.Spd_ContactPhone, opt => opt.MapFrom(s => s.TrainerPhoneNumber))
            .ForMember(d => d.Spd_HoursSpentPracticingSkills, opt => opt.MapFrom(s => s.HoursPracticingSkill));

        _ = CreateMap<Spd_Application, GDSDApp>()
            .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s.Spd_ServiceTypeId?.Id)))
            .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetLicenceApplicationTypeEnum((int?)s.Spd_LicenceApplicationType)))
            .ForMember(d => d.LicenceTermCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetLicenceTermEnum((int?)s.Spd_LicenceTerm)))
            .ForMember(d => d.ApplicationOriginTypeCode, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetEnum<ApplicationOriginOptionSet, ApplicationOriginTypeEnum>((int?)s.Spd_Origin)))
            .ForMember(d => d.AgreeToCompleteAndAccurate, opt => opt.MapFrom(s => s.Spd_Declaration))
            .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.Spd_LastName))
            .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.Spd_FirstName))
            .ForMember(d => d.MiddleName, opt => opt.MapFrom(s => s.Spd_MiddleName1))
            .ForMember(d => d.MailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)))
            .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => GetDateOnly(s.Spd_DateOfBirth)))
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.Spd_PhoneNumber))
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Spd_EmailAddress1))
            .ForMember(d => d.ApplicantOrLegalGuardianName, opt => opt.MapFrom(s => s.Spd_NameOfApplicantOrLegalGuardian))
            .ForMember(d => d.DogInfo, opt => opt.MapFrom(s => s))
            .ForMember(d => d.IsDogTrainedByAccreditedSchool, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetBool((int?)s.Spd_DogStrainingAccredited)));

        _ = CreateMap<Spd_Application, GDSDAppResp>()
            .IncludeBase<Spd_Application, GDSDApp>()
            .ForMember(d => d.ApplicationPortalStatus, opt => opt.MapFrom(s => GetPortalStatus(s.Spd_PortalStatus)))
            .ForMember(d => d.CaseNumber, opt => opt.MapFrom(s => s.Spd_Name))
            .ForMember(d => d.LicenceAppId, opt => opt.MapFrom(s => s.Spd_ApplicationId))
            .ForMember(d => d.AccreditedSchoolQuestions, opt => opt.MapFrom((s, _, _, context) => GetAccreditedSchoolQuestions(s, context)))
            .ForMember(d => d.NonAccreditedSchoolQuestions, opt => opt.MapFrom((s, _, _, context) => GetNonAccreditedSchoolQuestions(s, context)));

        _ = CreateMap<Spd_Application, DogInfo>()
            .ForMember(d => d.DogName, opt => opt.MapFrom(s => s.Spd_DogName))
            .ForMember(d => d.DogDateOfBirth, opt => opt.MapFrom(s => GetDateOnly(s.Spd_DogsDateOfBirth)))
            .ForMember(d => d.DogBreed, opt => opt.MapFrom(s => s.Spd_DogBreed))
            .ForMember(d => d.DogColorAndMarkings, opt => opt.MapFrom(s => s.Spd_DogColourAndMarkings))
            .ForMember(d => d.DogGender, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetEnum<GenderOptionSet, GenderEnum>((int?)s.Spd_DogsGender)))
            .ForMember(d => d.MicrochipNumber, opt => opt.MapFrom(s => s.Spd_MicrochipNumber));

        _ = CreateMap<Spd_Application, AccreditedSchoolQuestions>()
            .ForMember(d => d.IsGuideDog, opt => opt.MapFrom(s =>
                s.Spd_DogType == Spd_DogType.GuideDog ? true
                : s.Spd_DogType == Spd_DogType.ServiceDog ? false
                : (bool?)null))
            .ForMember(d => d.ServiceDogTasks, opt => opt.MapFrom(s =>
                s.Spd_DogType == Spd_DogType.GuideDog ? null : s.Spd_DogsAssistanceInDailyLiving))
            .ForMember(d => d.GraduationInfo, opt => opt.MapFrom((s, _, _, context) => GetGraduationInfo(s, context)));

        _ = CreateMap<Spd_Application, NonAccreditedSchoolQuestions>()
            .ForMember(d => d.AreInoculationsUpToDate, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetBool((int?)s.Spd_DogsInoculationsUpToDate)))
            .ForMember(d => d.DoctorIsProvidingNeedDogMedicalForm, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetBool((int?)s.Spd_DoctorIsProvidingGDSDMedicalForm)))
            .ForMember(d => d.TrainingInfo, opt => opt.MapFrom((s, _, _, context) => GetTrainingInfo(s, context)));

        _ = CreateMap<Spd_DogTrainingSchool, GraduationInfo>()
            .ForMember(d => d.AccreditedSchoolId, opt => opt.MapFrom(s => s.Spd_OrganizationId == null ? null : s.Spd_OrganizationId.Id))
            .ForMember(d => d.AccreditedSchoolName, opt => opt.MapFrom(s => s.Spd_TrainingSchoolName))
            .ForMember(d => d.SchoolContactSurname, opt => opt.MapFrom(s => s.Spd_ContactSurname))
            .ForMember(d => d.SchoolContactGivenName, opt => opt.MapFrom(s => s.Spd_ContactFirstname))
            .ForMember(d => d.SchoolContactEmailAddress, opt => opt.MapFrom(s => s.Spd_ContactEmail))
            .ForMember(d => d.SchoolContactPhoneNumber, opt => opt.MapFrom(s => s.Spd_ContactPhone));

        _ = CreateMap<Spd_DogTrainingSchool, TrainingSchoolInfo>()
            .ForMember(d => d.TrainingId, opt => opt.MapFrom(s => s.Spd_DogTrainingSchoolId))
            .ForMember(d => d.TrainingBizName, opt => opt.MapFrom(s => s.Spd_TrainingSchoolName))
            .ForMember(d => d.TrainingBizMailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)))
            .ForMember(d => d.ContactSurname, opt => opt.MapFrom(s => s.Spd_ContactSurname))
            .ForMember(d => d.ContactGivenName, opt => opt.MapFrom(s => s.Spd_ContactFirstname))
            .ForMember(d => d.ContactEmailAddress, opt => opt.MapFrom(s => s.Spd_ContactEmail))
            .ForMember(d => d.ContactPhoneNumber, opt => opt.MapFrom(s => s.Spd_ContactPhone))
            .ForMember(d => d.TotalTrainingHours, opt => opt.MapFrom(s => s.Spd_TrainingHours))
            .ForMember(d => d.TrainingStartDate, opt => opt.MapFrom(s => GetDateOnly(s.Spd_StartDate)))
            .ForMember(d => d.TrainingEndDate, opt => opt.MapFrom(s => GetDateOnly(s.Spd_EndDate)))
            .ForMember(d => d.TrainingName, opt => opt.MapFrom(s => s.Spd_NameAndOrTypeOfTrainingProgram))
            .ForMember(d => d.WhatLearned, opt => opt.MapFrom(s => s.Spd_TrainingDetails));

        _ = CreateMap<Spd_DogTrainingSchool, OtherTraining>()
            .ForMember(d => d.TrainingId, opt => opt.MapFrom(s => s.Spd_DogTrainingSchoolId))
            .ForMember(d => d.TrainingDetail, opt => opt.MapFrom(s => s.Spd_TrainingDetails))
            .ForMember(d => d.UsePersonalDogTrainer, opt => opt.MapFrom(s =>
                SharedMappingFuncs.GetBool((int?)s.Spd_USeaPersonalDogTrainer)))
            .ForMember(d => d.DogTrainerCredential, opt => opt.MapFrom(s => s.Spd_PersonalDogTrainerCredentials))
            .ForMember(d => d.TrainingTime, opt => opt.MapFrom(s => s.Spd_HowMuchTimeWasSpentTraining))
            .ForMember(d => d.TrainerSurname, opt => opt.MapFrom(s => s.Spd_ContactSurname))
            .ForMember(d => d.TrainerGivenName, opt => opt.MapFrom(s => s.Spd_ContactFirstname))
            .ForMember(d => d.TrainerEmailAddress, opt => opt.MapFrom(s => s.Spd_ContactEmail))
            .ForMember(d => d.TrainerPhoneNumber, opt => opt.MapFrom(s => s.Spd_ContactPhone))
            .ForMember(d => d.HoursPracticingSkill, opt => opt.MapFrom(s => s.Spd_HoursSpentPracticingSkills));

        _ = CreateMap<Contact, GDSDApp>()
            .ForMember(d => d.GivenName, opt => opt.MapFrom(s => s.FirstName))
            .ForMember(d => d.MiddleName, opt => opt.MapFrom(s => s.Spd_MiddleName1))
            .ForMember(d => d.Surname, opt => opt.MapFrom(s => s.LastName))
            .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => GetDateOnly(s.Birthdate)))
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.EmailAddress1))
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.Telephone1))
            .ForMember(d => d.MailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)));

        _ = CreateMap<GDSDApp, spd_application>()
        .ForMember(d => d.spd_applicationid, opt => opt.MapFrom(s => Guid.NewGuid()))
        .ForMember(d => d.spd_licenceapplicationtype, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceApplicationTypeOptionSet(s.ApplicationTypeCode)))
        .ForMember(d => d.spd_firstname, opt => opt.MapFrom(s => s.GivenName))
        .ForMember(d => d.spd_lastname, opt => opt.MapFrom(s => s.Surname))
        .ForMember(d => d.spd_middlename1, opt => opt.MapFrom(s => s.MiddleName))
        .ForMember(d => d.spd_dateofbirth, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateFromDateOnly(s.DateOfBirth)))
        .ForMember(d => d.spd_emailaddress1, opt => opt.MapFrom(s => s.EmailAddress))
        .ForMember(d => d.spd_phonenumber, opt => opt.MapFrom(s => s.PhoneNumber))
        .ForMember(d => d.spd_origin, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationOriginTypeEnum, ApplicationOriginOptionSet>(s.ApplicationOriginTypeCode)))
        .ForMember(d => d.statecode, opt => opt.MapFrom(s => DataverseConstants.StateCode_Active))
        .ForMember(d => d.spd_nameofapplicantorlegalguardian, opt => opt.MapFrom(s => s.ApplicantOrLegalGuardianName))
        .ForMember(d => d.spd_dogstrainingaccredited, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsDogTrainedByAccreditedSchool)))
        .ForMember(d => d.spd_addressline1, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.AddressLine1)))
        .ForMember(d => d.spd_addressline2, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.AddressLine2)))
        .ForMember(d => d.spd_city, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.City)))
        .ForMember(d => d.spd_postalcode, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.PostalCode)))
        .ForMember(d => d.spd_province, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.Province)))
        .ForMember(d => d.spd_country, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.Country)))
        .ForMember(d => d.spd_submittedon, opt => opt.Ignore())
        .ForMember(d => d.spd_portalmodifiedon, opt => opt.MapFrom(s => DateTimeOffset.UtcNow))
        .ForMember(d => d.spd_licenceterm, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<LicenceTermEnum, LicenceTermOptionSet>(s.LicenceTermCode)))
        .ForMember(d => d.spd_declaration, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
        .ForMember(d => d.spd_consent, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
        .ForMember(d => d.spd_declarationdate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDeclarationDate(s.AgreeToCompleteAndAccurate)))
        .ForMember(d => d.spd_identityconfirmed, opt => opt.MapFrom(s => SharedMappingFuncs.GetIdentityConfirmed(s.ApplicationOriginTypeCode, s.ApplicationTypeCode.Value)))
        .AfterMap((s, d, context) =>
        {
            context.Mapper.Map<DogInfo, spd_application>(s.DogInfo, d);
        })
        .ReverseMap()
        .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s._spd_servicetypeid_value)))
        .ForMember(d => d.ApplicationOriginTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<ApplicationOriginOptionSet, ApplicationOriginTypeEnum>(s.spd_origin)))
        .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceApplicationTypeEnum(s.spd_licenceapplicationtype)))
        .ForMember(d => d.LicenceTermCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceTermEnum(s.spd_licenceterm)))
        .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateOnly(s.spd_dateofbirth)))
        .ForMember(d => d.MailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)))
        .ForMember(d => d.DogInfo, opt => opt.MapFrom((src, dest, destMember, context) => GetDogInfo(src, context)))
        .ForMember(d => d.IsDogTrainedByAccreditedSchool, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool(s.spd_dogstrainingaccredited)));

        _ = CreateMap<GDSDApp, contact>()
        .ForMember(d => d.contactid, opt => opt.Condition((src, dest, srcMember) => dest.contactid == null))
        .ForMember(d => d.contactid, opt => opt.MapFrom(s => Guid.NewGuid()))
        .ForMember(d => d.firstname, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.GivenName)))
        .ForMember(d => d.spd_middlename1, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MiddleName)))
        .ForMember(d => d.lastname, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.Surname)))
        .ForMember(d => d.middlename, opt => opt.Ignore())
        .ForMember(d => d.emailaddress1, opt => opt.MapFrom(s => s.EmailAddress))
        .ForMember(d => d.birthdate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateFromDateOnly(s.DateOfBirth)))
        .ForMember(d => d.telephone1, opt => opt.MapFrom(s => s.PhoneNumber))
        .ForMember(d => d.address1_line1, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.AddressLine1)))
        .ForMember(d => d.address1_line2, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.AddressLine2)))
        .ForMember(d => d.address1_city, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.City)))
        .ForMember(d => d.address1_postalcode, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.PostalCode)))
        .ForMember(d => d.address1_stateorprovince, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.Province)))
        .ForMember(d => d.address1_country, opt => opt.MapFrom(s => s.MailingAddress == null ? null : StringHelper.SanitizeEmpty(s.MailingAddress.Country)))
        .ReverseMap()
        .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => SharedMappingFuncs.GetDateOnly(s.birthdate)))
        .ForMember(d => d.MailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)));

        _ = CreateMap<AccreditedSchoolQuestions, spd_application>()
        .ForMember(d => d.spd_dogtype, opt => opt.MapFrom(s => GetDogTypeOptionSet(s.IsGuideDog)))
        .ForMember(d => d.spd_dogsassistanceindailyliving, opt => opt.MapFrom(s => (s.IsGuideDog != null && s.IsGuideDog.Value) ? null : s.ServiceDogTasks))
        .ForMember(d => d.spd_dogspayedorneutered, opt => opt.MapFrom(s => (int)YesNoOptionSet.Yes))
        .ReverseMap()
        .ForMember(d => d.IsGuideDog, opt => opt.MapFrom(s => GetBoolFromDogType(s.spd_dogtype)))
        .ForMember(d => d.ServiceDogTasks, opt => opt.MapFrom(s => GetBoolFromDogType(s.spd_dogtype) == true ? null : s.spd_dogsassistanceindailyliving))
        .ForMember(d => d.GraduationInfo, opt => opt.MapFrom((src, dest, destMember, context) => GetGraduationInfo(src, context)));

        _ = CreateMap<NonAccreditedSchoolQuestions, spd_application>()
        .ForMember(d => d.spd_dogtype, opt => opt.MapFrom(s => (int)DogTypeOptionSet.ServiceDog))
        .ForMember(d => d.spd_dogsinoculationsuptodate, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.AreInoculationsUpToDate))) //refine
        .ForMember(d => d.spd_dogspayedorneutered, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsDogSterilized)))
        .ForMember(d => d.spd_doctorisprovidinggdsdmedicalform, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.DoctorIsProvidingNeedDogMedicalForm)))
        .ReverseMap()
        .ForMember(d => d.AreInoculationsUpToDate, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool(s.spd_dogsinoculationsuptodate)))
        .ForMember(d => d.TrainingInfo, opt => opt.MapFrom((src, dest, destMember, context) => GetTrainingInfol(src, context)))
        .ForMember(d => d.DoctorIsProvidingNeedDogMedicalForm, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool(s.spd_doctorisprovidinggdsdmedicalform)))
        ;

        _ = CreateMap<DogInfo, spd_application>()
        .ForMember(d => d.spd_dogname, opt => opt.MapFrom(s => s.DogName))
        .ForMember(d => d.spd_dogsdateofbirth, opt => opt.MapFrom(s => s.DogDateOfBirth))
        .ForMember(d => d.spd_dogbreed, opt => opt.MapFrom(s => s.DogBreed))
        .ForMember(d => d.spd_dogcolourandmarkings, opt => opt.MapFrom(s => s.DogColorAndMarkings))
        .ForMember(d => d.spd_dogsgender, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<GenderEnum, GenderOptionSet>(s.DogGender)))
        .ForMember(d => d.spd_microchipnumber, opt => opt.MapFrom(s => s.MicrochipNumber))
        .ReverseMap()
        .ForMember(d => d.DogGender, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<GenderOptionSet, GenderEnum>(s.spd_dogsgender)))
        ;

        _ = CreateMap<GraduationInfo, spd_dogtrainingschool>()
         .ForMember(d => d.spd_trainingschoolname, opt => opt.MapFrom(s => s.AccreditedSchoolName))
         .ForMember(d => d.spd_contactsurname, opt => opt.MapFrom(s => s.SchoolContactSurname))
         .ForMember(d => d.spd_contactfirstname, opt => opt.MapFrom(s => s.SchoolContactGivenName))
         .ForMember(d => d.spd_contactphone, opt => opt.MapFrom(s => s.SchoolContactPhoneNumber))
         .ForMember(d => d.spd_contactemail, opt => opt.MapFrom(s => s.SchoolContactEmailAddress))
         .ReverseMap()
         .ForMember(d => d.AccreditedSchoolId, opt => opt.MapFrom(s => s._spd_organizationid_value));

        _ = CreateMap<TrainingSchoolInfo, spd_dogtrainingschool>()
           .ForMember(d => d.spd_trainingschoolname, opt => opt.MapFrom(s => s.TrainingBizName))
           .ForMember(d => d.spd_contactfirstname, opt => opt.MapFrom(s => s.ContactGivenName))
           .ForMember(d => d.spd_contactsurname, opt => opt.MapFrom(s => s.ContactSurname))
           .ForMember(d => d.spd_contactemail, opt => opt.MapFrom(s => s.ContactEmailAddress))
           .ForMember(d => d.spd_contactphone, opt => opt.MapFrom(s => s.ContactPhoneNumber))
           .ForMember(d => d.spd_traininghours, opt => opt.MapFrom(s => s.TotalTrainingHours))
           .ForMember(d => d.spd_startdate, opt => opt.MapFrom(s => s.TrainingStartDate))
           .ForMember(d => d.spd_enddate, opt => opt.MapFrom(s => s.TrainingEndDate))
           .ForMember(d => d.spd_name, opt => opt.MapFrom(s => s.TrainingName))
           .ForMember(d => d.spd_trainingschooltype, opt => opt.MapFrom(s => (int)DogTrainingSchoolTypeOptionSet.UnAccreditedSchool))
           .ForMember(d => d.spd_trainingdetails, opt => opt.MapFrom(s => s.WhatLearned))
           .ForMember(d => d.spd_street1, opt => opt.MapFrom(s => s.TrainingBizMailingAddress == null ? null : StringHelper.SanitizeEmpty(s.TrainingBizMailingAddress.AddressLine1)))
            .ForMember(d => d.spd_street2, opt => opt.MapFrom(s => s.TrainingBizMailingAddress == null ? null : StringHelper.SanitizeEmpty(s.TrainingBizMailingAddress.AddressLine2)))
            .ForMember(d => d.spd_city, opt => opt.MapFrom(s => s.TrainingBizMailingAddress == null ? null : StringHelper.SanitizeEmpty(s.TrainingBizMailingAddress.City)))
            .ForMember(d => d.spd_postalzipcode, opt => opt.MapFrom(s => s.TrainingBizMailingAddress == null ? null : StringHelper.SanitizeEmpty(s.TrainingBizMailingAddress.PostalCode)))
            .ForMember(d => d.spd_provincestate, opt => opt.MapFrom(s => s.TrainingBizMailingAddress == null ? null : StringHelper.SanitizeEmpty(s.TrainingBizMailingAddress.Province)))
            .ForMember(d => d.spd_country, opt => opt.MapFrom(s => s.TrainingBizMailingAddress == null ? null : StringHelper.SanitizeEmpty(s.TrainingBizMailingAddress.Country)))
            .ReverseMap()
            .ForMember(d => d.TrainingId, opt => opt.MapFrom(s => s.spd_dogtrainingschoolid))
            .ForMember(d => d.TrainingBizMailingAddress, opt => opt.MapFrom(s => SharedMappingFuncs.GetMailingAddressData(s)))
            ;

        _ = CreateMap<OtherTraining, spd_dogtrainingschool>()
           .ForMember(d => d.spd_trainingdetails, opt => opt.MapFrom(s => s.TrainingDetail))
           .ForMember(d => d.spd_trainingschooltype, opt => opt.MapFrom(s => (int)DogTrainingSchoolTypeOptionSet.Other))
            .ForMember(d => d.spd_useapersonaldogtrainer, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.UsePersonalDogTrainer)))
            .ForMember(d => d.spd_personaldogtrainercredentials, opt => opt.MapFrom(s => s.DogTrainerCredential))
            .ForMember(d => d.spd_howmuchtimewasspenttraining, opt => opt.MapFrom(s => s.TrainingTime))
            .ForMember(d => d.spd_contactsurname, opt => opt.MapFrom(s => s.TrainerSurname))
            .ForMember(d => d.spd_contactfirstname, opt => opt.MapFrom(s => s.TrainerGivenName))
            .ForMember(d => d.spd_contactemail, opt => opt.MapFrom(s => s.TrainerEmailAddress))
            .ForMember(d => d.spd_contactphone, opt => opt.MapFrom(s => s.TrainerPhoneNumber))
            .ForMember(d => d.spd_hoursspentpracticingskills, opt => opt.MapFrom(s => s.HoursPracticingSkill))
            .ReverseMap()
            .ForMember(d => d.TrainingId, opt => opt.MapFrom(s => s.spd_dogtrainingschoolid))
            .ForMember(d => d.UsePersonalDogTrainer, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool(s.spd_useapersonaldogtrainer)))
            ;

        CreateMap<SaveGDSDAppCmd, spd_application>()
         .ForMember(d => d.statuscode, opt => opt.MapFrom(s => SharedMappingFuncs.GetApplicationStatus(s.ApplicationStatusEnum)))
         .ForMember(d => d.spd_applicationid, opt => opt.MapFrom(s => s.LicenceAppId ?? Guid.NewGuid()))
         .IncludeBase<GDSDApp, spd_application>();

        CreateMap<CreateGDSDAppCmd, spd_application>()
         .ForMember(d => d.spd_applicationid, opt => opt.MapFrom(s => Guid.NewGuid()))
         .ForMember(d => d.spd_submittedon, opt => opt.MapFrom(s => DateTimeOffset.UtcNow))
         .ForMember(d => d.spd_dogsrequiredfordisabilityorimpairment, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.IsAssistanceStillRequired)))
         .IncludeBase<GDSDApp, spd_application>();

        _ = CreateMap<spd_application, GDSDAppResp>()
         .IncludeBase<spd_application, GDSDApp>()
          .ForMember(d => d.ApplicationPortalStatus, opt => opt.MapFrom(s => s.spd_portalstatus == null ? null : ((ApplicationPortalStatus)s.spd_portalstatus.Value).ToString()))
          .ForMember(d => d.CaseNumber, opt => opt.MapFrom(s => s.spd_name))
          .ForMember(d => d.LicenceAppId, opt => opt.MapFrom(s => s.spd_applicationid))
          .ForMember(d => d.AccreditedSchoolQuestions, opt => opt.MapFrom((src, dest, destMember, context) => GetAccreditedSchoolQuestions(src, context)))
          .ForMember(d => d.NonAccreditedSchoolQuestions, opt => opt.MapFrom((src, dest, destMember, context) => GetNonAccreditedSchoolQuestions(src, context)))
          ;
    }



    private static DateOnly? GetDateOnly(DateTime? date)
    {
        return date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
    }

    private static ApplicationPortalStatusEnum? GetPortalStatus(Spd_PortalStatus? portalStatus)
    {
        return portalStatus.HasValue
            ? Enum.Parse<ApplicationPortalStatusEnum>(portalStatus.Value.ToString())
            : null;
    }

    private static GraduationInfo? GetGraduationInfo(Spd_Application app, ResolutionContext context)
    {
        if (app.Spd_DogStrainingAccredited != Spd_YesNo.Yes)
            return null;

        Spd_DogTrainingSchool? graduation = app.Spd_Application_Spd_DogTrainingSchool_ApplicationId
            .FirstOrDefault(s => s.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.AccreditedSchool);
        return graduation == null ? null : context.Mapper.Map<GraduationInfo>(graduation);
    }

    private static AccreditedSchoolQuestions? GetAccreditedSchoolQuestions(Spd_Application app, ResolutionContext context)
    {
        return app.Spd_DogStrainingAccredited == Spd_YesNo.Yes
            ? context.Mapper.Map<AccreditedSchoolQuestions>(app)
            : null;
    }

    private static NonAccreditedSchoolQuestions? GetNonAccreditedSchoolQuestions(Spd_Application app, ResolutionContext context)
    {
        return app.Spd_DogStrainingAccredited == Spd_YesNo.No
            ? context.Mapper.Map<NonAccreditedSchoolQuestions>(app)
            : null;
    }

    private static TrainingInfo? GetTrainingInfo(Spd_Application app, ResolutionContext context)
    {
        if (app.Spd_DogStrainingAccredited != Spd_YesNo.No)
            return null;

        var trainingInfo = new TrainingInfo
        {
            SpecializedTasksWhenPerformed = app.Spd_DogsAssistanceInDailyLiving
        };
        var trainingSchools = app.Spd_Application_Spd_DogTrainingSchool_ApplicationId;
        if (trainingSchools.Any(s => s.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.UnaccreditedSchool))
        {
            trainingInfo.HasAttendedTrainingSchool = true;
            trainingInfo.SchoolTrainings = context.Mapper.Map<List<TrainingSchoolInfo>>(trainingSchools
                .Where(s => s.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.UnaccreditedSchool)
                .ToList());
        }
        else
        {
            trainingInfo.HasAttendedTrainingSchool = false;
            trainingInfo.OtherTrainings = context.Mapper.Map<List<OtherTraining>>(trainingSchools
                .Where(s => s.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.Other)
                .ToList());
        }

        return trainingInfo;
    }

    private static int? GetDogTypeOptionSet(bool? isGuidDog)
    {
        if (isGuidDog == null) return null;
        else
            return isGuidDog.Value ? (int)DogTypeOptionSet.GuideDog : (int)DogTypeOptionSet.ServiceDog;
    }

    private static DateTime? GetDataverseDate(DateOnly? date)
    {
        return date.HasValue
            ? DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
    }

    private static string? GetAddressPart(string? value)
    {
        return value == null ? null : StringHelper.SanitizeEmpty(value);
    }

    private static bool? GetBoolFromDogType(int? dogType)
    {
        if (dogType == null) return null;
        else
            return dogType == (int)DogTypeOptionSet.GuideDog ? true : false;
    }

    private static DogInfo? GetDogInfo(spd_application app, ResolutionContext context)
    {
        return context.Mapper.Map<DogInfo>(app);
    }

    private static GraduationInfo? GetGraduationInfo(spd_application app, ResolutionContext context)
    {
        if (app.spd_dogstrainingaccredited != null && app.spd_dogstrainingaccredited.Value == (int)YesNoOptionSet.Yes)
        {
            if (app.spd_application_spd_dogtrainingschool_ApplicationId.Any())
            {
                return context.Mapper.Map<GraduationInfo>(app.spd_application_spd_dogtrainingschool_ApplicationId.FirstOrDefault(s => s.spd_trainingschooltype == (int)DogTrainingSchoolTypeOptionSet.AccreditedSchool));
            }
        }
        return null;
    }

    private static AccreditedSchoolQuestions? GetAccreditedSchoolQuestions(spd_application app, ResolutionContext context)
    {
        if (app.spd_dogstrainingaccredited != null && app.spd_dogstrainingaccredited.Value == (int)YesNoOptionSet.Yes)
        {
            return context.Mapper.Map<AccreditedSchoolQuestions>(app);
        }
        return null;
    }

    private static NonAccreditedSchoolQuestions? GetNonAccreditedSchoolQuestions(spd_application app, ResolutionContext context)
    {
        if (app.spd_dogstrainingaccredited != null && app.spd_dogstrainingaccredited.Value == (int)YesNoOptionSet.No)
        {
            return context.Mapper.Map<NonAccreditedSchoolQuestions>(app);
        }
        return null;
    }

    private static TrainingInfo? GetTrainingInfol(spd_application app, ResolutionContext context)
    {
        if (app.spd_dogstrainingaccredited != null && app.spd_dogstrainingaccredited.Value == (int)YesNoOptionSet.No)
        {
            TrainingInfo ti = new TrainingInfo();
            ti.SpecializedTasksWhenPerformed = app.spd_dogsassistanceindailyliving;
            if (app.spd_application_spd_dogtrainingschool_ApplicationId.Any(s => s.spd_trainingschooltype == (int)DogTrainingSchoolTypeOptionSet.UnAccreditedSchool))
            {
                ti.HasAttendedTrainingSchool = true;
                ti.SchoolTrainings = context.Mapper.Map<List<TrainingSchoolInfo>>(app.spd_application_spd_dogtrainingschool_ApplicationId.Where(d => d.spd_trainingschooltype == (int)DogTrainingSchoolTypeOptionSet.UnAccreditedSchool).ToList());
            }
            else
            {
                ti.HasAttendedTrainingSchool = false;
                ti.OtherTrainings = context.Mapper.Map<List<OtherTraining>>(app.spd_application_spd_dogtrainingschool_ApplicationId.Where(d => d.spd_trainingschooltype == (int)DogTrainingSchoolTypeOptionSet.Other).ToList());

            }
            return ti;
        }
        return null;
    }
}

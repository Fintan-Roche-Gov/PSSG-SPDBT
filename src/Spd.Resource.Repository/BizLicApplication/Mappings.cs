using AutoMapper;
using Spd.Resource.Repository.PersonLicApplication;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.BizLicApplication;
internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<BizLicApplication, Spd_Application>()
         .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => Guid.NewGuid()))
         .ForMember(d => d.Spd_LicenceApplicationType, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationTypeEnum, Spd_LicenceApplicationType>(s.ApplicationTypeCode)))
         .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
         .ForMember(d => d.Spd_LastName, opt => opt.MapFrom(s => s.Surname))
         .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName1))
         .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => s.MiddleName2))
         .ForMember(d => d.Spd_EmailAddress1, opt => opt.MapFrom(s => s.EmailAddress))
         .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber))
         .ForMember(d => d.Spd_BusinessManagerFirstName, opt => opt.MapFrom(s => s.ManagerGivenName))
         .ForMember(d => d.Spd_BusinessManagerSurname, opt => opt.MapFrom(s => s.ManagerSurname))
         .ForMember(d => d.Spd_BusinessManagerMiddleName1, opt => opt.MapFrom(s => s.ManagerMiddleName1))
         .ForMember(d => d.Spd_BusinessManagerMiddleName2, opt => opt.MapFrom(s => s.ManagerMiddleName2))
         .ForMember(d => d.Spd_BusinessmanAgeRemail, opt => opt.MapFrom(s => s.ManagerEmailAddress))
         .ForMember(d => d.Spd_BusinessManagerPhone, opt => opt.MapFrom(s => s.ManagerPhoneNumber))
         .ForMember(d => d.Spd_Origin, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationOriginTypeEnum, Incident_CaseOriginCode>(s.ApplicationOriginTypeCode)))
         .ForMember(d => d.Spd_Payer, opt => opt.MapFrom(s => Spd_PayerType.Applicant))
         .ForMember(d => d.Spd_BusinessType, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<BizTypeEnum, Spd_BusinessType>(s.BizTypeCode)))
         .ForMember(d => d.Spd_RequestDogs, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.UseDogs) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.UseDogs)))
         .ForMember(d => d.Spd_RequestDogsReasons, opt => opt.MapFrom(s => GetDogReasonEnums(s.IsDogsPurposeDetectionDrugs, s.IsDogsPurposeProtection, s.IsDogsPurposeDetectionExplosives)))
         .ForMember(d => d.StateCode, opt => opt.MapFrom(s => Spd_Application_StateCode.Active))
         .ForMember(d => d.Spd_SubmittedOn, opt => opt.Ignore())
         .ForMember(d => d.Spd_UploadedDocuments, opt => opt.MapFrom(s => GetUploadedDocumentsEnums(s.UploadedDocumentEnums)))
         .ForMember(d => d.Spd_PortalModifiedOn, opt => opt.MapFrom(s => DateTime.UtcNow))
         .ForMember(d => d.Spd_NoLogoOrBranding, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.NoBranding) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.NoBranding)))
         .ForMember(d => d.Spd_LicenceTerm, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<LicenceTermEnum, Spd_LicenceTerm>(s.LicenceTermCode)))
         .ForMember(d => d.Spd_Declaration, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
         .ForMember(d => d.Spd_Consent, opt => opt.MapFrom(s => s.AgreeToCompleteAndAccurate))
         .ForMember(d => d.Spd_DeclarationDate, opt => opt.MapFrom(s => SharedMappingFuncs.GetDeclarationDate(s.AgreeToCompleteAndAccurate)))
         .ForMember(d => d.Spd_IdentityConfirmed, opt => opt.MapFrom(s => SharedMappingFuncs.GetIdentityConfirmed(s.ApplicationOriginTypeCode, s.ApplicationTypeCode)))
         .ReverseMap()
         .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s.Spd_ServiceTypeId != null ? s.Spd_ServiceTypeId.Id : (Guid?)null)))
         .ForMember(d => d.ApplicationOriginTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Incident_CaseOriginCode, ApplicationOriginTypeEnum>((int?)s.Spd_Origin)))
         .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceApplicationTypeEnum((int?)s.Spd_LicenceApplicationType)))
         .ForMember(d => d.BizTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetBizTypeEnum((int?)s.Spd_BusinessType)))
         .ForMember(d => d.LicenceTermCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceTermEnum((int?)s.Spd_LicenceTerm)))
         .ForMember(d => d.NoBranding, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_NoLogoOrBranding)))
         .ForMember(d => d.UseDogs, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_RequestDogs)))
         .ForMember(d => d.IsDogsPurposeProtection, opt => opt.MapFrom(s => s.Spd_RequestDogsReasons != null && s.Spd_RequestDogsReasons.Contains(Spd_RequestDogsPurpose.Protection)))
         .ForMember(d => d.IsDogsPurposeDetectionDrugs, opt => opt.MapFrom(s => s.Spd_RequestDogsReasons != null && s.Spd_RequestDogsReasons.Contains(Spd_RequestDogsPurpose.DetectionDrugs)))
         .ForMember(d => d.IsDogsPurposeDetectionExplosives, opt => opt.MapFrom(s => s.Spd_RequestDogsReasons != null && s.Spd_RequestDogsReasons.Contains(Spd_RequestDogsPurpose.DetectionExplosives)))
         .ForMember(d => d.CategoryCodes, opt => opt.MapFrom(s => GetWorkerCategoryTypeEnums(s.Spd_Application_Spd_LicenceCategory)))
         .ForMember(d => d.UploadedDocumentEnums, opt => opt.MapFrom(s => GetUploadedDocumentEnums(s.Spd_UploadedDocuments)))
         .ForMember(d => d.ExpiredLicenceNumber, opt => opt.MapFrom(s => s.Spd_Licence_Spd_Application_CurrentExpired == null ? null : s.Spd_Licence_Spd_Application_CurrentExpired.Spd_LicenceNumber))
         .ForMember(d => d.ApplicantIsBizManager, opt => opt.MapFrom(s => IsApplicantBizManager(s)));

        CreateMap<SaveBizLicApplicationCmd, Spd_Application>()
         .ForMember(d => d.StatusCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationStatusEnum, Spd_Application_StatusCode>(s.ApplicationStatusEnum)))
         .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => s.LicenceAppId ?? Guid.NewGuid()))
         .IncludeBase<BizLicApplication, Spd_Application>();

        CreateMap<CreateBizLicApplicationCmd, Spd_Application>()
         .ForMember(d => d.Spd_ApplicationId, opt => opt.MapFrom(s => Guid.NewGuid()))
         .ForMember(d => d.Spd_SubmittedOn, opt => opt.MapFrom(s => DateTime.UtcNow))
         .ForMember(d => d.Spd_UpdateSummary, opt => opt.MapFrom(s => s.ChangeSummary))
         .IncludeBase<BizLicApplication, Spd_Application>();

        _ = CreateMap<Spd_Application, BizLicApplicationResp>()
         .ForMember(d => d.ContactId, opt => opt.MapFrom(s => s.Spd_Contact_Spd_Application_ApplicantId != null ? s.Spd_Contact_Spd_Application_ApplicantId.ContactId : (Guid?)null))
         .ForMember(d => d.ExpiryDate, opt => opt.MapFrom(s => s.Spd_Licence_Spd_Application_CurrentExpired == null ? null : SharedMappingFuncs.GetDateOnlyFromDateTimeOffset(s.Spd_Licence_Spd_Application_CurrentExpired.Spd_ExpiryDate)))
         .ForMember(d => d.ApplicationPortalStatus, opt => opt.Ignore())
         .ForMember(d => d.CaseNumber, opt => opt.MapFrom(s => s.Spd_Name))
         .ForMember(d => d.LicenceAppId, opt => opt.MapFrom(s => s.Spd_ApplicationId))
         .ForMember(d => d.OriginalLicenceTermCode, opt => opt.MapFrom(s => s.Spd_Licence_Spd_Application_CurrentExpired == null ? null : SharedMappingFuncs.GetLicenceTermEnum((int?)s.Spd_Licence_Spd_Application_CurrentExpired.Spd_LicenceTerm)))
         .ForMember(d => d.BizId, opt => opt.MapFrom(s => s.Spd_Account_Spd_Application_ApplicantId == null ? null : s.Spd_Account_Spd_Application_ApplicantId.AccountId))
         .ForMember(d => d.ExpiredLicenceId, opt => opt.MapFrom(s => s.Spd_CurrentExpiredLicenceId != null ? s.Spd_CurrentExpiredLicenceId.Id : (Guid?)null))
         .ForMember(d => d.HasExpiredLicence, opt => opt.MapFrom(s => s.Spd_CurrentExpiredLicenceId != null))
         .ForMember(d => d.PrivateInvestigatorSwlInfo, opt => opt.Ignore())
         .ForMember(d => d.SoleProprietorSWLAppId, opt => opt.MapFrom(s => GetSwlAppId(s.ReferencedSpd_BusinessApplication_Spd_WorkerApplication.ToList())))
         .ForMember(d => d.SoleProprietorSWLAppOriginTypeCode, opt => opt.MapFrom(s => GetSwlAppOrigin(s.ReferencedSpd_BusinessApplication_Spd_WorkerApplication.ToList())))
         .ForMember(d => d.NonSwlControllingMemberCrcAppIds, opt => opt.MapFrom(s => GetNonSwlCmAppIds(s.ReferencedSpd_BusinessApplication_Spd_WorkerApplication.ToList())))
         .IncludeBase<Spd_Application, BizLicApplication>();

        _ = CreateMap<PrivateInvestigatorSwlContactInfo, Spd_BusinessContact>()
         .ForMember(d => d.Spd_BusinessContactId, opt => opt.MapFrom(s => s.BizContactId))
         .ForMember(d => d.Spd_FirstName, opt => opt.MapFrom(s => s.GivenName))
         .ForMember(d => d.Spd_Surname, opt => opt.MapFrom(s => s.Surname))
         .ForMember(d => d.Spd_MiddleName1, opt => opt.MapFrom(s => s.MiddleName1))
         .ForMember(d => d.Spd_MiddleName2, opt => opt.MapFrom(s => s.MiddleName2))
         .ForMember(d => d.Spd_FullName, opt => opt.MapFrom(s => $"{s.Surname}, {s.GivenName}"))
         .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.EmailAddress));
    }

    private static bool IsApplicantBizManager(Spd_Application application)
    {
        if (application.Spd_BusinessManagerFirstName == application.Spd_FirstName &&
            application.Spd_BusinessManagerSurname == application.Spd_LastName &&
            application.Spd_BusinessManagerMiddleName1 == application.Spd_MiddleName1 &&
            application.Spd_BusinessManagerMiddleName2 == application.Spd_MiddleName2 &&
            application.Spd_BusinessmanAgeRemail == application.Spd_EmailAddress1)
            return true;

        return false;
    }

    private static Guid? GetSwlAppId(List<Spd_Application> apps)
    {
        Guid? swlServiceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SecurityWorkerLicence.ToString());
        return apps.Where(a => a.Spd_ServiceTypeId != null && a.Spd_ServiceTypeId.Id == swlServiceTypeId)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefault()?.Spd_ApplicationId;
    }

    private static ApplicationOriginTypeEnum? GetSwlAppOrigin(List<Spd_Application> apps)
    {
        Guid? swlServiceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SecurityWorkerLicence.ToString());
        int? origin = apps.Where(a => a.Spd_ServiceTypeId != null && a.Spd_ServiceTypeId.Id == swlServiceTypeId)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefault() != null
            ? (int?)apps.Where(a => a.Spd_ServiceTypeId != null && a.Spd_ServiceTypeId.Id == swlServiceTypeId)
                .OrderByDescending(a => a.CreatedOn)
                .First().Spd_Origin
            : null;

        return SharedMappingFuncs.GetEnum<Incident_CaseOriginCode, ApplicationOriginTypeEnum>(origin);
    }

    private static IEnumerable<Guid> GetNonSwlCmAppIds(List<Spd_Application> apps)
    {
        Guid? serviceTypeId = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SECURITY_BUSINESS_LICENCE_CONTROLLING_MEMBER_CRC.ToString());
        return apps.Where(a => a.Spd_ServiceTypeId != null && a.Spd_ServiceTypeId.Id == serviceTypeId)
            .Where(a => a.Spd_ApplicationId != null)
            .Select(a => a.Spd_ApplicationId.Value)
            .ToList();
    }

    private static IEnumerable<Spd.Resource.Repository.PersonLicApplication.WorkerCategoryTypeEnum> GetWorkerCategoryTypeEnums(IEnumerable<Spd_LicenceCategory> categories)
    {
        List<Spd.Resource.Repository.PersonLicApplication.WorkerCategoryTypeEnum> codes = new();
        foreach (Spd_LicenceCategory category in categories)
        {
            string key = Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LookupLicenceCategoryKey(category.Spd_LicenceCategoryId);
            if (!string.IsNullOrWhiteSpace(key) && Enum.TryParse(key, out Spd.Resource.Repository.PersonLicApplication.WorkerCategoryTypeEnum parsed))
            {
                codes.Add(parsed);
            }
        }

        return codes;
    }

    private static IEnumerable<Spd.Resource.Repository.PersonLicApplication.UploadedDocumentEnum>? GetUploadedDocumentEnums(IEnumerable<Spd_UploadedDocuments>? docs)
    {
        if (docs == null) return null;

        List<Spd.Resource.Repository.PersonLicApplication.UploadedDocumentEnum> values = new();
        foreach (Spd_UploadedDocuments doc in docs)
        {
            if (Enum.TryParse(doc.ToString(), out Spd.Resource.Repository.PersonLicApplication.UploadedDocumentEnum parsed))
                values.Add(parsed);
        }

        return values;
    }

    private static IEnumerable<Spd_UploadedDocuments>? GetUploadedDocumentsEnums(IEnumerable<Spd.Resource.Repository.PersonLicApplication.UploadedDocumentEnum>? docs)
    {
        if (docs == null) return null;

        List<Spd_UploadedDocuments> values = new();
        foreach (Spd.Resource.Repository.PersonLicApplication.UploadedDocumentEnum doc in docs)
        {
            if (Enum.TryParse(doc.ToString(), out Spd_UploadedDocuments parsed))
                values.Add(parsed);
        }

        return values;
    }

    private static IEnumerable<Spd_RequestDogsPurpose>? GetDogReasonEnums(bool? isDetectionDrugs, bool? isProtection, bool? isDetectionExplosives)
    {
        List<Spd_RequestDogsPurpose> reasons = new();

        if (isDetectionDrugs == true)
            reasons.Add(Spd_RequestDogsPurpose.DetectionDrugs);
        if (isDetectionExplosives == true)
            reasons.Add(Spd_RequestDogsPurpose.DetectionExplosives);
        if (isProtection == true)
            reasons.Add(Spd_RequestDogsPurpose.Protection);

        return reasons.Any() ? reasons : null;
    }
}

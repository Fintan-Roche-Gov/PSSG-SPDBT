using AutoMapper;
using Spd.Resource.Repository.PersonLicApplication;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Licence
{
    internal class Mappings : Profile
    {
        private static readonly List<ServiceTypeEnum> OrgServiceTypes = new()
        {
            ServiceTypeEnum.MDRA,
            ServiceTypeEnum.SecurityBusinessLicence
        };

        public Mappings()
        {
            _ = CreateMap<Spd_Licence, LicenceResp>()
                .ForMember(d => d.LicenceId, opt => opt.MapFrom(s => s.Spd_LicenceId))
                .ForMember(d => d.CaseId, opt => opt.MapFrom(s => s.Spd_CaseId == null ? null : s.Spd_CaseId.Id))
                .ForMember(d => d.LicenceAppId, opt => opt.MapFrom(s => s.Spd_Incident_Spd_Licence?.Spd_ApplicationId?.Id))
                .ForMember(d => d.LicenceHolderId, opt => opt.MapFrom(s => GetLicenceHolderId(s)))
                .ForMember(d => d.LicenceNumber, opt => opt.MapFrom(s => s.Spd_LicenceNumber))
                .ForMember(d => d.ExpiryDate, opt => opt.MapFrom(s => GetDateOnly(s.Spd_ExpiryDate) ?? default))
                .ForMember(d => d.ServiceTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetServiceType(s.Spd_LicenceType?.Id)))
                .ForMember(d => d.LicenceTermCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetLicenceTermEnum((int?)s.Spd_LicenceTerm)))
                .ForMember(d => d.BizLegalName, opt => opt.MapFrom(s => GetBizLegalName(s)))
                .ForMember(d => d.LicenceHolderFirstName, opt => opt.MapFrom(s => IsOrgLicence(s)
                    ? s.Spd_Account_Spd_Licence_LicenceHolder?.Name
                    : s.Spd_Contact_Spd_Licence_LicenceHolder?.FirstName))
                .ForMember(d => d.LicenceStatusCode, opt => opt.MapFrom(s => GetLicenceStatusEnum(s.StatusCode)))
                .ForMember(d => d.LicenceHolderLastName, opt => opt.MapFrom(s => IsOrgLicence(s)
                    ? null
                    : s.Spd_Contact_Spd_Licence_LicenceHolder?.LastName))
                .ForMember(d => d.LicenceHolderMiddleName1, opt => opt.MapFrom(s => IsOrgLicence(s)
                    ? null
                    : s.Spd_Contact_Spd_Licence_LicenceHolder?.Spd_MiddleName1))
                .ForMember(d => d.NameOnCard, opt => opt.MapFrom(s => s.Spd_NameOnLicence))
                .ForMember(d => d.CreatedOn, opt => opt.MapFrom(s => ToDateTimeOffset(s.CreatedOn)))
                .ForMember(d => d.PermitOtherRequiredReason, opt => opt.MapFrom(s => s.Spd_PermitPurposeOther))
                .ForMember(d => d.EmployerName, opt => opt.MapFrom(s => s.Spd_EmployerName))
                .ForMember(d => d.SupervisorName, opt => opt.MapFrom(s => s.Spd_EmployerContactName))
                .ForMember(d => d.SupervisorEmailAddress, opt => opt.MapFrom(s => s.Spd_EmployerEmail))
                .ForMember(d => d.SupervisorPhoneNumber, opt => opt.MapFrom(s => s.Spd_EmployerPhoneNumber))
                .ForMember(d => d.EmployerPrimaryAddress, opt => opt.MapFrom(s => s))
                .ForMember(d => d.Rationale, opt => opt.MapFrom(s => s.Spd_Rationale))
                .ForMember(d => d.PhotoDocumentUrlId, opt => opt.MapFrom(s => s.Spd_PhotographId == null ? null : s.Spd_PhotographId.Id))
                .ForMember(d => d.PrintingPreviewJobId, opt => opt.MapFrom(s => s.Spd_BCMPJobId))
                .ForMember(d => d.IsTemporary, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_TemporaryLicence)))
                .ForMember(d => d.PermitPurposeEnums, opt => opt.MapFrom(s => GetPermitPurposeEnums(s.Spd_PermitPurpose)))
                .ForMember(d => d.CategoryCodes, opt => opt.MapFrom(s => GetCategoryCodes(s)))
                .ForMember(d => d.BizTypeCode, opt => opt.MapFrom(s => GetBizType(s)))
                .ForMember(d => d.IssuedDate, opt => opt.MapFrom(s => GetDateOnly(s.Spd_IssuedAte) ?? default))
                .ForMember(d => d.SoleProprietorOrgId, opt => opt.MapFrom(s => s.Spd_SoleProprietorId == null ? null : s.Spd_SoleProprietorId.Id))
                .ForMember(d => d.IsDogsPurposeProtection, opt => opt.MapFrom(s => GetDogReasonFlag(s, Spd_RequestDogsPurpose.Protection)))
                .ForMember(d => d.IsDogsPurposeDetectionDrugs, opt => opt.MapFrom(s => GetDogReasonFlag(s, Spd_RequestDogsPurpose.DetectionDrugs)))
                .ForMember(d => d.IsDogsPurposeDetectionExplosives, opt => opt.MapFrom(s => GetDogReasonFlag(s, Spd_RequestDogsPurpose.DetectionExplosives)))
                .ForMember(d => d.CarryAndUseRestraints, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_RequestRestraints)))
                .ForMember(d => d.UseDogs, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_RequestDogs)))
                .ForMember(d => d.Conditions, opt => opt.MapFrom(s => s.Spd_Spd_Licence_Spd_LicenceCondition
                    .Where(condition => condition.StateCode == Spd_LicenceCondition_StateCode.Active)))
                .ForMember(d => d.GDSDTeamId, opt => opt.MapFrom(s => GetDogTeamId(s)))
                .ForMember(d => d.IsDogAssessor, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_OutD)))
                .ForMember(d => d.BadgeName, opt => opt.MapFrom(s => s.Spd_BadgeIdName));

            _ = CreateMap<Spd_Licence, LicenceBasicResp>()
                .ForMember(d => d.LicenceId, opt => opt.MapFrom(s => s.Spd_LicenceId))
                .ForMember(d => d.PrintingPreviewJobId, opt => opt.MapFrom(s => s.Spd_BCMPJobId));

            _ = CreateMap<Spd_LicenceCondition, Condition>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_LicenceConditionId))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Spd_ConditionName));

            _ = CreateMap<Spd_Licence, Addr>()
                .ForMember(d => d.AddressLine1, opt => opt.MapFrom(s => s.Spd_EmployerAddress1))
                .ForMember(d => d.AddressLine2, opt => opt.MapFrom(s => s.Spd_EmployerAddress2))
                .ForMember(d => d.City, opt => opt.MapFrom(s => s.Spd_EmployerCity))
                .ForMember(d => d.Province, opt => opt.MapFrom(s => s.Spd_EmployerProvince))
                .ForMember(d => d.Country, opt => opt.MapFrom(s => s.Spd_EmployerCountry))
                .ForMember(d => d.PostalCode, opt => opt.MapFrom(s => s.Spd_EmployerPostalCode))
                .ReverseMap();

            _ = CreateMap<PermitLicence, Spd_Licence>()
                .ForMember(d => d.Spd_ExpiryDate, opt => opt.Ignore())
                .ForMember(d => d.StatusCode, opt => opt.Ignore())
                .ForMember(d => d.Spd_PermitPurposeOther, opt => opt.MapFrom(s => s.PermitOtherRequiredReason))
                .ForMember(d => d.Spd_EmployerName, opt => opt.MapFrom(s => s.EmployerName))
                .ForMember(d => d.Spd_EmployerContactName, opt => opt.MapFrom(s => s.SupervisorName))
                .ForMember(d => d.Spd_EmployerEmail, opt => opt.MapFrom(s => s.SupervisorEmailAddress))
                .ForMember(d => d.Spd_EmployerPhoneNumber, opt => opt.MapFrom(s => s.SupervisorPhoneNumber))
                .ForMember(d => d.Spd_EmployerAddress1, opt => opt.MapFrom(s => s.EmployerPrimaryAddress == null || s.EmployerPrimaryAddress.AddressLine1 == null ? string.Empty : s.EmployerPrimaryAddress.AddressLine1))
                .ForMember(d => d.Spd_EmployerAddress2, opt => opt.MapFrom(s => s.EmployerPrimaryAddress == null || s.EmployerPrimaryAddress.AddressLine2 == null ? string.Empty : s.EmployerPrimaryAddress.AddressLine2))
                .ForMember(d => d.Spd_EmployerCity, opt => opt.MapFrom(s => s.EmployerPrimaryAddress == null || s.EmployerPrimaryAddress.City == null ? string.Empty : s.EmployerPrimaryAddress.City))
                .ForMember(d => d.Spd_EmployerProvince, opt => opt.MapFrom(s => s.EmployerPrimaryAddress == null || s.EmployerPrimaryAddress.Province == null ? string.Empty : s.EmployerPrimaryAddress.Province))
                .ForMember(d => d.Spd_EmployerCountry, opt => opt.MapFrom(s => s.EmployerPrimaryAddress == null || s.EmployerPrimaryAddress.Country == null ? string.Empty : s.EmployerPrimaryAddress.Country))
                .ForMember(d => d.Spd_EmployerPostalCode, opt => opt.MapFrom(s => s.EmployerPrimaryAddress == null || s.EmployerPrimaryAddress.PostalCode == null ? string.Empty : s.EmployerPrimaryAddress.PostalCode))
                .ForMember(d => d.Spd_Rationale, opt => opt.MapFrom(s => s.Rationale))
                .ForMember(d => d.Spd_PermitPurpose, opt => opt.MapFrom(s => GetDataversePermitPurposes(s.PermitPurposeEnums)));
        }

        internal static LicenceStatusEnum? GetLicenceStatusEnum(Spd_Licence_StatusCode? statusCode)
        {
            return statusCode.HasValue
                ? Enum.Parse<LicenceStatusEnum>(statusCode.Value.ToString())
                : null;
        }

        internal static IEnumerable<WorkerCategoryTypeEnum> GetCategoryCodes(Spd_Licence licence)
        {
            if (SharedMappingFuncs.GetServiceType(licence.Spd_LicenceType?.Id) == ServiceTypeEnum.SpecialProvincialConstable)
                return [];

            return licence.Spd_Spd_Licence_Spd_CaseLicenceCategory_LicenceId
                .Where(category => category.Spd_Accepted == Spd_YesNo.Yes && category.StateCode == Spd_CaseLicenceCategory_StateCode.Active)
                .Select(category => Enum.Parse<WorkerCategoryTypeEnum>(
                    DynamicsContextLookupHelpers.LookupLicenceCategoryKey(category.Spd_LicenceCategoryId.Id)))
                .ToList();
        }

        internal static BizTypeEnum GetBizType(Spd_Licence licence)
        {
            int? bizTypeValue = IsSecurityBusinessLicence(licence)
                ? (int?)licence.Spd_Account_Spd_Licence_LicenceHolder?.Spd_LicensingBusinessType
                : null;

            return bizTypeValue.HasValue
                ? SharedMappingFuncs.GetBizTypeEnum(bizTypeValue).GetValueOrDefault()
                : BizTypeEnum.None;
        }

        internal static Guid? GetLicenceHolderId(Spd_Licence licence)
        {
            return licence.Spd_LicenceHolder?.Id;
        }

        internal static string? GetBizLegalName(Spd_Licence licence)
        {
            return IsOrgLicence(licence)
                ? licence.Spd_Account_Spd_Licence_LicenceHolder?.Spd_OrganizationLegalName
                : null;
        }

        private static bool IsOrgLicence(Spd_Licence licence)
        {
            ServiceTypeEnum? serviceType = SharedMappingFuncs.GetServiceType(licence.Spd_LicenceType?.Id);
            return serviceType.HasValue && OrgServiceTypes.Contains(serviceType.Value);
        }

        private static bool IsSecurityBusinessLicence(Spd_Licence licence)
        {
            return SharedMappingFuncs.GetServiceType(licence.Spd_LicenceType?.Id) == ServiceTypeEnum.SecurityBusinessLicence;
        }

        private static Guid? GetDogTeamId(Spd_Licence licence)
        {
            return SharedMappingFuncs.GetServiceType(licence.Spd_LicenceType?.Id) == ServiceTypeEnum.GDSDTeamCertification
                ? licence.Spd_Licence_Spd_DogTeam_LicenceId.FirstOrDefault()?.Spd_DogTeamId
                : null;
        }

        private static bool? GetDogReasonFlag(Spd_Licence licence, Spd_RequestDogsPurpose purpose)
        {
            return licence.Spd_RequestDogsReasons == null
                ? null
                : licence.Spd_RequestDogsReasons.Contains(purpose);
        }

        private static IEnumerable<PermitPurposeEnum>? GetPermitPurposeEnums(IEnumerable<Spd_PermitPurpose> purposes)
        {
            string values = string.Join(",", purposes.Select(purpose => ((int)purpose).ToString()));
            return SharedMappingFuncs.GetPermitPurposeEnums(string.IsNullOrEmpty(values) ? null : values);
        }

        private static IEnumerable<Spd_PermitPurpose>? GetDataversePermitPurposes(IEnumerable<PermitPurposeEnum>? purposes)
        {
            string? values = SharedMappingFuncs.GetPermitPurposeOptionSets(purposes);
            return values == null
                ? null
                : values.Split(',').Select(value => (Spd_PermitPurpose)int.Parse(value)).ToList();
        }

        private static DateOnly? GetDateOnly(DateTime? date)
        {
            return date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
        }

        private static DateTimeOffset ToDateTimeOffset(DateTime? date)
        {
            if (!date.HasValue)
                return default;

            return new DateTimeOffset(DateTime.SpecifyKind(date.Value, DateTimeKind.Utc));
        }
    }
}

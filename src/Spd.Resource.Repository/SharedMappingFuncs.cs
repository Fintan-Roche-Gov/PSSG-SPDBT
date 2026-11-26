using Microsoft.OData.Edm;
using Spd.Resource.Repository.PersonLicApplication;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using System.Text.RegularExpressions;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;

namespace Spd.Resource.Repository;
internal static class SharedMappingFuncs
{
    internal static int? GetYesNo(bool? value)
    {
        if (value == null) return null;
        return value.Value ? (int)YesNoOptionSet.Yes : (int)YesNoOptionSet.No;
    }

    internal static bool? GetBool(int? value)
    {
        if (value == null) return null;
        if (value == (int)YesNoOptionSet.Yes) return true;
        return false;
    }

    internal static int? GetGender(GenderEnum? code)
    {
        if (code == null) return null;
        return (int)Enum.Parse<GenderOptionSet>(code.ToString());
    }

    internal static GenderEnum? GetGenderEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<GenderEnum>(Enum.GetName(typeof(GenderOptionSet), optionset));
    }
    internal static DateOnly? GetDateOnly(Date? date)
    {
        if (date == null) return null;
        return new DateOnly(date.Value.Year, date.Value.Month, date.Value.Day);
    }

    internal static DateTimeOffset? GetDateTimeOffset(Date? date)
    {
        if (date == null) return null;
        return new DateTimeOffset(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0, TimeSpan.Zero);
    }

    internal static Date? GetDateFromDateOnly(DateOnly? dateOnly)
    {
        if (dateOnly == null) return null;
        return new Microsoft.OData.Edm.Date(dateOnly.Value.Year, dateOnly.Value.Month, dateOnly.Value.Day);
    }

    internal static Date? GetDate(DateTimeOffset? datetime)
    {
        if (datetime == null) return null;
        return new Microsoft.OData.Edm.Date(datetime.Value.Year, datetime.Value.Month, datetime.Value.Day);
    }

    internal static DateOnly? GetDateOnlyFromDateTimeOffset(DateTimeOffset? datetime)
    {
        if (datetime == null) return null;
        return new DateOnly(datetime.Value.Year, datetime.Value.Month, datetime.Value.Day);
    }

    internal static LicenceTermEnum? GetLicenceTermEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<LicenceTermEnum>(Enum.GetName(typeof(LicenceTermOptionSet), optionset));
    }

    internal static ServiceTypeEnum? GetServiceType(Guid? serviceTypeGuid)
    {
        if (serviceTypeGuid == null) return null;
        return Enum.Parse<ServiceTypeEnum>(global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.GetServiceTypeName(serviceTypeGuid));
    }

    internal static BizTypeEnum? GetBizTypeEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<BizTypeEnum>(Enum.GetName(typeof(BizTypeOptionSet), optionset));
    }

    internal static int? GetBizType(BizTypeEnum? code)
    {
        if (code == null) return (int)BizTypeOptionSet.None;
        return (int)Enum.Parse<BizTypeOptionSet>(code.ToString());
    }

    internal static ApplicationTypeEnum? GetLicenceApplicationTypeEnum(int? applicationTypeOptionSet)
    {
        if (applicationTypeOptionSet == null)
            return null;
        return applicationTypeOptionSet switch
        {
            (int)LicenceApplicationTypeOptionSet.Update => ApplicationTypeEnum.Update,
            (int)LicenceApplicationTypeOptionSet.Replacement => ApplicationTypeEnum.Replacement,
            (int)LicenceApplicationTypeOptionSet.New => ApplicationTypeEnum.New,
            (int)LicenceApplicationTypeOptionSet.Renewal => ApplicationTypeEnum.Renewal,
            _ => throw new ArgumentException("invalid int application type option set")
        };
    }

    internal static int? GetLicenceApplicationTypeOptionSet(ApplicationTypeEnum? applicationType)
    {
        if (applicationType == null)
            return null;
        return applicationType switch
        {
            ApplicationTypeEnum.Update => (int)LicenceApplicationTypeOptionSet.Update,
            ApplicationTypeEnum.Replacement => (int)LicenceApplicationTypeOptionSet.Replacement,
            ApplicationTypeEnum.New => (int)LicenceApplicationTypeOptionSet.New,
            ApplicationTypeEnum.Renewal => (int)LicenceApplicationTypeOptionSet.Renewal,
            _ => throw new ArgumentException("invalid application type code")
        };
    }

    internal static int? GetApplicationStatus(ApplicationStatusEnum? code)
    {
        if (code == null) return (int)ApplicationStatusOptionSet.Incomplete;
        return (int)Enum.Parse<ApplicationStatusOptionSet>(code.ToString());
    }

    internal static ApplicationStatusEnum? GetApplicationStatusEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<ApplicationStatusEnum>(Enum.GetName(typeof(ApplicationStatusOptionSet), optionset));
    }
    internal static int? GetPoliceRoleOptionSet(PoliceOfficerRoleEnum? policeRole)
    {
        if (policeRole == null)
            return null;
        return (int)Enum.Parse<PoliceOfficerRoleOptionSet>(policeRole.ToString());
    }

    internal static PoliceOfficerRoleEnum? GetPoliceRoleEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<PoliceOfficerRoleEnum>(Enum.GetName(typeof(PoliceOfficerRoleOptionSet), optionset));
    }

    internal static string? GetPermitPurposeOptionSets(IEnumerable<PermitPurposeEnum>? permitPurposes)
    {
        if (permitPurposes == null) return null;
        var result = String.Join(',', permitPurposes.Select(p => ((int)Enum.Parse<PermitPurposeOptionSet>(p.ToString())).ToString()).ToArray());
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    internal static IEnumerable<PermitPurposeEnum> GetPermitPurposeEnums(string? optionsetStr)
    {
        if (optionsetStr == null) return null;
        string[] strs = optionsetStr.Split(',');
        return strs.Select(s => Enum.Parse<PermitPurposeEnum>(Enum.GetName(typeof(PermitPurposeOptionSet), Int32.Parse(s)))).ToList();
    }

    internal static int? GetBizTypeOptionSet(BizTypeEnum? bizType)
    {
        if (bizType == null)
            return null;
        return (int)Enum.Parse<BizTypeOptionSet>(bizType.ToString());
    }

    internal static O? GetOptionset<E, O>(E? code)
        where E : struct, Enum
        where O : struct, Enum
    {
        if (code == null) return null;
        return Enum.Parse<O>(code.ToString());
    }

    internal static E? GetEnum<O, E>(int? optionset)
        where O : struct, Enum
        where E : struct, Enum
    {
        if (optionset == null) return null;
        return Enum.Parse<E>(Enum.GetName(typeof(O), optionset));
    }

    internal static WorkerCategoryTypeEnum[] GetWorkerCategoryTypeEnums(ICollection<Spd_LicenceCategory> categories)
    {
        List<WorkerCategoryTypeEnum> codes = new() { };
        foreach (Spd_LicenceCategory cat in categories)
        {
            codes.Add(Enum.Parse<WorkerCategoryTypeEnum>(global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LookupLicenceCategoryKey(cat.Spd_LicenceCategoryId)));
        }
        return codes.ToArray();
    }

    internal static string? GetUploadedDocumentOptionSets(IEnumerable<UploadedDocumentEnum>? uploadDocs)
    {
        if (uploadDocs == null) return null;
        var result = String.Join(',', uploadDocs.Select(p => ((int)Enum.Parse<UploadedDocumentOptionSet>(p.ToString())).ToString()).ToArray());
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    internal static IEnumerable<UploadedDocumentEnum> GetUploadedDocumentEnums(string? optionsetStr)
    {
        if (optionsetStr == null) return null;
        string[] strs = optionsetStr.Split(',');
        return strs.Select(s => Enum.Parse<UploadedDocumentEnum>(Enum.GetName(typeof(UploadedDocumentOptionSet), Int32.Parse(s)))).ToList();
    }

    internal static string? GetPayeeType(int? code)
    {
        if (code == null) return null;
        return Enum.GetName(typeof(PayerPreferenceOptionSet), code);
    }

    internal static int? GetPayeeTypeCode(PayerPreferenceTypeCode? code)
    {
        if (code == null) return null;
        try
        {
            return (int)Enum.Parse<PayerPreferenceOptionSet>(code.ToString());
        }
        catch
        {
            return null;
        }
    }

    internal static ContactRoleCode? GetContactRoleCode(IEnumerable<Spd_Role> spdRoles)
    {
        Spd_Role role = spdRoles.FirstOrDefault();
        if (role == null) return null;
        return Enum.Parse<ContactRoleCode>(
           global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.RoleGuidDictionary.FirstOrDefault(x => x.Value == role.Spd_RoleId).Key);
    }
    internal static bool GetIdentityConfirmed(ApplicationOriginTypeEnum? origin, ApplicationTypeEnum type)
    {
        bool isNotPortal = origin == null || origin != ApplicationOriginTypeEnum.Portal;
        bool isNewOrRenewal = type == ApplicationTypeEnum.New ||
                              type == ApplicationTypeEnum.Renewal;

        return !(isNotPortal && isNewOrRenewal);
    }
    internal static string? GetDogReasonOptionSets(bool? IsDogsPurposeDetectionDrugs, bool? IsDogsPurposeProtection, bool? IsDogsPurposeDetectionExplosives)
    {
        List<string> reasons = new();

        if (IsDogsPurposeDetectionDrugs != null && IsDogsPurposeDetectionDrugs == true)
            reasons.Add(((int)RequestDogPurposeOptionSet.DetectionDrugs).ToString());
        if (IsDogsPurposeDetectionExplosives != null && IsDogsPurposeDetectionExplosives == true)
            reasons.Add(((int)RequestDogPurposeOptionSet.DetectionExplosives).ToString());
        if (IsDogsPurposeProtection != null && IsDogsPurposeProtection == true)
            reasons.Add(((int)RequestDogPurposeOptionSet.Protection).ToString());
        var result = String.Join(',', reasons.ToArray());

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
    internal static bool? GetDogReasonFlag(string dogreasonsStr, RequestDogPurposeOptionSet type)
    {
        if (dogreasonsStr == null) return null;
        string[] reasons = dogreasonsStr.Split(',');
        string str = ((int)type).ToString();
        if (reasons.Any(s => s == str)) return true;

        return false;
    }

    internal static string? GetWeightStr(int? weight, WeightUnitEnum? unit)
    {
        if (unit != null)
        {
            return unit switch
            {
                WeightUnitEnum.Kilograms => weight + "kg",
                WeightUnitEnum.Pounds => weight + "lb",
            };
        }
        else
        {
            return weight?.ToString();
        }
    }

    //str should be like 130lb or 65kg or lb or kg or 130
    internal static int? GetWeightNumber(string? str)
    {
        if (str == null) return null;
        try
        {
            string temp = str.Replace("lb", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("kg", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            return int.Parse(temp);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    //str should be like 130lb or 65kg or lb or kg or 130
    internal static WeightUnitEnum? GetWeightUnitCode(string? str)
    {
        if (str == null) return null;
        try
        {
            string temp = Regex.Replace(str, @"\d", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(3));
            if (string.Equals(temp, "kg", StringComparison.InvariantCultureIgnoreCase)) return WeightUnitEnum.Kilograms;
            if (string.Equals(temp, "lb", StringComparison.InvariantCultureIgnoreCase)) return WeightUnitEnum.Pounds;
            else
                return null;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    internal static string? GetHeightStr(int? height, HeightUnitEnum? unit)
    {
        //if residential address is the same as mailing address, fe will send an empty mailing address
        if (unit != null)
        {
            return unit switch
            {
                HeightUnitEnum.Centimeters => height + "cm",
                HeightUnitEnum.Inches => height + "in", //todo: when ui decide what to use.
            };
        }
        else
        {
            return height?.ToString();
        }
    }

    //str should be like 130lb or 65kg or lb or kg or 130
    internal static int? GetHeightNumber(string? str)
    {
        if (str == null) return null;
        try
        {
            string temp = str.Replace("cm", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("in", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            return int.Parse(temp);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    //str should be like 130lb or 65kg or lb or kg or 130
    internal static HeightUnitEnum? GetHeightUnitCode(string? str)
    {
        if (str == null) return null;
        try
        {
            string temp = Regex.Replace(str, @"\d", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(3));
            if (string.Equals(temp, "in", StringComparison.InvariantCultureIgnoreCase)) return HeightUnitEnum.Inches;
            if (string.Equals(temp, "cm", StringComparison.InvariantCultureIgnoreCase)) return HeightUnitEnum.Centimeters;
            else
                return null;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    internal static int? GetHairColor(HairColourEnum? code)
    {
        if (code == null) return null;
        return (int)Enum.Parse<HairColorOptionSet>(code.ToString());
    }

    internal static HairColourEnum? GetHairColorEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<HairColourEnum>(Enum.GetName(typeof(HairColorOptionSet), optionset));
    }

    internal static int? GetEyeColor(EyeColourEnum? code)
    {
        if (code == null) return null;
        return (int)Enum.Parse<EyeColorOptionSet>(code.ToString());
    }

    internal static EyeColourEnum? GetEyeColorEnum(int? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<EyeColourEnum>(Enum.GetName(typeof(EyeColorOptionSet), optionset));
    }

    internal static DateTimeOffset? GetDeclarationDate(bool? agree)
    {
        return agree != null && agree == true ? DateTime.Now : null;
    }

    internal static MailingAddr? GetMailingAddressData(Spd_DogTrainingSchool s)
    {
        MailingAddr mailingAddress = new();
        mailingAddress.AddressLine1 = s.Spd_Street1;
        mailingAddress.AddressLine2 = s.Spd_Street2;
        mailingAddress.City = s.Spd_City;
        mailingAddress.Province = s.Spd_ProvinceState;
        mailingAddress.Country = s.Spd_Country;
        mailingAddress.PostalCode = s.Spd_PostalZipCode;
        return mailingAddress;
    }

    internal static MailingAddr? GetMailingAddressData(Spd_Application c)
    {
        MailingAddr mailingAddress = new();
        mailingAddress.AddressLine1 = c.Spd_AddressLine1;
        mailingAddress.AddressLine2 = c.Spd_AddressLine2;
        mailingAddress.City = c.Spd_City;
        mailingAddress.Province = c.Spd_Province;
        mailingAddress.Country = c.Spd_Country;
        mailingAddress.PostalCode = c.Spd_PostalCode;
        return mailingAddress;
    }

    internal static MailingAddr? GetMailingAddressData(ContactEntity c)
    {
        MailingAddr mailingAddress = new();
        mailingAddress.AddressLine1 = c.Address1_Line1;
        mailingAddress.AddressLine2 = c.Address1_Line2;
        mailingAddress.City = c.Address1_City;
        mailingAddress.Province = c.Address1_StateOrProvince;
        mailingAddress.Country = c.Address1_Country;
        mailingAddress.PostalCode = c.Address1_PostalCode;
        return mailingAddress;
    }

    internal static Addr? GetEmployerAddressData(Spd_Application app)
    {
        Addr addr = new();
        addr.AddressLine1 = app.Spd_EmployerAddress1;
        addr.AddressLine2 = app.Spd_EmployerAddress2;
        addr.City = app.Spd_EmployerCity;
        addr.Province = app.Spd_EmployerProvince;
        addr.Country = app.Spd_EmployerCountry;
        addr.PostalCode = app.Spd_EmployerPostalCode;
        return addr;
    }
    internal static ResidentialAddr? GetResidentialAddressData(ContactEntity c)
    {
        ResidentialAddr mailingAddress = new();
        mailingAddress.AddressLine1 = c.Address2_Line1;
        mailingAddress.AddressLine2 = c.Address2_Line2;
        mailingAddress.City = c.Address2_City;
        mailingAddress.Province = c.Address2_StateOrProvince;
        mailingAddress.Country = c.Address2_Country;
        mailingAddress.PostalCode = c.Address2_PostalCode;
        return mailingAddress;
    }

    internal static ResidentialAddr? GetResidentialAddressData(Spd_Application app)
    {
        ResidentialAddr addr = new();
        addr.AddressLine1 = app.Spd_ResidentialAddress1;
        addr.AddressLine2 = app.Spd_ResidentialAddress2;
        addr.City = app.Spd_ResidentialCity;
        addr.Province = app.Spd_ResidentialProvince;
        addr.Country = app.Spd_ResidentialCountry;
        addr.PostalCode = app.Spd_ResidentialPostalCode;
        return addr;
    }

    internal static bool? IsMailingResidentialSame(ContactEntity c)
    {
        if (c.Address1_Line1 == null
            && c.Address1_Line2 == null
            && c.Address1_City == null
            && c.Address1_StateOrProvince == null
            && c.Address1_Country == null
            && c.Address1_PostalCode == null
            && c.Address2_Line1 == null
            && c.Address2_Line2 == null
            && c.Address2_City == null
            && c.Address2_StateOrProvince == null
            && c.Address2_Country == null
            && c.Address2_PostalCode == null)
            return null;
        return c.Address1_Line1 == c.Address2_Line1 &&
            c.Address1_Line2 == c.Address2_Line2 &&
            c.Address1_City == c.Address2_City &&
            c.Address1_StateOrProvince == c.Address2_StateOrProvince &&
            c.Address1_Country == c.Address2_Country &&
            c.Address1_PostalCode == c.Address2_PostalCode;
    }
}
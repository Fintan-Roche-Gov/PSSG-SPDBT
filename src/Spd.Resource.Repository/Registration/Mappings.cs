using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Tools;

namespace Spd.Resource.Repository.Registration
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<OrgRegistration, Spd_OrgRegistration>()
            .ForMember(d => d.Spd_OrgRegistrationId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Spd_WorksWithDetails, opt => opt.MapFrom(s => s.EmployeeInteractionDetails))
            .ForMember(d => d.Spd_WorksWith, opt => opt.MapFrom(s => (Spd_OrganizationClientBase?)Enum.Parse<Spd_OrganizationClientBase>(s.EmployeeInteractionFlag.ToString())))
            .ForMember(d => d.Spd_EstimatedApplicationsSubmittedPerYear, opt => opt.MapFrom(s => (Spd_EstimatedApplicationsPerYear?)Enum.Parse<Spd_EstimatedApplicationsPerYear>(s.ScreeningsCount.ToString())))
            .ForMember(d => d.Spd_PayerPreference, opt => opt.MapFrom(s => (Spd_PayerType?)Enum.Parse<Spd_PayerType>(s.PayerPreference.ToString())))
            .ForMember(d => d.Spd_ReceiveMoneyCompensation, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNoOptionSet(s.EmployeeMonetaryCompensationFlag)))
            .ForMember(d => d.Spd_City, opt => opt.MapFrom(s => s.MailingCity))
            .ForMember(d => d.Spd_Country, opt => opt.MapFrom(s => s.MailingCountry))
            .ForMember(d => d.Spd_Declaration, opt => opt.MapFrom(s => s.AgreeToTermsAndConditions))
            .ForMember(d => d.Spd_PostalCode, opt => opt.MapFrom(s => s.MailingPostalCode))
            .ForMember(d => d.Spd_Province, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.MailingProvince)))
            .ForMember(d => d.Spd_Street1, opt => opt.MapFrom(s => s.MailingAddressLine1))
            .ForMember(d => d.Spd_Street2, opt => opt.MapFrom(s => s.MailingAddressLine2))
            .ForMember(d => d.Spd_AuthorizedContactEmail, opt => opt.MapFrom(s => s.ContactEmail))
            .ForMember(d => d.Spd_AuthorizedContactGivenName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.ContactGivenName)))
            .ForMember(d => d.Spd_AuthorizedContactSurname, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.ContactSurname)))
            .ForMember(d => d.Spd_JobTitle, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.ContactJobTitle)))
            .ForMember(d => d.Spd_AuthorizedContactPhoneNumber, opt => opt.MapFrom(s => s.ContactPhoneNumber))
            .ForMember(d => d.Spd_OrganizationName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.OrganizationName)))
            .ForMember(d => d.Spd_OrganizationLegalName, opt => opt.MapFrom(s => StringHelper.ToTitleCase(s.OrganizationLegalName)))
            .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.GenericEmail))
            .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.GenericPhoneNumber))
            .ForMember(d => d.Spd_IdentityGuid, opt => opt.MapFrom(s => s.BizIdentityGuid))
            .ForMember(d => d.Spd_IdentityProvider, opt => opt.MapFrom(s => GetPortalUserIdentityType(s.IdentityProviderTypeCode)))
            .ForMember(d => d.Spd_PortalUserIdentityGuid, opt => opt.MapFrom(s => s.BCeIDUserGuid))
            .ForMember(d => d.Spd_PotentialDuplicate, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNoOptionSet(s.HasPotentialDuplicate)))
            .ReverseMap()
            .ForMember(d => d.MailingProvince, opt => opt.MapFrom(s => s.Spd_Province))
            .ForMember(d => d.ContactGivenName, opt => opt.MapFrom(s => s.Spd_AuthorizedContactGivenName))
            .ForMember(d => d.ContactSurname, opt => opt.MapFrom(s => s.Spd_AuthorizedContactSurname))
            .ForMember(d => d.ContactJobTitle, opt => opt.MapFrom(s => s.Spd_JobTitle))
            .ForMember(d => d.OrganizationName, opt => opt.MapFrom(s => s.Spd_OrganizationName));

            _ = CreateMap<Spd_OrgRegistration, OrgRegistrationResult>()
            .IncludeBase<Spd_OrgRegistration, OrgRegistration>()
            .ForMember(d => d.CreatedOn, opt => opt.MapFrom(s => s.CreatedOn))
            .ForMember(d => d.OrgRegistrationNumber, opt => opt.MapFrom(s => s.Spd_RegistrationNumber))
            .ForMember(d => d.OrgRegistrationId, opt => opt.MapFrom(s => s.Spd_OrgRegistrationId))
            .ForMember(d => d.OrgRegistrationStatusStr, opt => opt.MapFrom(s => ((OrgRegistrationStatus?)s.StatusCode).ToString()));
        }

        private static Spd_IdentityProvider? GetPortalUserIdentityType(IdentityProviderTypeEnum? code)
        {
            if (code == null) return null;
            return code switch
            {
                IdentityProviderTypeEnum.BceidBusiness => Spd_IdentityProvider.BusinessBceid,
                IdentityProviderTypeEnum.BcServicesCard => Spd_IdentityProvider.BcServicesCard,
                IdentityProviderTypeEnum.Iidir => Spd_IdentityProvider.IDIR,
                _ => null
            };
        }

    }
}

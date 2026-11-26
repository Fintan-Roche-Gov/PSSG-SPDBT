using AutoMapper;
using Spd.Resource.Repository.Biz;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.MDRARegistration;
internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<MDRARegistration, Spd_OrgRegistration>()
             .ForMember(d => d.Spd_OrgRegistrationId, opt => opt.MapFrom(s => Guid.NewGuid()))
             .ForMember(d => d.Spd_ApplicationType, opt => opt.MapFrom(s => SharedMappingFuncs.GetOptionset<ApplicationTypeEnum, Spd_LicenceApplicationType>(s.ApplicationTypeCode)))
             .ForMember(d => d.Spd_AuthorizedContactGivenName, opt => opt.MapFrom(s => s.BizOwnerGivenNames == null ? null : s.BizOwnerGivenNames.Trim()))
             .ForMember(d => d.Spd_AuthorizedContactSurname, opt => opt.MapFrom(s => s.BizOwnerSurname))
             .ForMember(d => d.Spd_OrganizationLegalName, opt => opt.MapFrom(s => s.BizLegalName))
             .ForMember(d => d.Spd_OrganizationName, opt => opt.MapFrom(s => s.BizTradeName))
             .ForMember(d => d.Spd_Email, opt => opt.MapFrom(s => s.BizEmailAddress))
             .ForMember(d => d.Spd_PhoneNumber, opt => opt.MapFrom(s => s.BizPhoneNumber))
             .ForMember(d => d.Spd_Declaration, opt => opt.MapFrom(s => true))
             .ForMember(d => d.Spd_Street1, opt => opt.MapFrom(s => s.BizMailingAddress == null ? null : s.BizMailingAddress.AddressLine1))
             .ForMember(d => d.Spd_Street2, opt => opt.MapFrom(s => s.BizMailingAddress == null ? null : s.BizMailingAddress.AddressLine2))
             .ForMember(d => d.Spd_City, opt => opt.MapFrom(s => s.BizMailingAddress == null ? null : s.BizMailingAddress.City))
             .ForMember(d => d.Spd_Province, opt => opt.MapFrom(s => s.BizMailingAddress == null ? null : s.BizMailingAddress.Province))
             .ForMember(d => d.Spd_PostalCode, opt => opt.MapFrom(s => s.BizMailingAddress == null ? null : s.BizMailingAddress.PostalCode))
             .ForMember(d => d.Spd_Country, opt => opt.MapFrom(s => s.BizMailingAddress == null ? null : s.BizMailingAddress.Country))
             .ReverseMap()
             .ForMember(d => d.ApplicationTypeCode, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<Spd_LicenceApplicationType, ApplicationTypeEnum>((int?)s.Spd_ApplicationType)))
             .ForMember(d => d.BizOwnerGivenNames, opt => opt.MapFrom(s => s.Spd_AuthorizedContactGivenName))
        ;

        _ = CreateMap<CreateMDRARegistrationCmd, Spd_OrgRegistration>()
            .IncludeBase<MDRARegistration, Spd_OrgRegistration>()
            .ForMember(d => d.Spd_PotentialDuplicate, opt => opt.MapFrom(s => SharedMappingFuncs.GetYesNo(s.HasPotentialDuplicate) == null ? null : (Spd_YesNo?)SharedMappingFuncs.GetYesNo(s.HasPotentialDuplicate)))
            ;

        _ = CreateMap<Spd_OrgRegistration, MDRARegistrationResp>()
            .IncludeBase<Spd_OrgRegistration, MDRARegistration>()
            .ForMember(d => d.RegistrationId, opt => opt.MapFrom(s => s.Spd_OrgRegistrationId))
            .ForMember(d => d.RegistrationNumber, opt => opt.MapFrom(s => s.Spd_RegistrationNumber))
            ;

        CreateMap<MDRARegistration, List<Spd_Address>>()
        .ConvertUsing((src, dest, context) =>
        {
            var result = new List<Spd_Address>();

            // Main Office
            if (src.BizAddress != null)
            {
                result.Add(new Spd_Address
                {
                    Spd_AddressId = Guid.NewGuid(),
                    Spd_Type = Spd_AddressType.MainOffice,
                    Spd_BranchManagerName = src.BizManagerFullName,
                    Spd_BranchPhone = src.BizManagerPhoneNumber,
                    Spd_BranchEmail = src.BizManagerEmailAddress,
                    Spd_Address1 = src.BizAddress.AddressLine1,
                    Spd_Address2 = src.BizAddress.AddressLine2,
                    Spd_City = src.BizAddress.City,
                    Spd_ProvinceState = src.BizAddress.Province,
                    Spd_PostalCode = src.BizAddress.PostalCode,
                    Spd_Country = src.BizAddress.Country
                });
            }

            // Mailing Address
            if (src.BizMailingAddress != null)
            {
                result.Add(new Spd_Address
                {
                    Spd_AddressId = Guid.NewGuid(),
                    Spd_Type = Spd_AddressType.Mailing,
                    Spd_Address1 = src.BizMailingAddress.AddressLine1,
                    Spd_Address2 = src.BizMailingAddress.AddressLine2,
                    Spd_City = src.BizMailingAddress.City,
                    Spd_ProvinceState = src.BizMailingAddress.Province,
                    Spd_PostalCode = src.BizMailingAddress.PostalCode,
                    Spd_Country = src.BizMailingAddress.Country
                });
            }

            // Branches
            if (src.Branches != null)
            {
                foreach (var branch in src.Branches)
                {
                    result.Add(new Spd_Address
                    {
                        Spd_AddressId = Guid.NewGuid(),
                        Spd_Type = Spd_AddressType.Branch,
                        Spd_BranchManagerName = branch.BranchManager,
                        Spd_BranchPhone = branch.BranchPhoneNumber,
                        Spd_BranchEmail = branch.BranchEmailAddr,
                        Spd_Address1 = branch.AddressLine1,
                        Spd_Address2 = branch.AddressLine2,
                        Spd_City = branch.City,
                        Spd_ProvinceState = branch.Province,
                        Spd_PostalCode = branch.PostalCode,
                        Spd_Country = branch.Country
                    });
                }
            }

            return result;
        });

        CreateMap<List<Spd_Address>, MDRARegistrationResp>()
        .AfterMap((src, dest) =>
        {
            foreach (var address in src)
            {
                switch (address.Spd_Type)
                {
                    case Spd_AddressType.MainOffice:
                        dest.BizManagerFullName = address.Spd_BranchManagerName;
                        dest.BizManagerPhoneNumber = address.Spd_BranchPhone;
                        dest.BizManagerEmailAddress = address.Spd_BranchEmail;
                        dest.BizAddress = new Addr
                        {
                            AddressLine1 = address.Spd_Address1,
                            AddressLine2 = address.Spd_Address2,
                            City = address.Spd_City,
                            Province = address.Spd_ProvinceState,
                            PostalCode = address.Spd_PostalCode,
                            Country = address.Spd_Country
                        };
                        break;

                    case Spd_AddressType.Mailing:
                        dest.BizMailingAddress = new Addr
                        {
                            AddressLine1 = address.Spd_Address1,
                            AddressLine2 = address.Spd_Address2,
                            City = address.Spd_City,
                            Province = address.Spd_ProvinceState,
                            PostalCode = address.Spd_PostalCode,
                            Country = address.Spd_Country
                        };
                        break;

                    case Spd_AddressType.Branch:
                        dest.Branches ??= new List<BranchAddr>();
                        dest.Branches.Add(new BranchAddr
                        {
                            BranchId = address.Spd_AddressId,
                            BranchManager = address.Spd_BranchManagerName,
                            BranchPhoneNumber = address.Spd_BranchPhone,
                            BranchEmailAddr = address.Spd_BranchEmail,
                            AddressLine1 = address.Spd_Address1,
                            AddressLine2 = address.Spd_Address2,
                            City = address.Spd_City,
                            Province = address.Spd_ProvinceState,
                            PostalCode = address.Spd_PostalCode,
                            Country = address.Spd_Country
                        });
                        break;
                }
            }
        });


    }
}

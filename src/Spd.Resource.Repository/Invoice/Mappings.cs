using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.Invoice
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Spd_Invoice, InvoiceResp>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_InvoiceId ?? Guid.Empty))
            .ForMember(d => d.SiteNumber, opt => opt.MapFrom(s => s.Spd_OrganizationId == null ? null : s.Spd_OrganizationIdName))
            .ForMember(d => d.PartyNumber, opt => opt.MapFrom(s => s.Spd_OrganizationId == null ? null : s.Spd_OrganizationIdName))
            .ForMember(d => d.AccountNumber, opt => opt.MapFrom(s => s.Spd_OrganizationId == null ? null : s.Spd_OrganizationIdName))
            .ForMember(d => d.InvoiceStatus, opt => opt.MapFrom(s => GetInvoiceStatus((int?)s.StatusCode)))
            .ForMember(d => d.NumberOfApplications, opt => opt.MapFrom(s => s.Spd_NumberOfApplications))
            .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.Spd_TotalAmount == null ? 0m : s.Spd_TotalAmount.Value))
            .ForMember(d => d.OrganizationId, opt => opt.Ignore())
            .ForMember(d => d.TransactionDate, opt => opt.Ignore())
            .ForMember(d => d.InvoiceNumber, opt => opt.MapFrom(s => s.Spd_CasTransactionId))
            .ForMember(d => d.GlDate, opt => opt.Ignore())
            .ForMember(d => d.Comments, opt => opt.MapFrom(s => s.Spd_Comments))
            ;
        }

        private static InvoiceStatusEnum GetInvoiceStatus(int? optionset)
        {
            if (optionset == null) return InvoiceStatusEnum.Draft;
            return Enum.Parse<InvoiceStatusEnum>(Enum.GetName(typeof(InvoiceStatusOptionSet), optionset));
        }
    }
}

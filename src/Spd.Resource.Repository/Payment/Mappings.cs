using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.Payment
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<CreatePaymentCmd, Spd_Payment>()
                .ForMember(d => d.Spd_PaymentId, opt => opt.MapFrom(s => s.PaymentId))
                .ForMember(d => d.Spd_Amount, opt => opt.MapFrom(s => s.TransAmount == 0 ? null : new Money(s.TransAmount)))
                .ForMember(d => d.Spd_ManualPayment, opt => opt.MapFrom(s => false))
                .ForMember(d => d.Spd_TransactionId, opt => opt.MapFrom(s => s.TransNumber))
                .ForMember(d => d.Spd_PaymentType, opt => opt.MapFrom(s => (Spd_PaymentType)(int)Enum.Parse<PaymentTypeOptionSet>(s.PaymentType.ToString())))
                .ForMember(d => d.StateCode, opt => opt.MapFrom(s => Spd_Payment_StateCode.Active))
                .ForMember(d => d.Spd_Response, opt => opt.MapFrom(s => GetResponseCode(s.Success)))
                .ForMember(d => d.Spd_DateTimeOfPayment, opt => opt.Ignore())
                .ForMember(d => d.Spd_ErrorDescription, opt => opt.MapFrom(s => s.MessageText))
                .ForMember(d => d.Spd_OrderNumber, opt => opt.MapFrom(s => s.TransOrderId))
                .ForMember(d => d.StatusCode, opt => opt.MapFrom(s => Spd_Payment_StatusCode.Pending));

            _ = CreateMap<UpdatePaymentCmd, Spd_Payment>()
               .ForMember(d => d.StateCode, opt => opt.MapFrom(s => Spd_Payment_StateCode.Inactive))
               .ForMember(d => d.Spd_RefundErrorDescription, opt => opt.MapFrom(s => s.RefundErrorMsg))
               .ForMember(d => d.Spd_RefundId, opt => opt.MapFrom(s => s.RefundId))
               .ForMember(d => d.Spd_RefundDate, opt => opt.Ignore());

            _ = CreateMap<Spd_Payment, PaymentResp>()
                .ForMember(d => d.PaymentId, opt => opt.MapFrom(s => s.Spd_PaymentId ?? Guid.Empty))
                .ForMember(d => d.PaidSuccess, opt => opt.MapFrom(s => s.Spd_Response == Spd_Payment_Spd_Response.Success))
                .ForMember(d => d.Message, opt => opt.MapFrom(s => s.Spd_ErrorDescription))
                .ForMember(d => d.ApplicationId, opt => opt.MapFrom(s => s.Spd_ApplicationId == null ? Guid.Empty : s.Spd_ApplicationId.Id))
                .ForMember(d => d.TransAmount, opt => opt.MapFrom(s => s.Spd_Amount == null ? 0m : s.Spd_Amount.Value))
                .ForMember(d => d.TransOrderId, opt => opt.MapFrom(s => s.Spd_OrderNumber))
                .ForMember(d => d.TransDateTime, opt => opt.MapFrom(s => s.Spd_DateTimeOfPayment == null ? DateTimeOffset.MinValue : new DateTimeOffset(DateTime.SpecifyKind(s.Spd_DateTimeOfPayment.Value, DateTimeKind.Utc))))
                .ForMember(d => d.TransactionNumber, opt => opt.MapFrom(s => s.Spd_TransactionId))
                .ForMember(d => d.PaymentType, opt => opt.MapFrom(s => GetPaymentType((int?)s.Spd_PaymentType)))
                .ForMember(d => d.LicenceTermCode, opt => opt.Ignore())
                .ForMember(d => d.ApplicationTypeCode, opt => opt.Ignore())
                .ForMember(d => d.CaseNumber, opt => opt.Ignore())
                .ForMember(d => d.Refunded, opt => opt.MapFrom(s => s.StatusCode == Spd_Payment_StatusCode.Refunded))
                .ForMember(d => d.ApplicationEmail, opt => opt.Ignore())
                .ForMember(d => d.OrganizationId, opt => opt.MapFrom(s => Guid.Empty))
                .ForMember(d => d.PayeeType, opt => opt.Ignore())
                .ForMember(d => d.ServiceType, opt => opt.Ignore());
        }

        private Spd_Payment_Spd_Response? GetResponseCode(bool? success)
        {
            if (success == null) return null;
            if ((bool)success) return Spd_Payment_Spd_Response.Success;
            else return Spd_Payment_Spd_Response.Failure;
        }

        private PaymentTypeEnum? GetPaymentType(int? paymentType)
        {
            if (paymentType == null) return null;

            return Enum.Parse<PaymentTypeEnum>(Enum.GetName(typeof(PaymentTypeOptionSet), paymentType));
        }
    }
}

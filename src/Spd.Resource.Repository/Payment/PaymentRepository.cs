using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.Payment;
internal class PaymentRepository : IPaymentRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;
    public PaymentRepository(IDataverseContextFactory ctx,
        IMapper mapper)
    {
        _context = ctx.CreateChangeOverwrite();
        _mapper = mapper;
    }
    public async Task<PaymentListResp> QueryAsync(PaymentQry qry, CancellationToken ct)
    {
        IQueryable<Spd_Payment> payments = _context.Spd_PaymentSet;

        if (qry.ApplicationId != null)
            payments = payments.Where(d => d.Spd_ApplicationId != null && d.Spd_ApplicationId.Id == qry.ApplicationId);

        if (qry.PaymentId != null)
            payments = payments.Where(d => d.Spd_PaymentId == qry.PaymentId);

        var result = payments.ToList();
        result = result.OrderByDescending(a => a.CreatedOn).ToList();
        return new PaymentListResp
        {
            Items = _mapper.Map<IEnumerable<PaymentResp>>(result.ToList())
        };
    }
    public async Task<Guid> ManageAsync(PaymentCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            CreatePaymentCmd c => await PaymentCreateAsync(c, ct),
            UpdatePaymentCmd c => await PaymentUpdateAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    private async Task<Guid> PaymentCreateAsync(CreatePaymentCmd cmd, CancellationToken ct)
    {
        Spd_Application? application = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == cmd.ApplicationId).FirstOrDefault();
        if (application == null)
            throw new ArgumentException("invalid application id");

        Spd_Payment? payment = _context.Spd_PaymentSet.Where(p => p.Spd_PaymentId == cmd.PaymentId).FirstOrDefault();
        if (payment != null)
        {
            //payment should not exist, so, should not get here.
            if (payment.Spd_OrderNumber == cmd.TransOrderId)
                return cmd.PaymentId; //payment already updated
            else
                cmd.PaymentId = Guid.NewGuid();
        }

        payment = _mapper.Map<Spd_Payment>(cmd);
        _context.AddObject(payment);
        payment.Spd_ApplicationId = application.ToEntityReference();
        _context.SaveChanges();
        return cmd.PaymentId;
    }

    private async Task<Guid> PaymentUpdateAsync(UpdatePaymentCmd cmd, CancellationToken ct)
    {
        Spd_Payment? payment = _context.Spd_PaymentSet.Where(p => p.Spd_PaymentId == cmd.PaymentId).FirstOrDefault();
        if (payment == null) throw new ApiException(HttpStatusCode.BadRequest, "The payment does not exist.");
        payment = _mapper.Map(cmd, payment);
        if (cmd.PaymentStatus != null)
        {
            payment.StatusCode = (Spd_Payment_StatusCode?)(int)Enum.Parse<Spd_Payment_StatusCode>(cmd.PaymentStatus.ToString());
        }
        _context.UpdateObject(payment);
        _context.SaveChanges();
        return (Guid)payment.Spd_PaymentId;
    }

}


using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.Invoice;
internal class InvoiceRepository : IInvoiceRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;

    public InvoiceRepository(DataverseContext ctx,
        IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    public async Task<InvoiceListResp> QueryAsync(InvoiceQry qry, CancellationToken ct)
    {
        IQueryable<Spd_Invoice> invoices = _context.Spd_InvoiceSet;

        if (!qry.IncludeInactive)
            invoices = invoices.Where(d => d.StateCode != Spd_Invoice_StateCode.Inactive);
        if (qry.OrganizationId != null) invoices = invoices.Where(d => d.Spd_OrganizationId != null && d.Spd_OrganizationId.Id == qry.OrganizationId);
        if (qry.InvoiceId != null) invoices = invoices.Where(d => d.Spd_InvoiceId == qry.InvoiceId);
        if (qry.InvoiceStatus != null)
        {
            int status = (int)Enum.Parse<InvoiceStatusOptionSet>(qry.InvoiceStatus.ToString());
            invoices = invoices.Where(d => (int?)d.StatusCode == status);
        }
        return new InvoiceListResp
        {
            Items = _mapper.Map<IEnumerable<InvoiceResp>>(invoices.ToList())
        };
    }

    public async Task<InvoiceResp> ManageAsync(InvoiceCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            UpdateInvoiceCmd c => await UpdateInvoiceAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    private async Task<InvoiceResp> UpdateInvoiceAsync(UpdateInvoiceCmd c, CancellationToken ct)
    {
        Spd_Invoice? invoice = _context.Spd_InvoiceSet
            .Where(i => i.Spd_InvoiceId == c.InvoiceId)
            .FirstOrDefault();
        if (invoice == null)
            throw new InvalidOperationException("Cannot find the correct invoice.");
        if (c.InvoiceStatus != null)
        {
            int status = (int)Enum.Parse<InvoiceStatusOptionSet>(c.InvoiceStatus.ToString());
            invoice.StatusCode = (Spd_Invoice_StatusCode?)status;
            if (status == (int)InvoiceStatusOptionSet.Paid || status == (int)InvoiceStatusOptionSet.Cancelled)
            {
                invoice.StateCode = Spd_Invoice_StateCode.Inactive;
            }
        }
        if (c.InvoiceNumber != null) invoice.Spd_CasTransactionId = c.InvoiceNumber;
        if (c.CasResponse != null) invoice.Spd_CasResponse = c.CasResponse;
        _context.UpdateObject(invoice);
        _context.SaveChanges();
        return _mapper.Map<InvoiceResp>(invoice);
    }

}



using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository.Report
{
    internal class ReportRepository : IOrgReportRepository
    {
        private readonly DataverseContext _dynaContext;
        private readonly IMapper _mapper;

        public ReportRepository(DataverseContext ctx, IMapper mapper)
        {
            _dynaContext = ctx;
            _mapper = mapper;
        }

        public async Task<OrgReportsResult> QueryReportsAsync(OrgReportListQry qry, CancellationToken ct)
        {
            IQueryable<Spd_PdfReport> reports = _dynaContext.Spd_PdfReportSet
                .Where(r => r.Spd_OrganizationId != null && r.Spd_OrganizationId.Id == qry.OrgId)
                .OrderByDescending(r => r.Spd_ReportDate);

            return new OrgReportsResult(_mapper.Map<IEnumerable<OrgReportResult>>(reports));
        }
    }
}

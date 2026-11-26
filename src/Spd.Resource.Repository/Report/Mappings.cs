using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Report
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Spd_PdfReport, OrgReportResult>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_PdfReportId))
            .ForMember(d => d.ReportDate, opt => opt.MapFrom(s => new DateOnly(s.Spd_ReportDate.Value.Year, s.Spd_ReportDate.Value.Month, s.Spd_ReportDate.Value.Day)));
        }
    }
}

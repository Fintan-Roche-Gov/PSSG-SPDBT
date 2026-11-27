using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.LicApp;
internal class LicAppRepository : ILicAppRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;

    public LicAppRepository(DataverseContext ctx, IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    //for unauth, set applcation status to submitted.
    public async Task<LicenceApplicationCmdResp> CommitLicenceApplicationAsync(Guid applicationId,
        ApplicationStatusEnum status,
        decimal? price,
        CancellationToken ct,
        LicenceTermEnum? term = null)
    {
        Spd_Application? app = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == applicationId).FirstOrDefault();
        if (app == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid ApplicationId");

        app.StatusCode = Enum.Parse<Spd_Application_StatusCode>(status.ToString());

        if (status == ApplicationStatusEnum.Submitted)
            app.StateCode = Spd_Application_StateCode.Inactive;

        app.Spd_SubmittedOn = DateTime.UtcNow;
        app.Spd_PortalModifiedOn = DateTime.UtcNow;

        if (price != null && price >= 0)
            app.Spd_LicenceFee = new Money(price.Value);

        if (term != null) //spdbt-3194
        {
            int? bizLicAppTerm =(int?)SharedMappingFuncs.GetOptionset<LicenceTermEnum, LicenceTermOptionSet>(term);
            if (bizLicAppTerm > (int?)app.Spd_LicenceTerm)
                app.Spd_LicenceTerm = (Spd_LicenceTerm?)bizLicAppTerm;
        }

        _context.UpdateObject(app);
        _context.SaveChanges();

        // For business application, return organization id, for all others, return applicant id
        if (app.Spd_OrganizationId != null)
            return await Task.FromResult(new LicenceApplicationCmdResp(app.Spd_ApplicationId ?? Guid.Empty, null, app.Spd_OrganizationId.Id));
        else
            return await Task.FromResult(new LicenceApplicationCmdResp(app.Spd_ApplicationId ?? Guid.Empty, app.Spd_ApplicantId?.Id ?? Guid.Empty, null));
    }

    public async Task<IEnumerable<LicenceAppListResp>> QueryAsync(LicenceAppQuery qry, CancellationToken cancellationToken)
    {
        IQueryable<Spd_Application> apps = _context.Spd_ApplicationSet;

        if (qry.ApplicantId != null)
        {
            apps = apps.Where(a => a.Spd_ApplicantId != null && a.Spd_ApplicantId.Id == qry.ApplicantId);
        }
        if (qry.BizId != null)
        {
            apps = apps.Where(a => a.Spd_ApplicantId != null && a.Spd_ApplicantId.Id == qry.BizId);
        }
        if (qry.ApplicationId != null)
        {
            apps = apps.Where(a => a.Spd_ApplicationId == qry.ApplicationId);
        }

        var applist = apps.ToList();

        if (qry.ValidServiceTypeCodes != null && qry.ValidServiceTypeCodes.Any())
        {
            List<Guid?> serviceTypeGuid = qry.ValidServiceTypeCodes
                .Select(c => DataverseContextLookupHelpers.GetServiceTypeGuid(c.ToString()))
                .ToList();
            applist = applist.Where(a => serviceTypeGuid.Contains(a.Spd_ServiceTypeId != null ? a.Spd_ServiceTypeId.Id : (Guid?)null)).ToList();
        }

        if (qry.ValidPortalStatus != null && qry.ValidPortalStatus.Any())
        {
            List<int> portalStatusInt = qry.ValidPortalStatus.Select(s => (int)Enum.Parse<ApplicationPortalStatus>(s.ToString())).ToList();
            applist = applist.Where(a => a.Spd_PortalStatus != null && portalStatusInt.Contains((int)a.Spd_PortalStatus)).ToList();
        }

        return await Task.FromResult(_mapper.Map<IList<LicenceAppListResp>>(applist.OrderByDescending(o => o.CreatedOn)));
    }
}

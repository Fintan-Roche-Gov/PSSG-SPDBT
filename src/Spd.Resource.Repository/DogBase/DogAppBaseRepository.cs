using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;

namespace Spd.Resource.Repository.DogBase;
internal abstract class DogAppBaseRepository : IDogAppBaseRepository
{
    protected readonly DataverseContext _context;
    protected readonly IMapper _mapper;

    public DogAppBaseRepository(IDataverseContextFactory ctx, IMapper mapper)
    {
        _context = ctx.CreateChangeOverwrite();
        _mapper = mapper;
    }

    public async System.Threading.Tasks.Task CommitAppAsync(CommitAppCmd cmd, CancellationToken ct)
    {
        Spd_Application? app = await _context.GetApplicationById(cmd.LicenceAppId, ct);
        if (app == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid ApplicationId");

        //spdbt-4143: dog app, no need to verify identification, app go directly to submitted.
        app.StatusCode = Spd_Application_StatusCode.Submitted;
        app.StateCode = Spd_Application_StateCode.Inactive;
        app.Spd_SubmittedOn = DateTime.UtcNow;
        app.Spd_PortalModifiedOn = DateTime.UtcNow;
        app.Spd_LicenceFee = new Money(0);
        _context.UpdateObject(app);
        _context.SaveChanges();
    }
}

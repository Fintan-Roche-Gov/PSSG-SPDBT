using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse;

namespace Spd.Resource.Repository.Application;
internal partial class ApplicationRepository : IApplicationRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<IApplicationRepository> _logger;

    public ApplicationRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper, ILogger<IApplicationRepository> logger)
    {        _context = ctx;
        _mapper = mapper;
        _logger = logger;
    }
}



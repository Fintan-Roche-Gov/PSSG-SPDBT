using AutoMapper;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dynamics;

namespace Spd.Engine.Validation
{
    internal partial class DuplicateCheckEngine : IDuplicateCheckEngine
    {
        private readonly DynamicsContext _context;
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _dataverseContext;
        private readonly IMapper _mapper;
        public DuplicateCheckEngine(
            IDynamicsContextFactory context,
            IDataverseContextFactory dataverseContext,
            IMapper mapper)
        {
            _context = context.Create();
            _dataverseContext = dataverseContext.CreateReadOnly();
            _mapper = mapper;
        }
        public async Task<DuplicateCheckResponse> DuplicateCheckAsync(DuplicateCheckRequest qry, CancellationToken ct)
        {
            return qry switch
            {
                BulkUploadAppDuplicateCheckRequest q => await BulkUploadAppDuplicateCheckAsync(q, ct),
                AppInviteDuplicateCheckRequest q => await AppInviteDuplicateCheckAsync(q, ct),
                _ => throw new NotSupportedException($"{qry.GetType().Name} is not supported")
            };
        }
    }
}
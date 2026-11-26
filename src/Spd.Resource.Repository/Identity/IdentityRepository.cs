using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Resource.Repository.Registration;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.Identity
{
    internal class IdentityRepository : IIdentityRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _dynaContext;
        private readonly IMapper _mapper;

        public IdentityRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper mapper, ILogger<IdentityRepository> logger)
        {
            _dynaContext = ctx;
            _mapper = mapper;
        }

        public async Task<IdentityQueryResult> Query(IdentityQry query, CancellationToken ct)
        {
            return query switch
            {
                IdentityQry q => await HandleIdentityQry(q, ct),
                _ => throw new NotSupportedException($"{query.GetType().Name} is not supported")
            };
        }

        public async Task<IdentityCmdResult?> Manage(IdentityCmd cmd, CancellationToken ct)
        {
            return cmd switch
            {
                CreateIdentityCmd c => await HandleCreateIdentityCmd(c, ct),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        private async Task<IdentityQueryResult> HandleIdentityQry(IdentityQry qry, CancellationToken ct)
        {
            IQueryable<Spd_Identity> identities = _dynaContext.Spd_IdentitySet;

            if (qry.IdentityProviderType != null)
            {
                identities = identities.Where(i => i.Spd_Type == Enum.Parse<Spd_IdentityProvider>(qry.IdentityProviderType.ToString()));
            }

            if (!qry.includeInactive) identities = identities.Where(i => i.StateCode == Spd_Identity_StateCode.Active);
            if (qry.UserGuid != null) identities = identities.Where(i => i.Spd_UserGuid == qry.UserGuid);
            if (qry.OrgGuid != null) identities = identities.Where(i => i.Spd_OrgGuid == qry.OrgGuid.ToString());


            return new IdentityQueryResult(_mapper.Map<IEnumerable<Identity>>(identities.ToList(), opts => opts.Items["DataverseContext"] = _dynaContext));
        }

        private async Task<IdentityCmdResult> HandleCreateIdentityCmd(CreateIdentityCmd cmd, CancellationToken ct)
        {
            Spd_Identity identity = _mapper.Map<Spd_Identity>(cmd);
            _dynaContext.AddObject(identity);
            _dynaContext.SaveChanges();

            return _mapper.Map<IdentityCmdResult>(identity);
        }
    }
}

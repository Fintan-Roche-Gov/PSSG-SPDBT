using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Config
{
    internal class ConfigRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx, IMapper _mapper) : IConfigRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _dynaContext = ctx;

        public async Task<ConfigResult> Query(ConfigQuery query, CancellationToken ct)
        {
            var bcgov_configs = _dynaContext.Bcgov_ConfigSet.Where(c => c.StateCode != Bcgov_Config_StateCode.Inactive);

            if (query.Key != null) bcgov_configs = bcgov_configs.Where(c => c.Bcgov_Key == query.Key);
            if (query.Group != null) bcgov_configs = bcgov_configs.Where(c => c.Bcgov_Group == query.Group);

            return new ConfigResult(_mapper.Map<IEnumerable<ConfigItem>>(bcgov_configs.ToList()));
        }
    }
}
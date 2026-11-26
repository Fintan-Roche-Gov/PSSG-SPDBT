using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse.EarlyBound;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Alias;
internal class AliasRepository : IAliasRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AliasRepository> _logger;

    public AliasRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
        IMapper mapper,
        ILogger<AliasRepository> logger)
    {
        _context = ctx;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid?> CreateAliasAsync(CreateAliasCommand cmd, CancellationToken ct)
    {
        Spd.Utilities.Dataverse.EarlyBound.Contact? contact = _context.ContactSet.Where(c => c.ContactId == cmd.ContactId).FirstOrDefault();
        if (contact == null)
        {
            _logger.LogError("Contact for alias creation was not found");
            throw new ArgumentException("cannot find contact to be linked");
        }

        Spd_Alias alias = _mapper.Map<Spd_Alias>(cmd.Alias);
        CreateAlias(contact, alias);
        _context.SaveChanges();
        return await Task.FromResult(alias.Spd_AliasId);
    }

    public async Task DeleteAliasAsync(List<Guid?> aliasIds, CancellationToken ct)
    {
        foreach (var aliasId in aliasIds)
        {
            Spd_Alias? alias = _context.Spd_AliasSet.Where(a =>
                a.Spd_AliasId == aliasId &&
                a.StateCode == Spd_Alias_StateCode.Active &&
                a.Spd_Source == Spd_Alias_Spd_Source.UserEntered
            ).FirstOrDefault();

            if (alias == null)
            {
                _logger.LogError("Alias to be deleted was not found");
                throw new ArgumentException("cannot find alias to be deleted");
            }

            alias.StateCode = Spd_Alias_StateCode.Inactive;
            alias.StatusCode = Spd_Alias_StatusCode.Inactive;
            _context.UpdateObject(alias);
        }

        _context.SaveChanges();
        await Task.CompletedTask;
    }

    public async Task UpdateAliasAsync(UpdateAliasCommand cmd, CancellationToken ct)
    {
        foreach (AliasResp alias in cmd.Aliases)
        {
            Spd_Alias? existingAlias = _context.Spd_AliasSet.Where(a =>
                a.Spd_AliasId == alias.Id &&
                a.StateCode == Spd_Alias_StateCode.Active &&
                a.Spd_Source == Spd_Alias_Spd_Source.UserEntered
            ).FirstOrDefault();

            if (existingAlias == null)
            {
                _logger.LogError("Alias to be updated was not found");
                throw new ArgumentException("cannot find alias to be updated");
            }

            existingAlias.Spd_FirstName = alias.GivenName;
            existingAlias.Spd_Surname = alias.Surname;
            existingAlias.Spd_MiddleName1 = alias.MiddleName1;
            existingAlias.Spd_MiddleName2 = alias.MiddleName2;
            _context.UpdateObject(existingAlias);
        }

        _context.SaveChanges();
        await Task.CompletedTask;
    }

    private void CreateAlias(Spd.Utilities.Dataverse.EarlyBound.Contact contact,
        Spd_Alias alias)
    {
        if (AliasExists(alias, contact) == null)
        {
            alias.Spd_ContactId = contact.ToEntityReference();
            _context.AddObject(alias);
        }
    }

    private Spd_Alias? AliasExists(Spd_Alias newAlias, Spd.Utilities.Dataverse.EarlyBound.Contact contact)
    {
        var matchingAlias = _context.Spd_AliasSet.Where(o =>
           o.Spd_FirstName == newAlias.Spd_FirstName &&
           o.Spd_MiddleName1 == newAlias.Spd_MiddleName1 &&
           o.Spd_MiddleName2 == newAlias.Spd_MiddleName2 &&
           o.Spd_Surname == newAlias.Spd_Surname &&
           o.StateCode != Spd_Alias_StateCode.Inactive &&
           o.Spd_ContactId != null && o.Spd_ContactId.Id == contact.ContactId &&
           o.Spd_Source == Spd_Alias_Spd_Source.UserEntered
       ).FirstOrDefault();

        return matchingAlias;
    }
}

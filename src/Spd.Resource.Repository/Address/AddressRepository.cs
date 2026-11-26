using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Resource.Repository.Biz;
using Spd.Utilities.Dataverse.EarlyBound;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Address;
internal class AddressRepository : IAddressRepository
{
    private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<BizRepository> _logger;

    public AddressRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
            IMapper mapper,
            ILogger<BizRepository> logger)
    {
        _context = ctx;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AddressResp>> QueryAsync(AddressQry qry, CancellationToken ct)
    {
        IQueryable<Spd_Address> addresses = _context.Spd_AddressSet;

        if (qry.OrganizationId == null && qry.Type == null)
            throw new ArgumentException("at least need 1 parameter to do address query.");

        if (!qry.IncludeInactive)
            addresses = addresses.Where(a => a.StateCode != Spd_Address_StateCode.Inactive);

        if (qry.OrganizationId != null)
            addresses = addresses.Where(a => a.Spd_Organization != null && a.Spd_Organization.Id == qry.OrganizationId);

        if (qry.Type != null)
            addresses = addresses.Where(a => a.Spd_Type == Enum.Parse<Spd_AddressType>(qry.Type.ToString()));

        return await Task.FromResult(_mapper.Map<IEnumerable<AddressResp>>(addresses.ToList()));
    }

    public async Task<IEnumerable<AddressResp>> CreateAddressesAsync(UpsertAddressCmd cmd, CancellationToken ct)
    {
        List<AddressResp> createdAddresses = new();

        foreach (BranchAddr address in cmd.Addresses)
        {
            Spd_Address addressToCreate = _mapper.Map<Spd_Address>(address);
            Account? biz = _context.AccountSet.Where(a => a.AccountId == cmd.BizId && a.StateCode != Account_StateCode.Inactive).FirstOrDefault();

            if (biz == null)
            {
                _logger.LogError("Biz to be linked to address was not found");
                throw new ArgumentException("cannot find biz to be linked");
            }

            CreateAddress(biz, addressToCreate);
            _context.SaveChanges();
            AddressResp createdAddress = _mapper.Map<AddressResp>(addressToCreate);
            createdAddresses.Add(createdAddress);
        }

        return await Task.FromResult(createdAddresses.AsEnumerable());
    }

    public async Task DeleteAddressesAsync(List<Guid?> addressIds, CancellationToken ct)
    {
        foreach (var addressId in addressIds)
        {
            Spd_Address? address = _context.Spd_AddressSet.Where(a =>
                a.Spd_AddressId == addressId &&
                a.StateCode == Spd_Address_StateCode.Active
            ).FirstOrDefault();

            if (address == null)
            {
                _logger.LogError("Address to be deleted was not found");
                throw new ArgumentException("cannot find address to be deleted");
            }

            address.StateCode = Spd_Address_StateCode.Inactive;
            address.StatusCode = Spd_Address_StatusCode.Inactive;
            _context.UpdateObject(address);
        }

        _context.SaveChanges();
        await Task.CompletedTask;
    }

    public async Task UpdateAddressesAsync(UpsertAddressCmd cmd, CancellationToken ct)
    {
        foreach (BranchAddr address in cmd.Addresses)
        {
            Spd_Address? existingAddress = _context.Spd_AddressSet.Where(a =>
                a.Spd_AddressId == address.BranchId &&
                a.StateCode == Spd_Address_StateCode.Active
            ).FirstOrDefault();

            if (existingAddress == null)
            {
                _logger.LogError("Address to be updated was not found");
                throw new ArgumentException("cannot find address to be updated");
            }

            _mapper.Map(address, existingAddress);
            _context.UpdateObject(existingAddress);
        }

        _context.SaveChanges();
        await Task.CompletedTask;
    }

    private void CreateAddress(Account account, Spd_Address address)
    {
        address.Spd_Organization = account.ToEntityReference();
        _context.AddObject(address);
    }
}

using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Exceptions;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext; 
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using SystemTask = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Contact;
internal class ContactRepository : IContactRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ContactRepository> _logger;

    public ContactRepository(DataverseContext ctx,
        IMapper mapper,
        ILogger<ContactRepository> logger)
    {
        _context = ctx;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<ContactResp> GetAsync(Guid contactId, CancellationToken ct)
    {
        ContactEntity? contact = _context.ContactSet
            .Where(c => c.ContactId == contactId)
            .FirstOrDefault();
        if (contact == null) throw new ArgumentException($"cannot find the contact with contactId : {contactId}");
        _context.LoadProperty(contact, nameof(contact.Spd_Contact_Alias)); 
        return SystemTask.FromResult(_mapper.Map<ContactResp>(contact));
    }

    public Task<ContactListResp> QueryAsync(ContactQry qry, CancellationToken ct)
    {
        IQueryable<ContactEntity> contacts = _context.ContactSet;

        if (!qry.IncludeInactive)
            contacts = contacts.Where(d => d.StateCode != Contact_StateCode.Inactive);
        if (qry.UserEmail != null)
        {
            if (string.IsNullOrEmpty(qry.UserEmail))
                contacts = contacts.Where(d => d.EmailAddress1 == null || d.EmailAddress1 == string.Empty);
            else
                contacts = contacts.Where(d => d.EmailAddress1 == qry.UserEmail);
        }
        if (qry.FirstName != null)
        {
            if (string.IsNullOrEmpty(qry.FirstName))
                contacts = contacts.Where(d => d.FirstName == null || d.FirstName == string.Empty);
            else
                contacts = contacts.Where(d => d.FirstName == qry.FirstName);
        }
        if (qry.LastName != null)
        {
            contacts = contacts.Where(d => d.LastName == qry.LastName);
        }
        if (qry.MiddleName1 != null)
        {
            if (string.IsNullOrEmpty(qry.MiddleName1))
                contacts = contacts.Where(d => d.Spd_MiddleName1 == null || d.Spd_MiddleName1 == string.Empty);
            else
                contacts = contacts.Where(d => d.Spd_MiddleName1 == qry.MiddleName1);
        }
        if (qry.MiddleName2 != null)
        {
            if (string.IsNullOrEmpty(qry.MiddleName2))
                contacts = contacts.Where(d => d.Spd_MiddleName2 == null || d.Spd_MiddleName2 == string.Empty);
            else
                contacts = contacts.Where(d => d.Spd_MiddleName2 == qry.MiddleName2);
        }
        if (qry.BirthDate != null)
        {
            DateTime birthdate = new(qry.BirthDate.Value.Year, qry.BirthDate.Value.Month, qry.BirthDate.Value.Day, 0, 0, 0, DateTimeKind.Utc);
            contacts = contacts.Where(d => d.Birthdate == birthdate);
        }
        if (qry.IdentityId != null)
        {
            if (qry.IdentityId == Guid.Empty)
                contacts = contacts.Where(d => !d.Spd_Contact_Spd_Identity.Any());
        }
        List<ContactEntity> contactList = contacts.ToList();
        foreach (var contact in contactList)
        {
            _context.LoadProperty(contact, nameof(contact.Spd_Contact_Alias));
        }
        var result = new ContactListResp
        {
            Items = _mapper.Map<IEnumerable<ContactResp>>(contactList)
        };
        if (qry.ReturnLicenceInfo)
        {
            foreach (ContactResp resp in result.Items)
            {
                resp.LicenceInfos = _mapper.Map<IEnumerable<LicenceInfo>>(_context.Spd_LicenceSet.Where(l => l.Spd_LicenceHolder != null && l.Spd_LicenceHolder.Id == resp.Id).ToList());
            }
        }
        return SystemTask.FromResult(result);
    }

    public async Task<ContactResp> ManageAsync(ContactCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            UpdateContactCmd c => await UpdateContactAsync(c, ct),
            CreateContactCmd c => await CreateContactAsync(c, ct),
            TermAgreementCmd c => await TermAgreeAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    public Task<bool> MergeContactsAsync(MergeContactsCmd cmd, CancellationToken ct)
    {
        ContactEntity? oldContact = _context.ContactSet.Where(c => c.ContactId == cmd.OldContactId).SingleOrDefault();
        ContactEntity? newContact = _context.ContactSet.Where(c => c.ContactId == cmd.NewContactId).SingleOrDefault();
        if (oldContact == null || newContact == null)
        {
            _logger.LogError($"Merge contacts cannot find at least one of the contact");
            throw new ArgumentException("cannot find contact for merging");
        }
        return SystemTask.FromResult(true);
    }

    private async Task<ContactResp> UpdateContactAsync(UpdateContactCmd c, CancellationToken ct)
    {
        ContactEntity newContact = _mapper.Map<ContactEntity>(c);
        ContactResp resp = new();
        ContactEntity? existingContact = _context.ContactSet.Where(x => x.ContactId == c.Id).SingleOrDefault();
        List<Spd_Alias> newAliasesToAdd = (List<Spd_Alias>)_mapper.Map<IEnumerable<Spd_Alias>>(c.Aliases.Where(a => a.Id == null || a.Id == Guid.Empty)); // Only aliases with Id null or empty are considered as new
        existingContact = await _context.UpdateContact(existingContact, newContact, null, newAliasesToAdd, ct);
        _context.SaveChanges();
        return _mapper.Map<ContactEntity, ContactResp>(existingContact, resp);
    }

    private async Task<ContactResp> CreateContactAsync(CreateContactCmd c, CancellationToken ct)
    {
        ContactEntity contact = _mapper.Map<ContactEntity>(c);
        contact.Spd_LastLoggedInScreeningPortal = null;
        contact.Spd_LastLoggedInLicensingPortal = null;
        Spd_Identity? identity = null;
        if (c.IdentityId != null)
        {
            identity = _context.Spd_IdentitySet.Where(i => i.Spd_IdentityId == (Guid)c.IdentityId).SingleOrDefault();
            if (identity == null)
            {
                _logger.LogError($"no valid identity for {c.IdentityId}");
                throw new ApiException(System.Net.HttpStatusCode.BadRequest, "The identity cannot be found.");
            }
        }
        //two saveChanges because "Associate of 1:N navigation property with Create of Update is not supported in CRM"
        contact = await _context.CreateContact(contact, identity, _mapper.Map<IEnumerable<Spd_Alias>>(c.Aliases), ct);
        _context.SaveChanges();
        ContactResp resp = _mapper.Map<ContactResp>(contact);
        return resp;
    }

    private async Task<ContactResp> TermAgreeAsync(TermAgreementCmd c, CancellationToken ct)
    {
        ContactResp resp = new();
        ContactEntity? existingContact = _context.ContactSet.Where(x => x.ContactId == c.Id).SingleOrDefault();
        if (existingContact == null)
        {
            _logger.LogError($"no valid contact for {c.Id}");
            throw new ApiException(System.Net.HttpStatusCode.BadRequest, "The contact cannot be found.");
        }
        existingContact.Spd_LastLoggedInLicensingPortal = DateTime.UtcNow;
        _context.UpdateObject(existingContact);
        _context.SaveChanges();
        return _mapper.Map<ContactEntity, ContactResp>(existingContact, resp);
    }
}



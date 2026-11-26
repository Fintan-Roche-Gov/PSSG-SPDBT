using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Resource.Repository.Alias;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;
using ContactDataver = Spd.Utilities.Dataverse.EarlyBound.Contact;
namespace Spd.Resource.Repository.ControllingMemberCrcApplication;
public class ControllingMemberCrcRepository : IControllingMemberCrcRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;

    public ControllingMemberCrcRepository(DataverseContext ctx, IMapper mapper)
    {
        _context = ctx;
        _mapper = mapper;
    }

    #region anonymous new
    public async Task<ControllingMemberCrcApplicationCmdResp> CreateControllingMemberCrcApplicationAsync(SaveControllingMemberCrcAppCmd cmd, CancellationToken ct)
    {
        // get parent business license application
        Spd_Application? bizLicApplication = _context.Spd_ApplicationSet
                .Where(a => a.Spd_ApplicationId == cmd.ParentBizLicApplicationId)
                .SingleOrDefault();

        if (bizLicApplication == null)
            throw new ArgumentException("Parent business licence application was not found.");

        Spd_BusinessContact? bizContact = _context.Spd_BusinessContactSet.Where(x => x.Spd_BusinessContactId == cmd.BizContactId).FirstOrDefault();
        if (bizContact == null) throw new ArgumentException("Business contact was not found.");
        ContactDataver? contact = null;
        if (bizContact.Spd_ContactId == null)
        {
            //check contact duplicate
            contact = SharedRepositoryFuncs.GetDuplicateContact(_context, _mapper.Map<ContactDataver>(cmd), ct);
            //create or update contact
            contact = contact == null ?
                await _context.CreateContact(_mapper.Map<ContactDataver>(cmd), null, _mapper.Map<IEnumerable<Spd_Alias>>(cmd.Aliases), ct) :
                await UpdatePersonalInformationAsync(cmd, contact, ct);
            //link bizContact with contact
            bizContact.Spd_ContactId = contact?.ToEntityReference();
            _context.UpdateObject(bizContact);
        }
        else
        {
            contact = _context.ContactSet.Where(c => c.ContactId == bizContact.Spd_ContactId.Id).FirstOrDefault();
            await UpdatePersonalInformationAsync(cmd, contact, ct);
        }

        Spd_Application? app = _mapper.Map<Spd_Application>(cmd);
        _context.AddObject(app);

        //set applicant lookup
        app.Spd_ApplicantId = contact?.ToEntityReference();

        //link to biz
        Account? account = _context.AccountSet
            .Where(a => a.AccountId == bizLicApplication.Spd_ApplicantId.Id)
            .Where(a => a.StateCode == Account_StateCode.Active)
            .FirstOrDefault();
        if (account != null)
        {
            //set organization lookup
            app.Spd_OrganizationId = account.ToEntityReference();
        }
        // add link to parent business application
        _context.AddLink(bizLicApplication, new Relationship(Spd_Application.Fields.ReferencedSpd_BusinessApplication_Spd_WorkerApplication), app);

        SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
        SharedRepositoryFuncs.LinkTeam(_context, global::Spd.Utilities.Dataverse.DataverseConstants.Licensing_Client_Service_Team_Guid, app);

        //link to bizContact
        _context.AddLink(bizContact, new Relationship(Spd_BusinessContact.Fields.Spd_BusinessContact_Spd_Application), app);
        _context.SaveChanges();
        return new ControllingMemberCrcApplicationCmdResp((Guid)app.Spd_ApplicationId, contact?.ContactId);
    }
    #endregion
    public async Task<ControllingMemberCrcApplicationResp> GetCrcApplicationAsync(Guid controllingMemberApplicationId, CancellationToken ct)
    {
        Spd_Application? app = _context.Spd_ApplicationSet
            .Where(a => a.Spd_ApplicationId == controllingMemberApplicationId)
            .SingleOrDefault();

        if (app == null)
            throw new ArgumentException("invalid app id");

        _context.LoadProperty(app, nameof(app.Spd_ServiceTypeId));
        _context.LoadProperty(app, nameof(app.Spd_ApplicantId));

        ControllingMemberCrcApplicationResp appResp = _mapper.Map<ControllingMemberCrcApplicationResp>(app);

        if (app.Spd_ApplicantId?.Id != null)
        {
            var aliases = SharedRepositoryFuncs.GetAliases((Guid)app.Spd_ApplicantId.Id, _context);
            appResp.Aliases = _mapper.Map<AliasResp[]>(aliases);
        }

        return appResp;
    }

    public async Task<ControllingMemberCrcApplicationCmdResp> SaveControllingMemberCrcApplicationAsync(SaveControllingMemberCrcAppCmd cmd, CancellationToken ct)
    {
        // get parent business license application
        Spd_Application? bizLicApplication = _context.Spd_ApplicationSet
                .Where(a => a.Spd_ApplicationId == cmd.ParentBizLicApplicationId)
                .SingleOrDefault();

        if (bizLicApplication == null)
            throw new ArgumentException("Parent business licence application was not found.");

        Spd_BusinessContact? bizContact = _context.Spd_BusinessContactSet.Where(x => x.Spd_BusinessContactId == cmd.BizContactId).FirstOrDefault();
        ContactDataver? contact = _context.ContactSet
            .Where(c => c.ContactId == cmd.ContactId).SingleOrDefault();

        if (contact != null)
        {
            _context.LoadProperty(contact, nameof(contact.Spd_Contact_Alias));
        }

        if (contact == null)
        {
            throw new ArgumentException("applicant not found");
        }

        Spd_Application? app;
        if (cmd.ControllingMemberAppId != null)
        {
            app = _context.Spd_ApplicationSet
                .Where(a => a.Spd_ApplicationId == cmd.ControllingMemberAppId).FirstOrDefault();
            if (app == null)
                throw new ArgumentException("invalid app id");

            _context.LoadProperty(app, nameof(app.Spd_Application_Spd_LicenceCategory));
            _mapper.Map<SaveControllingMemberCrcAppCmd, Spd_Application>(cmd, app);
            app.Spd_ApplicationId = (Guid)(cmd.ControllingMemberAppId);
            _context.UpdateObject(app);
        }
        else
        {
            app = _mapper.Map<Spd_Application>(cmd);
            _context.AddObject(app);

            //set applicant lookup
            app.Spd_ApplicantId = contact.ToEntityReference();

            //link bizContact with contact
            bizContact.Spd_ContactId = contact.ToEntityReference();
            _context.UpdateObject(bizContact);

            //link to biz
            Account? account = _context.AccountSet
                .Where(a => a.AccountId == bizLicApplication.Spd_ApplicantId.Id)
                .Where(a => a.StateCode == Account_StateCode.Active)
                .FirstOrDefault();
            if (account != null)
            {
                //set organization lookup
                app.Spd_OrganizationId = account.ToEntityReference();
            }
            // add link to parent business application
            _context.AddLink(
                bizLicApplication,
                new Relationship(Spd_Application.Fields.ReferencedSpd_BusinessApplication_Spd_WorkerApplication),
                app);
            SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
            SharedRepositoryFuncs.LinkTeam(_context, global::Spd.Utilities.Dataverse.DataverseConstants.Licensing_Client_Service_Team_Guid, app);

            //link to bizContact
            _context.AddLink(bizContact, new Relationship(Spd_BusinessContact.Fields.Spd_BusinessContact_Spd_Application), app);
        }

        _context.SaveChanges();
        return new ControllingMemberCrcApplicationCmdResp((Guid)app.Spd_ApplicationId, cmd.ContactId);
    }

    //update contact, aliases and addreses, support partial save (to not add spd_address and spd_alias histroy record)
    private async Task<ContactDataver?> UpdatePersonalInformationAsync(SaveControllingMemberCrcAppCmd cmd, ContactDataver? contact, CancellationToken ct)
    {
        List<AliasResp> aliases = _mapper.Map<List<AliasResp>>(contact?.Spd_Contact_Alias.Where(a => a.StateCode == Spd_Alias_StateCode.Active &&
                a.Spd_Source == Spd_Alias_Spd_Source.UserEntered));
        ContactDataver newContact = _mapper.Map<ContactDataver>(cmd);
        List<Spd_Alias> aliasesToAdd = (List<Spd_Alias>)_mapper.Map<IEnumerable<Spd_Alias>>(cmd.Aliases.Where(a => a.Id == null || a.Id == Guid.Empty)); // Only aliases with Id null or empty are considered as new
        var modifiedAliases = cmd.Aliases.Where(a => a.Id != Guid.Empty && a.Id != null).ToList();
        List<Guid?> aliasesToRemove = aliases.Where(a => modifiedAliases.All(ap => ap.Id != a.Id)).Select(a => a.Id).ToList();

        //for spdbt-3706, do not update contact mental health condition from application when the value from app is No.
        if (newContact.Spd_MentalHealthCondition == Spd_YesNo.No)
            newContact.Spd_MentalHealthCondition = null;

        contact = await _context.UpdateContact(contact, newContact, null, aliasesToAdd, ct, cmd.IsPartialSaving);
        foreach (var aliasId in aliasesToRemove)
        {
            Spd_Alias? alias = _context.Spd_AliasSet.Where(a =>
                a.Spd_AliasId == aliasId &&
                a.StateCode == Spd_Alias_StateCode.Active &&
                a.Spd_Source == Spd_Alias_Spd_Source.UserEntered
            ).FirstOrDefault();

            if (alias != null)
            {
                alias.StateCode = Spd_Alias_StateCode.Inactive;
                alias.StatusCode = Spd_Alias_StatusCode.Inactive;
                _context.UpdateObject(alias);
            }
        }
        foreach (AliasResp alias in modifiedAliases)
        {
            Spd_Alias? existingAlias = _context.Spd_AliasSet.Where(a =>
                a.Spd_AliasId == alias.Id &&
                a.StateCode == Spd_Alias_StateCode.Active &&
                a.Spd_Source == Spd_Alias_Spd_Source.UserEntered
            ).FirstOrDefault();

            if (existingAlias != null)
            {
                existingAlias.Spd_FirstName = alias.GivenName;
                existingAlias.Spd_Surname = alias.Surname;
                existingAlias.Spd_MiddleName1 = alias.MiddleName1;
                existingAlias.Spd_MiddleName2 = alias.MiddleName2;
                _context.UpdateObject(existingAlias);
            }
        }
        _context.SaveChanges();
        return contact;
    }
}

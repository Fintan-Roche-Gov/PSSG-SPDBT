using Spd.Resource.Repository.PersonLicApplication;
using Microsoft.Dynamics.CRM.Microsoft.Dynamics.CRM;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository;
internal static class SharedRepositoryFuncs
{
    public static void ProcessCategories(DataverseContext _context, IEnumerable<WorkerCategoryTypeEnum> categories, Spd_Application app)
    {
        foreach (var c in categories)
        {
            global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LicenceCategoryDictionary.TryGetValue(c.ToString(), out Guid licenceCategoryId);
            var cat = _context.Spd_LicenceCategorySet
                .Where(s => s.Spd_LicenceCategoryId == licenceCategoryId)
                .Where(s => s.StateCode != Spd_LicenceCategory_StateCode.Inactive)
                .FirstOrDefault();
            if (cat == null)
                throw new ArgumentException($"licence category not found for {c.ToString()}");

            if (!app.Spd_Application_Spd_LicenceCategory.Any(c => c.Spd_LicenceCategoryId == cat.Spd_LicenceCategoryId))
            {
                _context.AddLink(app, new Relationship(Spd_Application.Fields.Spd_Application_Spd_LicenceCategory), cat);
            }
        }
        foreach (var appCategory in app.Spd_Application_Spd_LicenceCategory)
        {
            var code = global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LookupLicenceCategoryKey(appCategory.Spd_LicenceCategoryId);
            //if categories do not contain cat
            if (!categories.Any(c => c.ToString() == code))
            {
                _context.DeleteLink(app, new Relationship(Spd_Application.Fields.Spd_Application_Spd_LicenceCategory), appCategory);
            }
        }
    }
    //public static void ProcessCategories(ODataDataverseContext _context, IEnumerable<WorkerCategoryTypeEnum> categories, spd_application app)
    //{
    //    foreach (var c in categories)
    //    {
    //        global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LicenceCategoryDictionary.TryGetValue(c.ToString(), out Guid licenceCategoryId);
    //        var cat = _context.spd_licencecategories
    //            .Where(s => s.spd_licencecategoryid == licenceCategoryId)
    //            .Where(s => s.statecode != global::Spd.Utilities.Dynamics.DataverseConstants.StateCode_Inactive)
    //            .FirstOrDefault();
    //        if (cat == null)
    //            throw new ArgumentException($"licence category not found for {c.ToString()}");

    //        if (!app.spd_application_spd_licencecategory.Any(x => x.spd_licencecategoryid == cat.spd_licencecategoryid))
    //        {
    //            _context.AddLink(app, nameof(app.spd_application_spd_licencecategory), cat);
    //        }
    //    }
    //    foreach (var appCategory in app.spd_application_spd_licencecategory)
    //    {
    //        var code = global::Spd.Utilities.Dynamics.DynamicsContextLookupHelpers.LookupLicenceCategoryKey(appCategory.spd_licencecategoryid);
    //        //if categories do not contain cat
    //        if (!categories.Any(c => c.ToString() == code))
    //        {
    //            _context.DeleteLink(app, nameof(app.spd_application_spd_licencecategory), appCategory);
    //        }
    //    }
    //}
    public static void LinkTeam(DataverseContext _context, string teamGuidStr, Spd_Application app)
    {
        Guid teamGuid = Guid.Parse(teamGuidStr);
        Team? serviceTeam = _context.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
        app.OwnerId = serviceTeam?.ToEntityReference();
    }

    public static void LinkTrainingEventTeam(DataverseContext _context, string teamGuidStr, Spd_DogTrainingSchool trainingEvent)
    {
        Guid teamGuid = Guid.Parse(teamGuidStr);
        Team? serviceTeam = _context.TeamSet.Where(t => t.TeamId == teamGuid).FirstOrDefault();
        trainingEvent.OwnerId = serviceTeam?.ToEntityReference();
    }

    public static void LinkTrainingEventTeam(DataverseContext _context, Team team, Spd_DogTrainingSchool trainingEvent)
    {
        trainingEvent.OwnerId = team.ToEntityReference();
    }

    //public static void LinkTrainingEventTeam(ODataDataverseContext _context, string teamGuidStr, spd_dogtrainingschool trainingEvent)
    //{
    //    Guid teamGuid = Guid.Parse(teamGuidStr);
    //    team? serviceTeam = _context.teams.Where(t => t.teamid == teamGuid).FirstOrDefault();
    //    if (serviceTeam != null)
    //    {
    //        _context.SetLink(trainingEvent, nameof(trainingEvent.ownerid), serviceTeam);
    //    }
    //}

    //public static void LinkTrainingEventTeam(ODataDataverseContext _context, team team, spd_dogtrainingschool trainingEvent)
    //{
    //    _context.SetLink(trainingEvent, nameof(trainingEvent.ownerid), team);
    //}
    public static void LinkDog(DataverseContext _context, Guid? dogId, Spd_Application app)
    {
        if (dogId == null) return;
        Spd_Dog? dog = _context.Spd_DogSet.Where(t => t.Spd_DogId == dogId).FirstOrDefault();
        app.Spd_DogId = dog?.ToEntityReference();
    }

    //public static void LinkTeam(ODataDataverseContext _context, string teamGuidStr, spd_application app)
    //{
    //    Guid teamGuid = Guid.Parse(teamGuidStr);
    //    team? serviceTeam = _context.teams.Where(t => t.teamid == teamGuid).FirstOrDefault();
    //    if (serviceTeam != null)
    //    {
    //        _context.SetLink(app, nameof(app.ownerid), serviceTeam);
    //    }
    //}

    //public static void LinkDog(ODataDataverseContext _context, Guid? dogId, spd_application app)
    //{
    //    if (dogId == null) return;
    //    spd_dog? dog = _context.spd_dogs.Where(t => t.spd_dogid == dogId).FirstOrDefault();
    //    if (dog != null)
    //    {
    //        _context.SetLink(app, nameof(app.spd_DogId), dog);
    //    }
    //}

    //public static void LinkServiceType(ODataDataverseContext _context, ServiceTypeEnum? licenceType, spd_application app)
    //{
    //    if (licenceType == null) throw new ArgumentException("invalid LicenceApplication type");
    //    Guid? serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(licenceType.ToString());
    //    if (serviceTypeGuid == null) return;
    //    spd_servicetype? servicetype = _context.spd_servicetypes
    //        .Where(s => s.spd_servicetypeid == serviceTypeGuid)
    //        .FirstOrDefault();
    //    if (servicetype != null)
    //    {
    //        _context.SetLink(app, nameof(app.spd_ServiceTypeId), servicetype);
    //    }
    //}

    //public static void LinkLicence(ODataDataverseContext _context, Guid? licenceId, spd_application app)
    //{
    //    if (licenceId == null) return;
    //    spd_licence? licence = _context.spd_licences.Where(l => l.spd_licenceid == licenceId).FirstOrDefault();
    //    if (licence != null)
    //    {
    //        _context.SetLink(app, nameof(app.spd_CurrentExpiredLicenceId), licence);
    //    }
    //}

    public static void LinkServiceType(DataverseContext _context, ServiceTypeEnum? licenceType, Spd_Application app)
    {
        if (licenceType == null) throw new ArgumentException("invalid LicenceApplication type");
        Guid? serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(licenceType.ToString());
        if (serviceTypeGuid == null) return;
        Spd_ServiceType? servicetype = _context.Spd_ServiceTypeSet
            .Where(s => s.Spd_ServiceTypeId == serviceTypeGuid)
            .Where(s => s.StateCode != Spd_ServiceType_StateCode.Inactive)
            .FirstOrDefault();
        if (servicetype != null)
        {
            app.Spd_ServiceTypeId = servicetype.ToEntityReference();
        }
    }

    public static void LinkLicence(DataverseContext _context, Guid? licenceId, Spd_Application app)
    {
        if (licenceId == null) return;
        var licence = _context.Spd_LicenceSet.Where(l => l.Spd_LicenceId == licenceId).FirstOrDefault();
        if (licence != null)
        {
            app.Spd_CurrentExpiredLicenceId = licence.ToEntityReference();
        }
    }

    public static void LinkSubmittedByPortalUser(DataverseContext _context, Guid? portalUserId, Spd_Application app)
    {
        if (portalUserId == null) return;
        var portaluser = _context.Spd_PortalUserSet.Where(l => l.Spd_PortalUserId == portalUserId).FirstOrDefault();
        if (portaluser != null)
        {
            app.Spd_SubmittedBy = portaluser.ToEntityReference();
        }
    }
    public static List<Spd_Alias>? GetAliases(Guid contactId, DataverseContext _context)
    {
        var matchingAliases = _context.Spd_AliasSet.Where(o =>
           o.Spd_ContactId != null && o.Spd_ContactId.Id == contactId &&
           o.StateCode != Spd_Alias_StateCode.Inactive &&
           o.Spd_Source == Spd_Alias_Spd_Source.UserEntered
       ).ToList();
        return matchingAliases;
    }
    //public static List<spd_alias>? GetAliases(Guid contactId, ODataDataverseContext _context)
    //{
    //    var matchingAliases = _context.spd_aliases.Where(o =>
    //       o._spd_contactid_value != null && o._spd_contactid_value == contactId &&
    //       o.statecode != global::Spd.Utilities.Dynamics.DataverseConstants.StateCode_Inactive &&
    //       o.spd_source == (int)Spd_Alias_Spd_Source.UserEntered
    //   ).ToList();
    //    return matchingAliases;
    //}
    public static ContactEntity? GetDuplicateContact(DataverseContext context, ContactEntity contact, CancellationToken ct)
    {
        var query = context.ContactSet.Where(x => x.Birthdate == contact.Birthdate);
        if (!string.IsNullOrEmpty(contact.Spd_BcDriversLicense))
            query = query.Where(a => a.Spd_BcDriversLicense == contact.Spd_BcDriversLicense);

        return query.ToList().Where(a => string.Equals(a.FirstName, contact.FirstName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.LastName, contact.LastName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    }
    public static ContactEntity? GetLicenceHolderContact(DataverseContext context, Guid expiredLicenceId)
    {
        var licence = context.Spd_LicenceSet
            .Where(l => l.Spd_LicenceId == expiredLicenceId)
            .FirstOrDefault();
        if (licence?.Spd_LicenceHolder == null) return null;

        return context.ContactSet
            .Where(c => c.ContactId == licence.Spd_LicenceHolder.Id)
            .FirstOrDefault();
    }
}
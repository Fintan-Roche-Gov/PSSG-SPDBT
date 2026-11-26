using AutoMapper;
using Microsoft.Dynamics.CRM.Microsoft.Dynamics.CRM;
using Microsoft.Xrm.Sdk;
using Spd.Resource.Repository.DogBase;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using CrmApplication = Microsoft.Dynamics.CRM.Microsoft.Dynamics.CRM.spd_application;
using CrmContact = Spd.Utilities.Dataverse.EarlyBound.Contact;
using CrmDogTrainingSchool = Microsoft.Dynamics.CRM.Microsoft.Dynamics.CRM.spd_dogtrainingschool;

namespace Spd.Resource.Repository.DogTrainerApp;
internal class DogTrainerAppRepository : DogAppBaseRepository, IDogTrainerAppRepository
{
    public DogTrainerAppRepository(IDataverseContextFactory ctx, IMapper mapper) : base(ctx, mapper)
    { }

    public async Task<DogTrainerAppCmdResp> CreateDogTrainerAppAsync(CreateDogTrainerAppCmd cmd, CancellationToken ct)
    {
        var ebContext = _context.EarlyBoundContext ?? throw new InvalidOperationException("EarlyBoundContext is not initialized.");

        Spd_Application? app = null;
        CrmContact? contact = null;
        if (cmd.ApplicationTypeCode == ApplicationTypeEnum.New)
        {
            CrmContact contactLegacy = _mapper.Map<CrmContact>(cmd);
            contact = contactLegacy.ToEntity<CrmContact>();
            ebContext.AddObject(contact);
            app = PrepareNewAppDataInDbContext(cmd, contact);
            app.Spd_ApplicantId = contact.ToEntityReference();
        }
        else if (cmd.ApplicationTypeCode == ApplicationTypeEnum.Renewal || cmd.ApplicationTypeCode == ApplicationTypeEnum.Replacement)
        {
            contact = UpdateContact(cmd, (Guid)cmd.ApplicantId);
            app = PrepareNewAppDataInDbContext(cmd, contact);
            if (contact != null)
            {
                app.Spd_ApplicantId = contact.ToEntityReference();
            }
            SharedRepositoryFuncs.LinkLicence(ebContext, cmd.OriginalLicenceId, app);
        }

        ebContext.SaveChanges();

        if (app == null || contact == null)
        {
            throw new ApiException(HttpStatusCode.InternalServerError);
        }

        return new DogTrainerAppCmdResp((Guid)app.Spd_ApplicationId, contact.ContactId.Value);
    }

    public async Task<DogTrainerAppResp> GetDogTrainerAppAsync(Guid appId, CancellationToken ct)
    {
        var ebContext = _context.EarlyBoundContext ?? throw new InvalidOperationException("EarlyBoundContext is not initialized.");
        ct.ThrowIfCancellationRequested();

        Spd_Application? app = ebContext.Spd_ApplicationSet
            .Where(a => a.Spd_ApplicationId == appId)
            .FirstOrDefault();

        if (app != null)
        {
            ebContext.LoadProperty(app, nameof(app.Spd_ServiceTypeId));
            ebContext.LoadProperty(app, nameof(app.Spd_ApplicantId));
            ebContext.LoadProperty(app, nameof(app.Spd_Application_Spd_DogTrainingSchool_ApplicationId));
            var trainingSchool = app.Spd_Application_Spd_DogTrainingSchool_ApplicationId.FirstOrDefault();
            if (trainingSchool != null)
            {
                ebContext.LoadProperty(trainingSchool, nameof(trainingSchool.Spd_OrganizationId));
            }
        }

        if (app == null)
        {
            throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the application for application id = {appId} ");
        }

        var response = _mapper.Map<DogTrainerAppResp>(app.ToEntity<CrmApplication>());
        var school = app.Spd_Application_Spd_DogTrainingSchool_ApplicationId.FirstOrDefault();
        if (school != null)
        {
            _mapper.Map<CrmDogTrainingSchool, DogTrainerAppResp>(school.ToEntity<CrmDogTrainingSchool>(), response);
        }

        return response;
    }

    private CrmContact UpdateContact(DogTrainerApp appData, Guid applicantId)
    {

        CrmContact? applicant = _context.ContactSet.FirstOrDefault(c => c.ContactId == applicantId);
        if (applicant == null)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the applicant");
        }
        _mapper.Map(appData, applicant);
        _context.UpdateObject(applicant);

        return applicant;
    }

    private Spd_Application PrepareNewAppDataInDbContext(CreateDogTrainerAppCmd appData, Contact applicant)
    {
        var ebContext = _context.EarlyBoundContext ?? throw new InvalidOperationException("EarlyBoundContext is not initialized.");

        CrmApplication appLegacy = _mapper.Map<CrmApplication>(appData);
        var app = appLegacy.ToEntity<Spd_Application>();
        app.StatusCode = Spd_Application_StatusCode.Incomplete;
        ebContext.AddObject(app);

        if (appData.AccreditedSchoolId != Guid.Empty) //spdbt-4496, for allow the invalid data, we found in staging, when do replacement, some existing trainer licence does not have accredit school, which is not correct. but we have to deal with it.
        {
            CrmDogTrainingSchool trainEventLegacy = _mapper.Map<CrmDogTrainingSchool>(appData);
            var trainEvent = trainEventLegacy.ToEntity<Spd_DogTrainingSchool>();
            ebContext.AddObject(trainEvent);
            ebContext.AddLink(app, new Relationship(Spd_Application.Fields.Spd_Application_Spd_DogTrainingSchool_ApplicationId), trainEvent);
            trainEvent.Spd_ApplicantId = applicant.ToEntityReference();

            var school = ebContext.AccountSet.Where(a => a.AccountId == appData.AccreditedSchoolId).FirstOrDefault();
            if (school != null)
            {
                trainEvent.Spd_OrganizationId = school.ToEntityReference();
            }

            SharedRepositoryFuncs.LinkTrainingEventTeam(ebContext, Spd.Utilities.Dynamics.DataverseConstants.Licensing_Client_Service_Team_Guid, trainEvent);
        }

        SharedRepositoryFuncs.LinkServiceType(ebContext, appData.ServiceTypeCode, app);
        SharedRepositoryFuncs.LinkTeam(ebContext, Spd.Utilities.Dynamics.DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        return app;
    }
}

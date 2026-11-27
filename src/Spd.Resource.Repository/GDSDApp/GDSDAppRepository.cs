using AutoMapper;
using Microsoft.Xrm.Sdk;
using Spd.Resource.Repository.DogBase;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using ApplicationEntity = Spd.Utilities.Dataverse.EarlyBound.Spd_Application;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;
using TeamEntity = Spd.Utilities.Dataverse.EarlyBound.Team;
using TrainingSchoolEntity = Spd.Utilities.Dataverse.EarlyBound.Spd_DogTrainingSchool;

namespace Spd.Resource.Repository.GDSDApp;

internal class GDSDAppRepository : DogAppBaseRepository, IGDSDAppRepository
{
    private readonly DataverseContext _dataverseContext;

    public GDSDAppRepository(IDataverseContextFactory ctx, IMapper mapper) : base(ctx, mapper)
    {
        _dataverseContext = ctx.CreateChangeOverwrite();
    }

    public Task<GDSDAppCmdResp> CreateGDSDAppAsync(CreateGDSDAppCmd cmd, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ApplicationEntity? app;
        ContactEntity? contact;

        if (cmd.ApplicationTypeCode == ApplicationTypeEnum.New)
        {
            contact = _mapper.Map<ContactEntity>(cmd);
            _dataverseContext.AddObject(contact);
            app = PrepareNewAppDataInDbContext(cmd, contact, ct);
            app.Spd_ApplicantId = contact.ToEntityReference();
        }
        else if (cmd.ApplicationTypeCode == ApplicationTypeEnum.Renewal ||
                 cmd.ApplicationTypeCode == ApplicationTypeEnum.Replacement)
        {
            app = _mapper.Map<ApplicationEntity>(cmd);

            if (cmd.OriginalApplicationId.HasValue)
            {
                ct.ThrowIfCancellationRequested();
                ApplicationEntity? originalApp = _dataverseContext.Spd_ApplicationSet
                    .Where(application => application.Spd_ApplicationId == cmd.OriginalApplicationId)
                    .FirstOrDefault();
                if (originalApp == null)
                    throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the original application for renewal or replacement");

                app.Spd_DogsInoculationsUpToDate = originalApp.Spd_DogsInoculationsUpToDate;
                app.Spd_DogSpayedOrNeutered = originalApp.Spd_DogSpayedOrNeutered;
                app.Spd_DogStrainingAccredited = originalApp.Spd_DogStrainingAccredited;
            }

            if (!cmd.ApplicantId.HasValue)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the applicant");

            _dataverseContext.AddObject(app);
            contact = UpdateContact(cmd, cmd.ApplicantId.Value, ct);
            app.Spd_ApplicantId = contact.ToEntityReference();
            SharedRepositoryFuncs.LinkLicence(_dataverseContext, cmd.OriginalLicenceId, app);
            SharedRepositoryFuncs.LinkServiceType(_dataverseContext, cmd.ServiceTypeCode, app);
            SharedRepositoryFuncs.LinkTeam(_dataverseContext, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
            SharedRepositoryFuncs.LinkDog(_dataverseContext, cmd.DogId, app);
        }
        else
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Unsupported application type");
        }

        ct.ThrowIfCancellationRequested();
        _dataverseContext.SaveChanges();
        if (app.Spd_ApplicationId == null || contact.ContactId == null)
            throw new ApiException(HttpStatusCode.InternalServerError);

        return Task.FromResult(new GDSDAppCmdResp(app.Spd_ApplicationId.Value, contact.ContactId.Value));
    }

    public Task<GDSDAppCmdResp> SaveGDSDAppAsync(SaveGDSDAppCmd cmd, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ContactEntity applicant = UpdateContact(cmd, cmd.ApplicantId, ct);
        ApplicationEntity app;

        if (cmd.LicenceAppId.HasValue)
        {
            app = PrepareUpdateAppDataInDbContext(cmd, cmd.LicenceAppId.Value, applicant, ct);
        }
        else
        {
            app = PrepareNewAppDataInDbContext(cmd, applicant, ct);
            app.Spd_ApplicantId = applicant.ToEntityReference();
        }

        ct.ThrowIfCancellationRequested();
        _dataverseContext.SaveChanges();
        return Task.FromResult(new GDSDAppCmdResp(app.Spd_ApplicationId!.Value, cmd.ApplicantId));
    }

    public Task<GDSDAppResp> GetGDSDAppAsync(Guid licenceApplicationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ApplicationEntity? app = _dataverseContext.Spd_ApplicationSet
            .Where(application => application.Spd_ApplicationId == licenceApplicationId)
            .FirstOrDefault();

        if (app == null)
            throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the application for application id = {licenceApplicationId} ");

        ct.ThrowIfCancellationRequested();
        _dataverseContext.LoadProperty(app, nameof(ApplicationEntity.Spd_Application_Spd_DogTrainingSchool_ApplicationId));
        return Task.FromResult(_mapper.Map<GDSDAppResp>(app));
    }

    private ApplicationEntity PrepareNewAppDataInDbContext<TApp>(
        TApp appData,
        ContactEntity applicant,
        CancellationToken ct)
        where TApp : GDSDApp
    {
        ApplicationEntity app = _mapper.Map<ApplicationEntity>(appData);
        if (appData is CreateGDSDAppCmd)
            app.StatusCode = Spd_Application_StatusCode.Incomplete;

        _dataverseContext.AddObject(app);
        TeamEntity? serviceTeam = GetServiceTeam(ct);

        if (appData.IsDogTrainedByAccreditedSchool == true)
        {
            app.Spd_DogsInoculationsUpToDate = Spd_YesNo.Yes;
            if (appData.AccreditedSchoolQuestions != null)
                _mapper.Map(appData.AccreditedSchoolQuestions, app);

            if (appData.AccreditedSchoolQuestions?.GraduationInfo is GraduationInfo graduationInfo)
            {
                TrainingSchoolEntity graduation = _mapper.Map<TrainingSchoolEntity>(graduationInfo);
                graduation.Spd_TrainingSchoolType = Spd_DogTrainingSchoolType.AccreditedSchool;
                SetGraduationOrganization(graduation, graduationInfo.AccreditedSchoolId, ct);
                AddTrainingSchool(app, applicant, serviceTeam, graduation);
            }
        }
        else if (appData.NonAccreditedSchoolQuestions is NonAccreditedSchoolQuestions questions)
        {
            _mapper.Map(questions, app);
            app.Spd_DogsAssistanceInDailyLiving = questions.TrainingInfo?.SpecializedTasksWhenPerformed;

            if (questions.TrainingInfo?.HasAttendedTrainingSchool == true)
            {
                foreach (TrainingSchoolInfo schoolInfo in questions.TrainingInfo.SchoolTrainings ?? [])
                {
                    ct.ThrowIfCancellationRequested();
                    TrainingSchoolEntity school = _mapper.Map<TrainingSchoolEntity>(schoolInfo);
                    AddTrainingSchool(app, applicant, serviceTeam, school);
                }
            }
            else
            {
                foreach (OtherTraining other in questions.TrainingInfo?.OtherTrainings ?? [])
                {
                    ct.ThrowIfCancellationRequested();
                    TrainingSchoolEntity training = _mapper.Map<TrainingSchoolEntity>(other);
                    AddTrainingSchool(app, applicant, serviceTeam, training);
                }
            }
        }

        ct.ThrowIfCancellationRequested();
        SharedRepositoryFuncs.LinkServiceType(_dataverseContext, appData.ServiceTypeCode, app);
        SharedRepositoryFuncs.LinkTeam(_dataverseContext, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        return app;
    }

    private ApplicationEntity PrepareUpdateAppDataInDbContext(
        GDSDApp appData,
        Guid appId,
        ContactEntity applicant,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ApplicationEntity? app = _dataverseContext.Spd_ApplicationSet
            .Where(application => application.Spd_ApplicationId == appId)
            .FirstOrDefault();
        if (app == null)
            throw new ArgumentException("invalid app id");

        ct.ThrowIfCancellationRequested();
        _dataverseContext.LoadProperty(app, nameof(ApplicationEntity.Spd_Application_Spd_DogTrainingSchool_ApplicationId));
        _mapper.Map<GDSDApp, ApplicationEntity>(appData, app);
        _dataverseContext.UpdateObject(app);

        TeamEntity? serviceTeam = GetServiceTeam(ct);
        List<TrainingSchoolEntity> existingTrainings = app.Spd_Application_Spd_DogTrainingSchool_ApplicationId.ToList();

        if (appData.IsDogTrainedByAccreditedSchool == true)
        {
            if (appData.AccreditedSchoolQuestions != null)
                _mapper.Map(appData.AccreditedSchoolQuestions, app);

            app.StatusCode = Spd_Application_StatusCode.Incomplete;
            _dataverseContext.UpdateObject(app);

            foreach (TrainingSchoolEntity training in existingTrainings)
            {
                if (training.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.UnaccreditedSchool ||
                    training.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.Other)
                {
                    _dataverseContext.DeleteObject(training);
                }
            }

            GraduationInfo? graduationInfo = appData.AccreditedSchoolQuestions?.GraduationInfo;
            if (graduationInfo != null)
            {
                TrainingSchoolEntity? existingGraduation = existingTrainings
                    .FirstOrDefault(training => training.Spd_TrainingSchoolType == Spd_DogTrainingSchoolType.AccreditedSchool);
                if (existingGraduation == null)
                {
                    TrainingSchoolEntity graduation = _mapper.Map<TrainingSchoolEntity>(graduationInfo);
                    graduation.Spd_TrainingSchoolType = Spd_DogTrainingSchoolType.AccreditedSchool;
                    SetGraduationOrganization(graduation, graduationInfo.AccreditedSchoolId, ct);
                    AddTrainingSchool(app, applicant, serviceTeam, graduation);
                }
                else
                {
                    _mapper.Map(graduationInfo, existingGraduation);
                    existingGraduation.Spd_TrainingSchoolType = Spd_DogTrainingSchoolType.AccreditedSchool;
                    SetGraduationOrganization(existingGraduation, graduationInfo.AccreditedSchoolId, ct);
                    _dataverseContext.UpdateObject(existingGraduation);
                    if (serviceTeam != null)
                        SharedRepositoryFuncs.LinkTrainingEventTeam(_dataverseContext, serviceTeam, existingGraduation);
                }
            }
        }
        else
        {
            NonAccreditedSchoolQuestions? questions = appData.NonAccreditedSchoolQuestions;
            if (questions != null)
            {
                _mapper.Map(questions, app);
                app.Spd_DogsAssistanceInDailyLiving = questions.TrainingInfo?.SpecializedTasksWhenPerformed;
                app.StatusCode = Spd_Application_StatusCode.Incomplete;
                _dataverseContext.UpdateObject(app);
            }

            TrainingInfo? trainingInfo = questions?.TrainingInfo;
            IEnumerable<TrainingSchoolInfo>? schools = trainingInfo?.SchoolTrainings;
            IEnumerable<OtherTraining>? otherTrainings = trainingInfo?.OtherTrainings;

            foreach (TrainingSchoolEntity training in existingTrainings)
            {
                bool isIncludedSchool = schools?.Any(item => item.TrainingId == training.Spd_DogTrainingSchoolId) == true;
                bool isIncludedOther = otherTrainings?.Any(item => item.TrainingId == training.Spd_DogTrainingSchoolId) == true;
                if (!isIncludedSchool && !isIncludedOther)
                    _dataverseContext.DeleteObject(training);
            }

            if (trainingInfo?.HasAttendedTrainingSchool == true)
            {
                foreach (TrainingSchoolInfo schoolInfo in schools ?? [])
                {
                    ct.ThrowIfCancellationRequested();
                    if (schoolInfo.TrainingId == null)
                    {
                        TrainingSchoolEntity school = _mapper.Map<TrainingSchoolEntity>(schoolInfo);
                        AddTrainingSchool(app, applicant, null, school);
                    }
                    else
                    {
                        TrainingSchoolEntity existingSchool = FindExistingTraining(existingTrainings, schoolInfo.TrainingId.Value);
                        _mapper.Map(schoolInfo, existingSchool);
                        _dataverseContext.UpdateObject(existingSchool);
                    }
                }
            }
            else if (trainingInfo != null)
            {
                foreach (OtherTraining other in otherTrainings ?? [])
                {
                    ct.ThrowIfCancellationRequested();
                    if (other.TrainingId == null)
                    {
                        TrainingSchoolEntity training = _mapper.Map<TrainingSchoolEntity>(other);
                        AddTrainingSchool(app, applicant, serviceTeam, training);
                    }
                    else
                    {
                        TrainingSchoolEntity existingTraining = FindExistingTraining(existingTrainings, other.TrainingId.Value);
                        _mapper.Map(other, existingTraining);
                        _dataverseContext.UpdateObject(existingTraining);
                    }
                }
            }
        }

        ct.ThrowIfCancellationRequested();
        SharedRepositoryFuncs.LinkServiceType(_dataverseContext, appData.ServiceTypeCode, app);
        SharedRepositoryFuncs.LinkTeam(_dataverseContext, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        return app;
    }

    private ContactEntity UpdateContact(GDSDApp appData, Guid applicantId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ContactEntity? applicant = _dataverseContext.ContactSet
            .Where(contact => contact.ContactId == applicantId)
            .FirstOrDefault();
        if (applicant == null)
            throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the applicant");

        _mapper.Map<GDSDApp, ContactEntity>(appData, applicant);
        _dataverseContext.UpdateObject(applicant);
        return applicant;
    }

    private TeamEntity? GetServiceTeam(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Guid teamId = Guid.Parse(DataverseConstants.Licensing_Client_Service_Team_Guid);
        return _dataverseContext.TeamSet
            .Where(team => team.TeamId == teamId)
            .FirstOrDefault();
    }

    private void AddTrainingSchool(
        ApplicationEntity app,
        ContactEntity applicant,
        TeamEntity? serviceTeam,
        TrainingSchoolEntity trainingSchool)
    {
        _dataverseContext.AddObject(trainingSchool);
        _dataverseContext.AddLink(
            app,
            new Relationship(ApplicationEntity.Fields.Spd_Application_Spd_DogTrainingSchool_ApplicationId),
            trainingSchool);
        trainingSchool.Spd_ApplicantId = applicant.ToEntityReference();
        if (serviceTeam != null)
            SharedRepositoryFuncs.LinkTrainingEventTeam(_dataverseContext, serviceTeam, trainingSchool);
    }

    private void SetGraduationOrganization(TrainingSchoolEntity graduation, Guid? organizationId, CancellationToken ct)
    {
        graduation.Spd_OrganizationId = null;
        if (!organizationId.HasValue)
            return;

        ct.ThrowIfCancellationRequested();
        Account? organization = _dataverseContext.AccountSet
            .Where(account => account.AccountId == organizationId)
            .FirstOrDefault();
        graduation.Spd_OrganizationId = organization?.ToEntityReference();
    }

    private static TrainingSchoolEntity FindExistingTraining(
        IEnumerable<TrainingSchoolEntity> trainings,
        Guid trainingId)
    {
        return trainings.FirstOrDefault(training => training.Spd_DogTrainingSchoolId == trainingId)
            ?? throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find training record {trainingId}");
    }
}

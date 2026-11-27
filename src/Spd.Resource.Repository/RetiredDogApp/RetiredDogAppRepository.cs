using AutoMapper;
using Spd.Resource.Repository.DogBase;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using ApplicationEntity = Spd.Utilities.Dataverse.EarlyBound.Spd_Application;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;

namespace Spd.Resource.Repository.RetiredDogApp;

internal class RetiredDogAppRepository : DogAppBaseRepository, IRetiredDogAppRepository
{
    public RetiredDogAppRepository(IDataverseContextFactory ctx, IMapper mapper) : base(ctx, mapper)
    {
    }

    public System.Threading.Tasks.Task<RetiredDogAppCmdResp> CreateRetiredDogAppAsync(CreateRetiredDogAppCmd cmd, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ApplicationEntity app;
        ContactEntity contact;

        if (cmd.ApplicationTypeCode == ApplicationTypeEnum.New)
        {
            contact = _mapper.Map<ContactEntity>(cmd);
            _context.AddObject(contact);
            app = _mapper.Map<ApplicationEntity>(cmd);
            app.StatusCode = Spd_Application_StatusCode.Incomplete;
            _context.AddObject(app);
            SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
            SharedRepositoryFuncs.LinkTeam(_context, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
            app.Spd_ApplicantId = contact.ToEntityReference();
        }
        else if (cmd.ApplicationTypeCode == ApplicationTypeEnum.Renewal ||
                 cmd.ApplicationTypeCode == ApplicationTypeEnum.Replacement)
        {
            if (!cmd.ApplicantId.HasValue)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the applicant");

            app = _mapper.Map<ApplicationEntity>(cmd);
            _context.AddObject(app);
            contact = UpdateContact(cmd, cmd.ApplicantId.Value, ct);
            app.Spd_ApplicantId = contact.ToEntityReference();
            SharedRepositoryFuncs.LinkLicence(_context, cmd.OriginalLicenceId, app);
            SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
            SharedRepositoryFuncs.LinkTeam(_context, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
            SharedRepositoryFuncs.LinkDog(_context, cmd.DogId, app);
        }
        else
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Unsupported application type");
        }

        ct.ThrowIfCancellationRequested();
        _context.SaveChanges();
        if (!app.Spd_ApplicationId.HasValue || !contact.ContactId.HasValue)
            throw new ApiException(HttpStatusCode.InternalServerError);

        return System.Threading.Tasks.Task.FromResult(new RetiredDogAppCmdResp(app.Spd_ApplicationId.Value, contact.ContactId.Value));
    }

    public System.Threading.Tasks.Task<RetiredDogAppCmdResp> SaveRetiredDogAppAsync(SaveRetiredDogAppCmd cmd, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ContactEntity applicant = UpdateContact(cmd, cmd.ApplicantId, ct);
        ApplicationEntity app;

        if (cmd.LicenceAppId.HasValue)
        {
            ct.ThrowIfCancellationRequested();
            _context.SaveChanges();

            ct.ThrowIfCancellationRequested();
            app = _context.Spd_ApplicationSet
                .Where(application => application.Spd_ApplicationId == cmd.LicenceAppId)
                .FirstOrDefault()
                ?? throw new ArgumentException("invalid app id");

            _mapper.Map<SaveRetiredDogAppCmd, ApplicationEntity>(cmd, app);
            _context.UpdateObject(app);
            SharedRepositoryFuncs.LinkServiceType(_context, cmd.ServiceTypeCode, app);
            SharedRepositoryFuncs.LinkTeam(_context, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        }
        else
        {
            app = PrepareNewAppDataInDbContext(cmd);
            app.Spd_ApplicantId = applicant.ToEntityReference();
        }

        ct.ThrowIfCancellationRequested();
        _context.SaveChanges();
        if (!app.Spd_ApplicationId.HasValue)
            throw new ApiException(HttpStatusCode.InternalServerError);

        return System.Threading.Tasks.Task.FromResult(new RetiredDogAppCmdResp(app.Spd_ApplicationId.Value, cmd.ApplicantId));
    }

    public System.Threading.Tasks.Task<RetiredDogAppResp> GetRetiredDogAppAsync(Guid licenceApplicationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ApplicationEntity? app = _context.Spd_ApplicationSet
            .Where(application => application.Spd_ApplicationId == licenceApplicationId)
            .FirstOrDefault();

        if (app == null)
            throw new ApiException(HttpStatusCode.BadRequest, $"Cannot find the application for application id = {licenceApplicationId} ");

        return System.Threading.Tasks.Task.FromResult(_mapper.Map<RetiredDogAppResp>(app));
    }

    private ApplicationEntity PrepareNewAppDataInDbContext(SaveRetiredDogAppCmd appData)
    {
        ApplicationEntity app = _mapper.Map<ApplicationEntity>(appData);
        app.Spd_ApplicationId = Guid.NewGuid();
        app.StatusCode = Spd_Application_StatusCode.Incomplete;
        _context.AddObject(app);
        SharedRepositoryFuncs.LinkServiceType(_context, appData.ServiceTypeCode, app);
        SharedRepositoryFuncs.LinkTeam(_context, DataverseConstants.Licensing_Client_Service_Team_Guid, app);
        return app;
    }

    private ContactEntity UpdateContact(RetiredDogApp appData, Guid applicantId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ContactEntity applicant = _context.ContactSet
            .Where(contact => contact.ContactId == applicantId)
            .FirstOrDefault()
            ?? throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the applicant");

        _mapper.Map<RetiredDogApp, ContactEntity>(appData, applicant);
        _context.UpdateObject(applicant);
        return applicant;
    }
}

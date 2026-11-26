using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Application;
internal partial class ApplicationRepository : IApplicationRepository
{
    public async Task<BulkHistoryListResp> QueryBulkHistoryAsync(BulkHistoryListQry query, CancellationToken ct)
    {
        IQueryable<Spd_GenericUpload> uploads = _context.Spd_GenericUploadSet
            .Where(u => u.Spd_OrganizationId != null && u.Spd_OrganizationId.Id == query.OrgId)
            .Where(u => u.StateCode == Spd_GenericUpload_StateCode.Active);

        if (query.SortBy == null || query.SortBy.SubmittedDateDesc == null || query.SortBy.SubmittedDateDesc == true)
            uploads = uploads.OrderByDescending(a => a.Spd_DateTimeUploaded);
        if (query?.SortBy?.SubmittedDateDesc == false)
            uploads = uploads.OrderBy(a => a.Spd_DateTimeUploaded);

        List<Spd_GenericUpload> allUploads = uploads.ToList();

        foreach (Spd_GenericUpload upload in allUploads)
        {
            _context.LoadProperty(upload, nameof(Spd_GenericUpload.Spd_PortalUser_GenericUpload_UploadedBy));
        }

        IEnumerable<Spd_GenericUpload> pagedUploads = allUploads;
        if (query?.Paging != null)
        {
            pagedUploads = allUploads
                .Skip(query.Paging.Page * query.Paging.PageSize)
                .Take(query.Paging.PageSize);
        }

        var response = new BulkHistoryListResp
        {
            BulkUploadHistorys = pagedUploads.Select(u => new BulkHistoryResp
            {
                Id = u.Spd_GenericUploadId ?? Guid.Empty,
                BatchNumber = u.Spd_BatchNumber ?? string.Empty,
                FileName = u.Spd_FileName ?? string.Empty,
                UploadedDateTime = u.Spd_DateTimeUploaded.HasValue ? new DateTimeOffset(u.Spd_DateTimeUploaded.Value) : DateTimeOffset.MinValue,
                UploadedByUserFullName = u.Spd_PortalUser_GenericUpload_UploadedBy?.Spd_FullName ?? string.Empty
            })
        };

        if (query?.Paging != null)
        {
            response.Pagination = new PaginationResp
            {
                PageSize = query.Paging.PageSize,
                PageIndex = query.Paging.Page,
                Length = allUploads.Count
            };
        }

        return await Task.FromResult(response);
    }

    public async Task<BulkAppsCreateResp> AddBulkAppsAsync(BulkAppsCreateCmd cmd, CancellationToken ct)
    {
        Account? org = _context.AccountSet
            .Where(a => a.AccountId == cmd.OrgId)
            .SingleOrDefault();

        if (org == null)
            throw new ApiException(HttpStatusCode.PreconditionFailed, "Invalid organization or user.");

        _context.LoadProperty(org, nameof(Account.Spd_Account_Spd_ServiceType));
        Spd_ServiceType? servicetype = org.Spd_Account_Spd_ServiceType
            .FirstOrDefault(s => s.Spd_ServiceCategory == Spd_ServiceCategory.Screening);

        Spd_PortalUser? user = _context.Spd_PortalUserSet.Where(u => u.Spd_PortalUserId == cmd.UserId).FirstOrDefault();
        Team? team = _context.TeamSet.Where(t => t.TeamId == Guid.Parse(DataverseConstants.Client_Service_Team_Guid)).FirstOrDefault();

        if (user == null || team == null)
            throw new ApiException(HttpStatusCode.PreconditionFailed, "Invalid organization or user.");

        List<ApplicationCreateCmd> createApps = cmd.CreateApps.ToList();
        List<ApplicationCreateRslt> results = new();
        for (int i = 0; i < createApps.Count; i++)
        {
            results.Add(new ApplicationCreateRslt { LineNumber = i + 1 });
        }

        int begin = 0;
        int oneChangeSetMaxApps = 1000 / 15;
        while (begin < createApps.Count)
        {
            int len = createApps.Count - begin;
            if (len > oneChangeSetMaxApps)
                len = oneChangeSetMaxApps;

            for (int index = begin; index < begin + len; index++)
            {
                Spd_Application app = await CreateAppEarlyBoundAsync(createApps[index], org, user, team, servicetype);
                if (app.Spd_ApplicationId.HasValue)
                {
                    results[index].ApplicationId = app.Spd_ApplicationId.Value;
                }
            }

            _context.SaveChanges();
            results.ForEach(r =>
            {
                if (r.LineNumber > begin && r.LineNumber <= begin + len)
                {
                    r.CreateSuccess = true;
                }
            });
            begin += len;
        }

        int bulkSize = createApps.Count;
        int successCount = results.Count(r => r.CreateSuccess);

        BulkAppsCreateResultCd bulkResult;
        if (successCount == bulkSize)
        {
            bulkResult = BulkAppsCreateResultCd.Success;
        }
        else if (successCount == 0)
        {
            bulkResult = BulkAppsCreateResultCd.Failed;
        }
        else
        {
            bulkResult = BulkAppsCreateResultCd.PartiallySuccess;
        }

        if (bulkResult != BulkAppsCreateResultCd.Failed)
        {
            Spd_GenericUpload bulkInfo = new()
            {
                Spd_GenericUploadId = Guid.NewGuid(),
                Spd_FileName = cmd.FileName,
                Spd_DateTimeUploaded = DateTime.UtcNow,
                Spd_BatchNumber = DateTime.Now.Ticks.ToString()
            };

            bulkInfo.Spd_UploadedBy = user.ToEntityReference();
            bulkInfo.Spd_OrganizationId = org.ToEntityReference();
            _context.AddObject(bulkInfo);
            _context.SaveChanges();
        }

        return await Task.FromResult(new BulkAppsCreateResp { CreateAppResults = results, BulkAppsCreateCode = bulkResult });
    }

    private async Task<Spd_Application> CreateAppEarlyBoundAsync(
        ApplicationCreateCmd cmd,
        Account org,
        Spd_PortalUser? user,
        Team team,
        Spd_ServiceType? serviceType,
        Account? parentOrg = null)
    {
        var app = new Spd_Application
        {
            Spd_ApplicationId = Guid.NewGuid(),
            Spd_OrganizationId = org.ToEntityReference(),
            Spd_SubmittedBy = user?.ToEntityReference(),
            OwnerId = team.ToEntityReference(),
            Spd_ServiceTypeId = serviceType?.ToEntityReference(),
            Spd_ParentOrganizationId = parentOrg?.ToEntityReference(),
            Spd_FirstName = cmd.GivenName,
            Spd_MiddleName1 = cmd.MiddleName1,
            Spd_MiddleName2 = cmd.MiddleName2,
            Spd_LastName = cmd.Surname,
            Spd_EmailAddress1 = cmd.EmailAddress,
            Spd_EmployeeId = cmd.EmployeeId,
            Spd_UploadId = cmd.UploadId,
            Spd_DateOfBirth = cmd.DateOfBirth.HasValue ? new DateTime(cmd.DateOfBirth.Value.Year, cmd.DateOfBirth.Value.Month, cmd.DateOfBirth.Value.Day) : null,
            StateCode = Spd_Application_StateCode.Active,
            StatusCode = cmd.HaveVerifiedIdentity == true ? Spd_Application_StatusCode.PaymentPending : Spd_Application_StatusCode.ApplicantVerification,
            Spd_IdentityConfirmed = cmd.HaveVerifiedIdentity
        };

        _context.AddObject(app);
        return await Task.FromResult(app);
    }
}




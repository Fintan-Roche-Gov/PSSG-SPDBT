using AutoMapper;
using Microsoft.Extensions.Logging;
using Spd.Resource.Repository.Application;
using Spd.Resource.Repository.Registration;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.FileStorage;
using System.Collections.Immutable;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using TaskEntity = Spd.Utilities.Dataverse.EarlyBound.Task;
using SystemTask = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Document;

internal class DocumentRepository : IDocumentRepository
{
    private readonly DataverseContext _context;
    private readonly IMapper _mapper;
    private readonly IMainFileStorageService _fileStorageService;
    private readonly ITransientFileStorageService _transientFileStorageService;
    private readonly ITempFileStorageService _tempFileService;
    private readonly ILogger<IDocumentRepository> _logger;

    public DocumentRepository(DataverseContext ctx,
        IMapper mapper,
        IMainFileStorageService fileStorageService,
        ITempFileStorageService tempFileService,
        ITransientFileStorageService transientFileStorageService,
        ILogger<IDocumentRepository> logger)
    {
        _context = ctx;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
        _tempFileService = tempFileService;
        _transientFileStorageService = transientFileStorageService;
        _logger = logger;
    }

    public async Task<DocumentResp> GetAsync(Guid docUrlId, CancellationToken ct)
    {
        var document = _context.Bcgov_DocumentUrlSet
            .Where(d => d.Bcgov_DocumentUrlId == docUrlId)
            .FirstOrDefault();
        return _mapper.Map<DocumentResp>(document);
    }

    public async Task<DocumentListResp> QueryAsync(DocumentQry qry, CancellationToken ct)
    {
        var documents = _context.Bcgov_DocumentUrlSet.Where(d => d.StateCode != Bcgov_DocumentUrl_StateCode.Inactive);
        if (qry.ApplicantId != null)
            documents = documents.Where(d => d.Bcgov_Customer != null && d.Bcgov_Customer.Id == qry.ApplicantId);

        if (qry.AccountId != null)
            documents = documents.Where(d => d.Bcgov_Customer != null && d.Bcgov_Customer.Id == qry.AccountId);

        if (qry.ApplicationId != null)
            documents = documents.Where(d => d.Spd_ApplicationId != null && d.Spd_ApplicationId.Id == qry.ApplicationId);

        if (qry.CaseId != null)
            documents = documents.Where(d => d.Bcgov_CaseId != null && d.Bcgov_CaseId.Id == qry.CaseId);

        if (qry.ClearanceId != null)
            documents = documents.Where(d => d.Spd_ClearanceId != null && d.Spd_ClearanceId.Id == qry.ClearanceId);

        if (qry.ReportId != null)
            documents = documents.Where(d => d.Spd_PdfReportId != null && d.Spd_PdfReportId.Id == qry.ReportId);

        if (qry.LicenceId != null)
            documents = documents.Where(d => d.Spd_LicenceId != null && d.Spd_LicenceId.Id == qry.LicenceId);

        if (qry.FileType != null)
        {
            DynamicsContextLookupHelpers.BcGovTagDictionary.TryGetValue(qry.FileType.ToString(), out Guid tagId);
            documents = documents.Where(d => (d.Bcgov_Tag1Id != null && d.Bcgov_Tag1Id.Id == tagId)
                || (d.Bcgov_Tag2Id != null && d.Bcgov_Tag2Id.Id == tagId)
                || (d.Bcgov_Tag3Id != null && d.Bcgov_Tag3Id.Id == tagId));
        }

        var results = documents.ToList();
        IEnumerable<DocumentResp> resp = null;
        if (qry.MultiFileTypes != null)
        {
            List<Guid> tagIds = qry.MultiFileTypes.Select(f => DynamicsContextLookupHelpers.BcGovTagDictionary.GetValueOrDefault(f.ToString())).ToList();
            List<Bcgov_DocumentUrl> result = results
                .Where(d => d.Bcgov_Tag1Id != null && tagIds.Contains(d.Bcgov_Tag1Id.Id)).ToList();
            resp = _mapper.Map<IEnumerable<DocumentResp>>(result.OrderByDescending(a => a.CreatedOn));
        }
        else
        {
            results = results.OrderByDescending(a => a.CreatedOn).ToList();
            resp = _mapper.Map<IEnumerable<DocumentResp>>(results);
        }
        return qry.OnlyReturnLatestSet ? new DocumentListResp { Items = GetLatestSet(resp) } : new DocumentListResp { Items = resp };
    }

    public async Task<DocumentResp> ManageAsync(DocumentCmd cmd, CancellationToken ct)
    {
        return cmd switch
        {
            CreateDocumentCmd c => await DocumentCreateAsync(c, ct),
            RemoveDocumentCmd c => await DocumentRemoveAsync(c, ct),
            ReactivateDocumentCmd c => await DocumentReactivateAsync(c, ct),
            UpdateDocumentCmd c => await DocumentUpdateAsync(c, ct),
            CopyDocumentCmd c => await DocumentCopyAsync(c, ct),
            DeactivateDocumentCmd c => await DocumentDeactivateAsync(c, ct),
            _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
        };
    }

    private async Task<DocumentResp> DocumentReactivateAsync(ReactivateDocumentCmd cmd, CancellationToken ct)
    {
        Bcgov_DocumentUrl? documenturl = _context.Bcgov_DocumentUrlSet.Where(d => d.Bcgov_DocumentUrlId == cmd.DocumentUrlId).FirstOrDefault();
        if (documenturl == null) { return null; }
        documenturl.StateCode = Bcgov_DocumentUrl_StateCode.Active;
        documenturl.StatusCode = Bcgov_DocumentUrl_StatusCode.Active;
        _context.UpdateObject(documenturl);
        _context.SaveChanges();
        return _mapper.Map<DocumentResp>(documenturl);
    }

    //if the documents are in the same application, then we use applicationId to indicate its set.
    //if the documents are in the same licence, then we use licenceId to indicate its set.
    //Or we use uploadedDatetime
    private IEnumerable<DocumentResp> GetLatestSet(IEnumerable<DocumentResp> resp)
    {
        if (resp.Any())
        {
            DocumentResp? doc = resp.FirstOrDefault();
            if (doc?.LicenceId != null)
                return resp.Where(i => i.LicenceId == doc.LicenceId).ToList();
            else if (doc?.ApplicationId != null)
                return resp.Where(i => i.ApplicationId == doc.ApplicationId).ToList();
            else
                return resp.Where(i => i.CreatedOn == doc?.CreatedOn).ToList();
            //else
            //    return resp.Where(i => i.UploadedDateTime == doc.UploadedDateTime).ToList();
        }
        return resp;
    }

    private async Task<DocumentResp> DocumentCreateAsync(CreateDocumentCmd cmd, CancellationToken ct)
    {
        Bcgov_DocumentUrl documenturl = _mapper.Map<Bcgov_DocumentUrl>(cmd.TempFile);
        if (cmd.ApplicationId != null)
            documenturl.Bcgov_Url = $"spd_application/{cmd.ApplicationId}";
        else if (cmd.ApplicantId != null)
            documenturl.Bcgov_Url = $"contact/{cmd.ApplicantId}";
        else if (cmd.OrgRegistrationId != null)
            documenturl.Bcgov_Url = $"spd_orgregistration/{cmd.OrgRegistrationId}";

        if (cmd.ExpiryDate != null && cmd.ExpiryDate < new DateOnly(1800, 1, 1))
            throw new ArgumentException("Invalid Document Expiry Date");
        if (cmd.ExpiryDate != null) documenturl.Spd_ExpiryDate = new DateTime(cmd.ExpiryDate.Value.Year, cmd.ExpiryDate.Value.Month, cmd.ExpiryDate.Value.Day, 0, 0, 0, DateTimeKind.Utc);
        documenturl.Spd_DocumentId = cmd.DocumentIdNumber;
        _context.AddObject(documenturl);
        if (cmd.ApplicationId != null)
        {
            Spd_Application? application = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == cmd.ApplicationId).FirstOrDefault();
            if (application == null)
                throw new ArgumentException("invalid application id");
            documenturl.Spd_ApplicationId = application.ToEntityReference();
        }
        if (cmd.ApplicantId != null)
        {
            ContactEntity? contact = _context.ContactSet.Where(c => c.ContactId == cmd.ApplicantId).FirstOrDefault();
            if (contact == null)
                throw new ArgumentException("invalid contact id");
            documenturl.Bcgov_Customer = contact.ToEntityReference();
        }
        if (cmd.OrgRegistrationId != null)
        {
            Spd_OrgRegistration? registration = _context.Spd_OrgRegistrationSet.Where(r => r.Spd_OrgRegistrationId == cmd.OrgRegistrationId).FirstOrDefault();
            if (registration == null)
                throw new ArgumentException("invalid org registration id");
            documenturl.Spd_OrgRegistrationId = registration.ToEntityReference();
        }
        if (cmd.TaskId != null)
        {
            TaskEntity? task = _context.TaskSet.Where(t => t.ActivityId == cmd.TaskId).FirstOrDefault();
            if (task == null)
                throw new ArgumentException("invalid task id");
            documenturl.Bcgov_TaskId = task.ToEntityReference();
        }
        if (cmd.DocumentType != null)
        {
            Spd.Utilities.Dataverse.DynamicsContextLookupHelpers.BcGovTagDictionary.TryGetValue(cmd.DocumentType.ToString(), out Guid tagId);
            var tag = _context.Bcgov_TagSet.Where(t => t.Bcgov_TagId == tagId).FirstOrDefault();
            documenturl.Bcgov_Tag1Id = tag?.ToEntityReference();
        }
        if (cmd.DocumentType2 != null)
        {
            DynamicsContextLookupHelpers.BcGovTagDictionary.TryGetValue(cmd.DocumentType2.ToString(), out Guid tagId2);
            var tag2 = _context.Bcgov_TagSet.Where(t => t.Bcgov_TagId == tagId2).FirstOrDefault();
            documenturl.Bcgov_Tag2Id = tag2?.ToEntityReference();
        }
        if (cmd.SubmittedByApplicantId != null)
        {
            ContactEntity? contact = _context.ContactSet.Where(c => c.ContactId == cmd.SubmittedByApplicantId).FirstOrDefault();
            documenturl.Spd_SubmittedById = contact?.ToEntityReference();
        }
        if (cmd.LicenceId != null)
        {
            Spd_Licence? lic = _context.Spd_LicenceSet.Where(l => l.Spd_LicenceId == cmd.LicenceId).FirstOrDefault();
            documenturl.Spd_LicenceId = lic?.ToEntityReference();
        }
        if (cmd.AccountId != null)
        {
            Account? account = _context.AccountSet.Where(a => a.AccountId == cmd.AccountId).FirstOrDefault();
            documenturl.Bcgov_Customer = account?.ToEntityReference();
        }

        await UploadFileAsync(cmd.TempFile, cmd.ApplicationId, cmd.ApplicantId, documenturl.Bcgov_DocumentUrlId, null, ct, cmd.ToTransientBucket, cmd.OrgRegistrationId);
        _context.SaveChanges();
        return _mapper.Map<DocumentResp>(documenturl);
    }

    private async Task<DocumentResp> DocumentCopyAsync(CopyDocumentCmd cmd, CancellationToken ct)
    {
        Spd_Application? application = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == cmd.DestApplicationId).FirstOrDefault();
        if (application == null)
            throw new ArgumentException("invalid application id");

        Bcgov_DocumentUrl sourceDoc = _context.Bcgov_DocumentUrlSet.Where(d => d.Bcgov_DocumentUrlId == cmd.SourceDocumentUrlId).FirstOrDefault();
        if (sourceDoc == null)
            throw new ArgumentException("cannot find the source documenturl for copying");
        Bcgov_DocumentUrl destDoc = new();
        destDoc.Bcgov_DocumentUrlId = Guid.NewGuid();
        destDoc.Bcgov_Url = $"spd_application/{cmd.DestApplicationId}";
        destDoc.Bcgov_FileName = sourceDoc.Bcgov_FileName;
        destDoc.Bcgov_FileSize = sourceDoc.Bcgov_FileSize;
        destDoc.Bcgov_OriginCode = Bcgov_OriginCode.Web;
        destDoc.Bcgov_ReceivedDate = sourceDoc.Bcgov_ReceivedDate;
        destDoc.Bcgov_FileExtension = sourceDoc.Bcgov_FileExtension;
        destDoc.Spd_ExpiryDate = sourceDoc.Spd_ExpiryDate;
        destDoc.Spd_DocumentId = sourceDoc.Spd_DocumentId;
        _context.AddObject(destDoc);
        destDoc.Spd_ApplicationId = application.ToEntityReference();
        destDoc.Bcgov_Tag1Id = sourceDoc.Bcgov_Tag1Id;
        destDoc.Bcgov_Tag2Id = sourceDoc.Bcgov_Tag2Id;
        destDoc.Bcgov_Tag3Id = sourceDoc.Bcgov_Tag3Id;
        if (cmd.SubmittedByApplicantId != null)
        {
            ContactEntity? contact = _context.ContactSet.Where(c => c.ContactId == cmd.SubmittedByApplicantId).FirstOrDefault();
            destDoc.Spd_SubmittedById = contact?.ToEntityReference();
            destDoc.Bcgov_Customer = contact?.ToEntityReference();
        }

        await _fileStorageService.HandleCommand(new CopyFileCommand(
            SourceKey: cmd.SourceDocumentUrlId.ToString(),
            SourceFolder: sourceDoc.Bcgov_Url,
            DestKey: destDoc.Bcgov_DocumentUrlId.ToString(),
            DestFolder: $"spd_application/{cmd.DestApplicationId}"
            ), ct);

        _context.SaveChanges();
        return _mapper.Map<DocumentResp>(destDoc);
    }

    private async Task<DocumentResp> DocumentRemoveAsync(RemoveDocumentCmd cmd, CancellationToken ct)
    {
        Bcgov_DocumentUrl? documenturl = _context.Bcgov_DocumentUrlSet.Where(d => d.Bcgov_DocumentUrlId == cmd.DocumentUrlId).FirstOrDefault();
        if (documenturl == null) { return null; }
        documenturl.StateCode = Bcgov_DocumentUrl_StateCode.Inactive;
        documenturl.StatusCode = Bcgov_DocumentUrl_StatusCode.Inactive;
        await DeleteFileAsync((Guid)documenturl.Bcgov_DocumentUrlId, documenturl.Spd_ApplicationId?.Id, ct);
        _context.UpdateObject(documenturl);
        _context.SaveChanges();
        return _mapper.Map<DocumentResp>(documenturl);
    }

    private async Task<DocumentResp> DocumentUpdateAsync(UpdateDocumentCmd cmd, CancellationToken ct)
    {
        Bcgov_DocumentUrl? documenturl = _context.Bcgov_DocumentUrlSet
            .Where(d => d.Bcgov_DocumentUrlId == cmd.DocumentUrlId).FirstOrDefault();
        if (documenturl == null) { return null; }
        if (cmd.ExpiryDate != null && cmd.ExpiryDate < new DateOnly(1800, 1, 1))
            throw new ArgumentException("Invalid Document Expiry Date");
        documenturl.Spd_ExpiryDate = cmd.ExpiryDate == null ? null :
            new DateTime(cmd.ExpiryDate.Value.Year, cmd.ExpiryDate.Value.Month, cmd.ExpiryDate.Value.Day, 0, 0, 0, DateTimeKind.Utc);
        documenturl.Spd_DocumentId = cmd.DocumentIdNumber;
        if (cmd.Tag1 != null)
        {
            DynamicsContextLookupHelpers.BcGovTagDictionary.TryGetValue(cmd.Tag1.ToString(), out Guid tagId);
            var tag1 = _context.Bcgov_TagSet.Where(t => t.Bcgov_TagId == tagId).FirstOrDefault();
            documenturl.Bcgov_Tag1Id = tag1?.ToEntityReference();
        }
        if (cmd.Tag2 != null)
        {
            DynamicsContextLookupHelpers.BcGovTagDictionary.TryGetValue(cmd.Tag2.ToString(), out Guid tagId2);
            var tag2 = _context.Bcgov_TagSet.Where(t => t.Bcgov_TagId == tagId2).FirstOrDefault();
            documenturl.Bcgov_Tag2Id = tag2?.ToEntityReference();
        }
        _context.UpdateObject(documenturl);
        _context.SaveChanges();
        return _mapper.Map<DocumentResp>(documenturl);
    }

    private async SystemTask UploadFileAsync(SpdTempFile tempFile, Guid? applicationId, Guid? contactId, Guid? docUrlId, Bcgov_Tag? tag, CancellationToken ct, bool toTransientBucket = false, Guid? OrgRegistrationId = null)
    {

        if ((applicationId == null && contactId == null && OrgRegistrationId == null) || docUrlId == null) return;

        // choose folder 
        string folder = applicationId != null
            ? $"spd_application/{applicationId}"
            : contactId != null
                ? $"contact/{contactId}"
                : $"spd_orgregistration/{OrgRegistrationId}";

        // single FileTag 
        FileTag fileTag = tag == null
            ? new FileTag { Tags = new List<Tag> { new("file-classification", "Unclassified") } }
            : new FileTag { Tags = new List<Tag> { new("file-classification", "Unclassified"), new("file-tag", tag.Bcgov_Name) } };

        // pick storage service once
        IFileStorageService storageService = toTransientBucket
            ? (IFileStorageService)_transientFileStorageService
            : (IFileStorageService)_fileStorageService;

        await UploadFileContentAsync(storageService, docUrlId.Value, folder, fileTag, tempFile, ct);

    }

    private async SystemTask UploadFileContentAsync(IFileStorageService storageService, Guid docUrlId, string folder, FileTag fileTag, SpdTempFile tempFile, CancellationToken ct)
    {
        if (tempFile.TempFileKey != null)
        {
            byte[]? fileContent = await _tempFileService.HandleQuery(new GetTempFileQuery(tempFile.TempFileKey), ct);
            if (fileContent == null) return;

            _logger.LogInformation("Read File {FileName} from cache, size {FileSize} bytes", tempFile.FileName, fileContent.Length);

            var file = new Utilities.FileStorage.File
            {
                Content = fileContent,
                ContentType = tempFile.ContentType,
                FileName = tempFile.FileName,
            };

            var uploadFileCmd = new UploadFileCommand(
                Key: docUrlId.ToString(),
                Folder: folder,
                File: file,
                FileTag: fileTag);

            await storageService.HandleCommand(uploadFileCmd, ct);
        }
        else
        {
            var fileStream = new Utilities.FileStorage.FileContent
            {
                FileContentStream = System.IO.File.OpenRead(tempFile.TempFilePath),
                ContentType = tempFile.ContentType,
                FileName = tempFile.FileName,
            };

            var uploadStreamCmd = new UploadFileStreamCommand(
                Key: docUrlId.ToString(),
                Folder: folder,
                FileStream: fileStream,
                FileTag: fileTag);

            await storageService.HandleCommand(uploadStreamCmd, ct);
        }
    }

    private async SystemTask DeleteFileAsync(Guid docUrlId, Guid? applicationId, CancellationToken ct)
    {
        if (applicationId == null) return;
        await _transientFileStorageService.HandleDeleteCommand(new StorageDeleteCommand(
            Key: docUrlId.ToString(),
            Folder: $"spd_application/{applicationId}"), ct);
    }

    private async Task<DocumentResp> DocumentDeactivateAsync(DeactivateDocumentCmd cmd, CancellationToken ct)
    {
        Bcgov_DocumentUrl? documenturl = _context.Bcgov_DocumentUrlSet.Where(d => d.Bcgov_DocumentUrlId == cmd.DocumentUrlId).FirstOrDefault();
        if (documenturl == null) { return null; }
        documenturl.StateCode = Bcgov_DocumentUrl_StateCode.Inactive;
        documenturl.StatusCode = Bcgov_DocumentUrl_StatusCode.Inactive;
        _context.UpdateObject(documenturl);
        _context.SaveChanges();
        return _mapper.Map<DocumentResp>(documenturl);
    }
}
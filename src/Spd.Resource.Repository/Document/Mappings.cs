using AutoMapper;
using Spd.Resource.Repository.Application;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Tools;

namespace Spd.Resource.Repository.Document
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Bcgov_DocumentUrl, DocumentResp>()
            .ForMember(d => d.DocumentType, opt => opt.MapFrom(s => GetDocumentType(s)))
            .ForMember(d => d.DocumentType2, opt => opt.MapFrom(s => GetDocumentType2(s)))
            .ForMember(d => d.UploadedDateTime, opt => opt.MapFrom(s => s.Bcgov_ReceivedDate == null ? DateTimeOffset.MinValue : new DateTimeOffset(DateTime.SpecifyKind(s.Bcgov_ReceivedDate.Value, DateTimeKind.Utc))))
            .ForMember(d => d.CreatedOn, opt => opt.MapFrom(s => s.CreatedOn == null ? DateTimeOffset.MinValue : new DateTimeOffset(DateTime.SpecifyKind(s.CreatedOn.Value, DateTimeKind.Utc))))
            .ForMember(d => d.DocumentUrlId, opt => opt.MapFrom(s => s.Bcgov_DocumentUrlId ?? Guid.Empty))
            .ForMember(d => d.ClearanceId, opt => opt.MapFrom(s => s.Spd_ClearanceId != null ? s.Spd_ClearanceId.Id : (Guid?)null))
            .ForMember(d => d.CaseId, opt => opt.MapFrom(s => s.Bcgov_CaseId != null ? s.Bcgov_CaseId.Id : (Guid?)null))
            .ForMember(d => d.ApplicationId, opt => opt.MapFrom(s => s.Spd_ApplicationId != null ? s.Spd_ApplicationId.Id : (Guid?)null))
            .ForMember(d => d.ReportId, opt => opt.MapFrom(s => s.Spd_PdfReportId != null ? s.Spd_PdfReportId.Id : (Guid?)null))
            .ForMember(d => d.FileName, opt => opt.MapFrom(s => s.Bcgov_FileName))
            .ForMember(d => d.Folder, opt => opt.MapFrom(s => s.Bcgov_Url))
            .ForMember(d => d.LicenceId, opt => opt.MapFrom(s => s.Spd_LicenceId != null ? s.Spd_LicenceId.Id : (Guid?)null))
            .ForMember(d => d.PhotoSkipAdvancedBackgroundRemovel, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_SkipBackgroundRemoval)))
            .ForMember(d => d.PhotoSkipFacialDetection, opt => opt.MapFrom(s => SharedMappingFuncs.GetBool((int?)s.Spd_SkipFacialDetection)))
            .ForMember(d => d.FileExtension, opt => opt.MapFrom(s => FileHelper.GetFileExtensionWithoutDot(s.Bcgov_FileExtension)))
            .ForMember(d => d.ExpiryDate, opt => opt.Ignore())
            .ForMember(d => d.DocumentIdNumber, opt => opt.MapFrom(s => s.Spd_DocumentId));

            _ = CreateMap<SpdTempFile, Bcgov_DocumentUrl>()
            .ForMember(d => d.Bcgov_DocumentUrlId, opt => opt.MapFrom(s => Guid.NewGuid()))
            .ForMember(d => d.Bcgov_FileName, opt => opt.MapFrom(s => s.FileName))
            .ForMember(d => d.Bcgov_FileSize, opt => opt.MapFrom(s => $"{Math.Round((decimal)s.FileSize / 1024, 2)} KB"))
            .ForMember(d => d.Bcgov_OriginCode, opt => opt.MapFrom(s => Bcgov_OriginCode.Web))
            .ForMember(d => d.Bcgov_ReceivedDate, opt => opt.MapFrom(s => DateTime.UtcNow))
            .ForMember(d => d.Bcgov_FileExtension, opt => opt.MapFrom(s => FileHelper.GetFileExtension(s.FileName)));
        }

        private static DocumentTypeEnum? GetDocumentType(Bcgov_DocumentUrl documenturl)
        {
            string? docType = DynamicsContextLookupHelpers.BcGovTagDictionary
                .FirstOrDefault(t => t.Value == documenturl.Bcgov_Tag1Id?.Id).Key;
            if (docType == null) { return null; }
            return Enum.Parse<DocumentTypeEnum>(docType);
        }

        private static DocumentTypeEnum? GetDocumentType2(Bcgov_DocumentUrl documenturl)
        {
            string? docType = DynamicsContextLookupHelpers.BcGovTagDictionary
                .FirstOrDefault(t => t.Value == documenturl.Bcgov_Tag2Id?.Id).Key;
            if (docType == null) { return null; }
            return Enum.Parse<DocumentTypeEnum>(docType);
        }
    }
}

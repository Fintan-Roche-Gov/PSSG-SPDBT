using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.BizContact
{
    internal class BizContactRepository : IBizContactRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BizContactRepository> _logger;

        public BizContactRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
            IMapper mapper,
            ILogger<BizContactRepository> logger)
        {
            _context = ctx;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BizContactResp?> GetBizContactAsync(Guid bizContactId, CancellationToken ct, bool includeInactive = false)
        {
            Spd_BusinessContact? bizContact = _context.Spd_BusinessContactSet
                .Where(c => c.Spd_BusinessContactId == bizContactId)
                .FirstOrDefault();

            if (bizContact == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the business contact");
            if (!includeInactive && bizContact.StateCode == Spd_BusinessContact_StateCode.Inactive)
                return null;

            _context.LoadProperty(bizContact, nameof(Spd_BusinessContact.Spd_Position_Spd_BusinessContact));
            _context.LoadProperty(bizContact, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
            _context.LoadProperty(bizContact, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_PortalInvitation));

            return await Task.FromResult(_mapper.Map<BizContactResp>(bizContact));
        }

        public async Task<IEnumerable<BizContactResp>> QueryBizContactsAsync(BizContactQry qry, CancellationToken ct)
        {
            IQueryable<Spd_BusinessContact> bizContacts = _context.Spd_BusinessContactSet;

            if (!qry.IncludeInactive)
                bizContacts = bizContacts.Where(a => a.StateCode != Spd_BusinessContact_StateCode.Inactive);
            if (qry.BizId != null)
                bizContacts = bizContacts.Where(a => a.Spd_OrganizationId != null && a.Spd_OrganizationId.Id == qry.BizId);
            if (qry.RoleCode != null)
            {
                Spd_BusinessContact_Spd_Role roleOption = Enum.Parse<Spd_BusinessContact_Spd_Role>(qry.RoleCode.ToString());
                bizContacts = bizContacts.Where(a => a.Spd_Role == roleOption);
            }

            var list = bizContacts.ToList();
            foreach (Spd_BusinessContact item in list)
            {
                _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_Position_Spd_BusinessContact));
                _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
                _context.LoadProperty(item, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_PortalInvitation));
            }

            return await Task.FromResult(_mapper.Map<IEnumerable<BizContactResp>>(list));
        }

        public async Task<Guid?> ManageBizContactsAsync(BizContactCmd cmd, CancellationToken ct)
        {
            return cmd switch
            {
                BizContactCreateCmd c => await CreateBizContactAsync(c, ct),
                BizContactUpdateCmd c => await UpdateBizContactAsync(c, ct),
                BizContactDeleteCmd c => await DeleteBizContactAsync(c, ct),
                BizContactsLinkBizAppCmd c => await LinkBizContactsToBizApp(c.BizId, c.BizAppId, ct),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        private async Task<Guid?> LinkBizContactsToBizApp(Guid bizId, Guid bizAppId, CancellationToken ct)
        {
            List<Spd_BusinessContact> bizContacts = _context.Spd_BusinessContactSet
                .Where(c => c.Spd_OrganizationId != null && c.Spd_OrganizationId.Id == bizId)
                .ToList();

            Spd_Application? bizApp = _context.Spd_ApplicationSet.Where(a => a.Spd_ApplicationId == bizAppId).FirstOrDefault();
            if (bizApp == null)
                throw new ApiException(HttpStatusCode.BadRequest, "cannot find business application");

            foreach (Spd_BusinessContact bc in bizContacts)
            {
                _context.LoadProperty(bc, nameof(Spd_BusinessContact.Spd_BusinessContact_Spd_Application));
                bool linked = bc.Spd_BusinessContact_Spd_Application.Any(a => a.Spd_ApplicationId == bizAppId);

                if (bc.StateCode == Spd_BusinessContact_StateCode.Active && !linked)
                {
                    _context.AddLink(bc, new Relationship("spd_businesscontact_spd_application"), bizApp);
                }
                else if (bc.StateCode == Spd_BusinessContact_StateCode.Inactive && linked)
                {
                    _context.DeleteLink(bc, new Relationship("spd_businesscontact_spd_application"), bizApp);
                }
            }

            _context.SaveChanges();
            return await Task.FromResult<Guid?>(null);
        }

        private async Task<Guid?> CreateBizContactAsync(BizContactCreateCmd cmd, CancellationToken ct)
        {
            Account? biz = _context.AccountSet.Where(a => a.AccountId == cmd.BizContact.BizId).FirstOrDefault();
            if (biz == null)
                throw new ApiException(HttpStatusCode.BadRequest, $"account {cmd.BizContact.BizId} does not exist.");

            Spd_BusinessContact bizContact = _mapper.Map<Spd_BusinessContact>(cmd.BizContact);
            bizContact.Spd_BusinessContactId = Guid.NewGuid();

            if (cmd.BizContact.ContactId != null)
            {
                Spd.Utilities.Dataverse.EarlyBound.Contact? c = _context.ContactSet.Where(x => x.ContactId == cmd.BizContact.ContactId).FirstOrDefault();
                if (c == null)
                    throw new ApiException(HttpStatusCode.BadRequest, $"invalid contact {cmd.BizContact.ContactId.Value}");
                bizContact.Spd_ContactId = c.ToEntityReference();
            }

            if (cmd.BizContact.LicenceId != null)
            {
                Spd_Licence? swlLic = _context.Spd_LicenceSet
                    .Where(l => l.Spd_LicenceId == cmd.BizContact.LicenceId && l.StateCode == Spd_Licence_StateCode.Active)
                    .FirstOrDefault();
                Guid swlServiceTypeId = DataverseContextLookupHelpers.ServiceTypeGuidDictionary[ServiceTypeEnum.SecurityWorkerLicence.ToString()];
                if (swlLic == null || swlLic.Spd_LicenceType == null || swlLic.Spd_LicenceType.Id != swlServiceTypeId)
                    throw new ApiException(HttpStatusCode.BadRequest, $"invalid licence {cmd.BizContact.LicenceId}");
                bizContact.Spd_SwLNumber = swlLic.ToEntityReference();
            }

            bizContact.Spd_OrganizationId = biz.ToEntityReference();
            _context.AddObject(bizContact);
            _context.SaveChanges();
            return await Task.FromResult(bizContact.Spd_BusinessContactId);
        }

        private async Task<Guid?> DeleteBizContactAsync(BizContactDeleteCmd cmd, CancellationToken ct)
        {
            Spd_BusinessContact? bizContact = _context.Spd_BusinessContactSet.Where(x => x.Spd_BusinessContactId == cmd.BizContactId).FirstOrDefault();

            if (bizContact == null)
                throw new ApiException(HttpStatusCode.BadRequest, $"business contact with id {cmd.BizContactId} not found.");

            Guid? cmServiceType = DataverseContextLookupHelpers.GetServiceTypeGuid(ServiceTypeEnum.SECURITY_BUSINESS_LICENCE_CONTROLLING_MEMBER_CRC.ToString());
            Spd_Application? app = null;

            List<Spd_Application> cmApps = _context.Spd_ApplicationSet
                .Where(x => x.Spd_ServiceTypeId != null && x.Spd_ServiceTypeId.Id == cmServiceType)
                .ToList();

            foreach (Spd_Application candidate in cmApps)
            {
                _context.LoadProperty(candidate, nameof(Spd_Application.Spd_BusinessContact_Spd_Application));
                if (candidate.Spd_BusinessContact_Spd_Application.Any(y => y.Spd_BusinessContactId == cmd.BizContactId))
                {
                    app = candidate;
                    break;
                }
            }

            if (app != null &&
                (app.StatusCode == Spd_Application_StatusCode.Incomplete ||
                 app.StatusCode == Spd_Application_StatusCode.Draft ||
                 app.StatusCode == Spd_Application_StatusCode.PaymentPending ||
                 app.StatusCode == Spd_Application_StatusCode.ApplicantVerification))
            {
                app.StateCode = Spd_Application_StateCode.Inactive;
                app.StatusCode = Spd_Application_StatusCode.Cancelled;
                _context.UpdateObject(app);
            }

            bizContact.StateCode = Spd_BusinessContact_StateCode.Inactive;
            bizContact.StatusCode = Spd_BusinessContact_StatusCode.Inactive;
            _context.UpdateObject(bizContact);

            _context.SaveChanges();
            return await Task.FromResult<Guid?>(null);
        }

        private async Task<Guid?> UpdateBizContactAsync(BizContactUpdateCmd cmd, CancellationToken ct)
        {
            Spd_BusinessContact? bizContact = _context.Spd_BusinessContactSet.Where(x => x.Spd_BusinessContactId == cmd.BizContactId).FirstOrDefault();
            if (bizContact == null) throw new ApiException(HttpStatusCode.BadRequest, "Cannot find the member.");
            if (bizContact.Spd_Role != Spd_BusinessContact_Spd_Role.ControllingMember && bizContact.Spd_Role != Spd_BusinessContact_Spd_Role.BusinessManager)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot update non-stakeholder.");
            if (bizContact.Spd_SwLNumber != null)
                throw new ApiException(HttpStatusCode.BadRequest, "Cannot update stakeholder with secure worker licence.");
            _mapper.Map<BizContact, Spd_BusinessContact>(cmd.BizContact, bizContact);
            _context.UpdateObject(bizContact);
            _context.SaveChanges();
            return await Task.FromResult(cmd.BizContactId);
        }
    }
}

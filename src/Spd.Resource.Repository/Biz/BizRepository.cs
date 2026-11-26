using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Shared.Exceptions;
using System.Collections.Immutable;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Spd.Resource.Repository.Biz
{
    internal class BizRepository : IBizRepository
    {
        private readonly Spd.Utilities.Dataverse.EarlyBound.DataverseContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BizRepository> _logger;
        private readonly List<BizTypeEnum> soleProprietorTypes = new()
        {
            BizTypeEnum.RegisteredSoleProprietor,
            BizTypeEnum.NonRegisteredSoleProprietor
        };

        public BizRepository(Spd.Utilities.Dataverse.EarlyBound.DataverseContext ctx,
            IMapper mapper,
            ILogger<BizRepository> logger)
        {
            _context = ctx;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<BizResult>> QueryBizAsync(BizsQry qry, CancellationToken ct)
        {
            IQueryable<Account> accounts = _context.AccountSet;

            if (!qry.IncludeInactive)
            {
                accounts = accounts.Where(a => a.StateCode != Account_StateCode.Inactive);
            }

            if (qry.BizGuid != null)
            {
                accounts = accounts.Where(a => a.Spd_OrgGuid == qry.BizGuid.ToString());
            }

            if (qry.BizCode != null)
            {
                accounts = accounts.Where(a => a.Spd_OrgCode == qry.BizCode);
            }

            var accountList = accounts.ToList();

            if (qry.ServiceTypes != null && qry.ServiceTypes.Any())
            {
                IEnumerable<Guid> stIds = qry.ServiceTypes
                    .Select(t => DataverseContextLookupHelpers.ServiceTypeGuidDictionary.GetValueOrDefault(t.ToString()))
                    .Where(g => g != Guid.Empty);

                accountList = accountList.Where(a =>
                {
                    _context.LoadProperty(a, nameof(Account.Spd_Account_Spd_ServiceType));
                    return stIds.Any(t => a.Spd_Account_Spd_ServiceType.Any(st => st.Spd_ServiceTypeId == t));
                }).ToList();
            }

            return await Task.FromResult(_mapper.Map<IEnumerable<BizResult>>(accountList));
        }

        public async Task<BizResult?> GetBizAsync(Guid accountId, CancellationToken ct, bool includeMainOffice = false)
        {
            Account? biz = _context.AccountSet
                .Where(a => a.StateCode != Account_StateCode.Inactive)
                .Where(a => a.AccountId == accountId)
                .FirstOrDefault();

            if (biz == null) throw new ApiException(HttpStatusCode.NotFound);

            _context.LoadProperty(biz, nameof(Account.Spd_Organization_Addresses));
            _context.LoadProperty(biz, nameof(Account.Spd_Organization_Spd_Licence_SoleProprietor));
            _context.LoadProperty(biz, nameof(Account.Spd_Account_Spd_ServiceType));

            Spd_Licence? swl = biz.Spd_Organization_Spd_Licence_SoleProprietor
                .Where(a => a.StateCode == Spd_Licence_StateCode.Active)
                .OrderByDescending(a => a.CreatedOn)
                .FirstOrDefault();

            var response = _mapper.Map<BizResult>(biz, opt =>
            {
                opt.Items["includeMainOffice"] = includeMainOffice;
            });

            response.SoleProprietorSwlContactInfo.LicenceId = swl?.Spd_LicenceId;
            response.SoleProprietorSwlExpiryDate = SharedMappingFuncs.GetDateOnlyFromDateTimeOffset(swl?.Spd_ExpiryDate);
            return await Task.FromResult(response);
        }

        public async Task<BizResult?> ManageBizAsync(BizCmd cmd, CancellationToken ct)
        {
            return cmd switch
            {
                UpdateBizCmd c => await UpdateBizAsync(c, ct),
                CreateBizCmd c => await CreateBizAsync(c, ct),
                MergeBizsCmd c => await MergeBizAsync(c, ct),
                AddBizServiceTypeCmd c => await AddBizServiceTypeAsync(c, ct),
                UpdateBizServiceTypeCmd c => await UpdateBizServiceTypeAsync(c, ct),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        private async Task<BizResult?> UpdateBizAsync(UpdateBizCmd updateBizCmd, CancellationToken ct)
        {
            Account? biz = _context.AccountSet
                .Where(a => a.StateCode != Account_StateCode.Inactive)
                .Where(a => a.AccountId == updateBizCmd.Id)
                .FirstOrDefault();

            if (biz == null) throw new ApiException(HttpStatusCode.NotFound);

            _context.LoadProperty(biz, nameof(Account.Spd_Organization_Addresses));
            _context.LoadProperty(biz, nameof(Account.Spd_Organization_Spd_Licence_SoleProprietor));

            _mapper.Map(updateBizCmd, biz);

            _context.UpdateObject(biz);
            if (updateBizCmd.UpdateSoleProprietor && IsSoleProprietor(updateBizCmd.BizType))
            {
                UpdateSPLicenceLink(biz, updateBizCmd.SoleProprietorSwlContactInfo?.LicenceId, updateBizCmd.BizType);
                await UpdateSPBizContact(biz, updateBizCmd.SoleProprietorSwlContactInfo?.LicenceId, ct);
            }

            _context.SaveChanges();
            return await Task.FromResult(_mapper.Map<BizResult>(biz));
        }

        private async Task<BizResult?> UpdateBizServiceTypeAsync(UpdateBizServiceTypeCmd updateBizServiceTypeCmd, CancellationToken ct)
        {
            var serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(updateBizServiceTypeCmd.ServiceTypeEnum.ToString());
            if (serviceTypeGuid == null)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "cannot find the service type");
            }

            Spd_ServiceType? st = _context.Spd_ServiceTypeSet
                .Where(s => s.Spd_ServiceTypeId == serviceTypeGuid)
                .FirstOrDefault();

            Account? biz = _context.AccountSet
                .Where(a => a.StateCode != Account_StateCode.Inactive)
                .Where(a => a.AccountId == updateBizServiceTypeCmd.BizId)
                .FirstOrDefault();

            if (biz == null)
                throw new ApiException(HttpStatusCode.BadRequest, "cannot find the biz");

            _context.LoadProperty(biz, nameof(Account.Spd_Account_Spd_ServiceType));

            if (st != null && !biz.Spd_Account_Spd_ServiceType.Any(s => s.Spd_ServiceTypeId == st.Spd_ServiceTypeId))
            {
                _context.AddLink(biz, new Relationship("spd_account_spd_servicetype"), st);
            }

            foreach (Spd_ServiceType serviceType in biz.Spd_Account_Spd_ServiceType)
            {
                var serviceTypeCode = DataverseContextLookupHelpers.GetServiceTypeName(serviceType.Spd_ServiceTypeId);
                if (updateBizServiceTypeCmd.ServiceTypeEnum.ToString() != serviceTypeCode)
                {
                    _context.DeleteLink(biz, new Relationship("spd_account_spd_servicetype"), serviceType);
                }
            }

            _context.SaveChanges();
            return await GetBizAsync(updateBizServiceTypeCmd.BizId, ct);
        }

        private async Task<BizResult?> CreateBizAsync(CreateBizCmd createBizCmd, CancellationToken ct)
        {
            var account = _mapper.Map<Account>(createBizCmd);
            _context.AddObject(account);

            foreach (ServiceTypeEnum serviceType in createBizCmd.ServiceTypes)
            {
                var serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(serviceType.ToString());
                if (serviceTypeGuid == null)
                {
                    continue;
                }

                Spd_ServiceType? st = _context.Spd_ServiceTypeSet
                    .Where(s => s.Spd_ServiceTypeId == serviceTypeGuid)
                    .FirstOrDefault();

                if (st != null)
                {
                    _context.AddLink(account, new Relationship("spd_account_spd_servicetype"), st);
                }
            }

            _context.SaveChanges();
            return await Task.FromResult(_mapper.Map<BizResult>(account));
        }

        private async Task<BizResult?> MergeBizAsync(MergeBizsCmd mergeBizCmd, CancellationToken ct)
        {
            _logger.LogError("MergeBizAsync is not yet implemented for EarlyBound DataverseContext in BizRepository.");
            throw new NotSupportedException("MergeBizAsync is not implemented for EarlyBound DataverseContext.");
        }

        private async Task<BizResult?> AddBizServiceTypeAsync(AddBizServiceTypeCmd addBizServiceTypeCmd, CancellationToken ct)
        {
            var serviceTypeGuid = DataverseContextLookupHelpers.GetServiceTypeGuid(addBizServiceTypeCmd.ServiceTypeEnum.ToString());
            if (serviceTypeGuid == null)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "cannot find the service type");
            }

            Spd_ServiceType? st = _context.Spd_ServiceTypeSet
                .Where(s => s.Spd_ServiceTypeId == serviceTypeGuid)
                .FirstOrDefault();

            Account? biz = _context.AccountSet
                .Where(a => a.StateCode != Account_StateCode.Inactive)
                .Where(a => a.AccountId == addBizServiceTypeCmd.BizId)
                .FirstOrDefault();

            if (biz == null)
                throw new ApiException(HttpStatusCode.BadRequest, "cannot find the biz");

            _context.LoadProperty(biz, nameof(Account.Spd_Account_Spd_ServiceType));

            if (st != null && !biz.Spd_Account_Spd_ServiceType.Any(s => s.Spd_ServiceTypeId == st.Spd_ServiceTypeId))
            {
                _context.AddLink(biz, new Relationship("spd_account_spd_servicetype"), st);
                _context.SaveChanges();
            }

            return await GetBizAsync(addBizServiceTypeCmd.BizId, ct);
        }

        private void UpdateSPLicenceLink(Account account, Guid? licenceId, BizTypeEnum? bizType)
        {
            Spd_Licence? existingLicence = account.Spd_Organization_Spd_Licence_SoleProprietor
                .FirstOrDefault(a => a.StateCode == Spd_Licence_StateCode.Active);

            if (existingLicence != null && existingLicence.Spd_LicenceId == licenceId && IsSoleProprietor(bizType))
                return;
            if (!IsSoleProprietor(bizType) && IsSoleProprietor(SharedMappingFuncs.GetBizTypeEnum((int?)account.Spd_LicensingBusinessType)))
                throw new ApiException(HttpStatusCode.BadRequest, "Biz type can only be changed from sole proprietor to non-sole proprietor");
            if (!IsSoleProprietor(bizType))
                return;

            if (existingLicence != null)
            {
                _context.DeleteLink(account, new Relationship("spd_organization_spd_licence_soleproprietor"), existingLicence);
            }

            Spd_Licence? newLicence = _context.Spd_LicenceSet
                .Where(l => l.Spd_LicenceId == licenceId)
                .Where(l => l.StateCode == Spd_Licence_StateCode.Active)
                .FirstOrDefault();

            if (newLicence != null)
            {
                _context.AddLink(account, new Relationship("spd_organization_spd_licence_soleproprietor"), newLicence);
            }
        }

        private Task UpdateSPBizContact(Account account, Guid? licenceId, CancellationToken ct)
        {
            if (licenceId == null) return Task.CompletedTask;

            IQueryable<Spd_BusinessContact> bizExistingContacts = _context.Spd_BusinessContactSet
                .Where(b => b.Spd_OrganizationId != null && b.Spd_OrganizationId.Id == account.AccountId)
                .Where(a => a.StateCode != Spd_BusinessContact_StateCode.Inactive);

            Spd_BusinessContact? validSpContact = bizExistingContacts.FirstOrDefault(
                c => c.Spd_Role == Spd_BusinessContact_Spd_Role.ControllingMember && c.Spd_SwLNumber != null && c.Spd_SwLNumber.Id == licenceId
                );

            if (validSpContact == null)
            {
                Spd_Licence? newLicence = _context.Spd_LicenceSet
                    .Where(l => l.Spd_LicenceId == licenceId)
                    .Where(l => l.StateCode == Spd_Licence_StateCode.Active)
                    .FirstOrDefault();

                if (newLicence == null)
                {
                    return Task.CompletedTask;
                }

                _context.LoadProperty(newLicence, nameof(Spd_Licence.Spd_Contact_Spd_Licence_LicenceHolder));
                Spd.Utilities.Dataverse.EarlyBound.Contact? contact = newLicence.Spd_Contact_Spd_Licence_LicenceHolder;
                if (contact == null)
                {
                    return Task.CompletedTask;
                }

                Spd_BusinessContact bizContact = new Spd_BusinessContact
                {
                    Spd_BusinessContactId = Guid.NewGuid(),
                    Spd_Role = Spd_BusinessContact_Spd_Role.ControllingMember,
                    Spd_ContactId = contact.ToEntityReference(),
                    Spd_OrganizationId = account.ToEntityReference(),
                    Spd_SwLNumber = newLicence.ToEntityReference()
                };

                _context.AddObject(bizContact);
            }

            return Task.CompletedTask;
        }

        private bool IsSoleProprietor(BizTypeEnum? bizType)
        {
            if (bizType == null) return false;

            return soleProprietorTypes.Contains((BizTypeEnum)bizType);
        }
    }
}

using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;

namespace Spd.Resource.Repository.Application;
internal partial class ApplicationRepository : IApplicationRepository
{
    public async Task<ApplicationStatisticsResp> QueryApplicationStatisticsAsync(ApplicationStatisticsQry query, CancellationToken ct)
    {
        if (query.OrganizationId != null)
        {
            var organization = await _context.GetOrgById((Guid)query.OrganizationId, ct);
            if (organization == null) return new ApplicationStatisticsResp();
            var organizationStatistics = (Spd_GetPSSOOrganizationStatisticsResponse)await _organizationService.ExecuteAsync(
                new Spd_GetPSSOOrganizationStatisticsRequest
                {
                    Target = organization.ToEntityReference()
                },
                ct);            
            var statisticsDictionary = new Dictionary<ApplicationPortalStatisticsCd, int>();
            if (organizationStatistics.AwaitingApplicant != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.AwaitingApplicant, organizationStatistics.AwaitingApplicant);
            if (organizationStatistics.AwaitingPayment != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.AwaitingPayment, organizationStatistics.AwaitingPayment);
            if (organizationStatistics.AwaitingThirdParty != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.AwaitingThirdParty, organizationStatistics.AwaitingThirdParty);
            if (organizationStatistics.CancelledByApplicant != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.CancelledByApplicant, organizationStatistics.CancelledByApplicant);
            if (organizationStatistics.ClosedNoConsent != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.ClosedNoConsent, organizationStatistics.ClosedNoConsent);
            if (organizationStatistics.ClosedNoResponse != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.ClosedNoResponse, organizationStatistics.ClosedNoResponse);
            if (organizationStatistics.Incomplete != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.Incomplete, organizationStatistics.Incomplete);
            if (organizationStatistics.InProgress != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.InProgress, organizationStatistics.InProgress);
            if (organizationStatistics.RiskFound != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.RiskFound, organizationStatistics.RiskFound);
            if (organizationStatistics.Underassessment != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.UnderAssessment, organizationStatistics.Underassessment);
            if (organizationStatistics.VerifyIdentity != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.VerifyIdentity, organizationStatistics.VerifyIdentity);
            // To do fix mapping
            //if (organizationStatistics.ClearedLastSevenDays != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.ClearedLastSevenDays, organizationStatistics.ClearedLastSevenDays);
            //if (organizationStatistics.NotClearedLastSevenDays != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.NotClearedLastSevenDays, organizationStatistics.NotClearedLastSevenDays);

            return new ApplicationStatisticsResp { Statistics = statisticsDictionary };
        }
        if (query.DelegateUserId != null)
        {
            var user = await _context.GetUserById((Guid)query.DelegateUserId, ct);
            if (user == null) return new ApplicationStatisticsResp();
            var pssoUserStatistics = (Spd_GetPSSOOrganizationStatisticsResponse)await _organizationService.ExecuteAsync(
                new Spd_GetPSSOOrganizationStatisticsRequest
                {
                    Target = user.ToEntityReference()
                },
                ct);
            var statisticsDictionary = new Dictionary<ApplicationPortalStatisticsCd, int>();
            if (pssoUserStatistics.AwaitingApplicant != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.AwaitingApplicant, pssoUserStatistics.AwaitingApplicant);
            if (pssoUserStatistics.AwaitingPayment != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.AwaitingPayment, pssoUserStatistics.AwaitingPayment);
            if (pssoUserStatistics.AwaitingThirdParty != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.AwaitingThirdParty, pssoUserStatistics.AwaitingThirdParty);
            if (pssoUserStatistics.CancelledByApplicant != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.CancelledByApplicant, pssoUserStatistics.CancelledByApplicant);
            if (pssoUserStatistics.ClosedNoConsent != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.ClosedNoConsent, pssoUserStatistics.ClosedNoConsent);
            if (pssoUserStatistics.ClosedNoResponse != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.ClosedNoResponse, pssoUserStatistics.ClosedNoResponse);
            if (pssoUserStatistics.Incomplete != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.Incomplete, pssoUserStatistics.Incomplete);
            if (pssoUserStatistics.InProgress != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.InProgress, pssoUserStatistics.InProgress);
            if (pssoUserStatistics.RiskFound != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.RiskFound, pssoUserStatistics.RiskFound);
            if (pssoUserStatistics.Underassessment != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.UnderAssessment, pssoUserStatistics.Underassessment);
            if (pssoUserStatistics.VerifyIdentity != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.VerifyIdentity, pssoUserStatistics.VerifyIdentity);
            if (pssoUserStatistics.Completed != null) statisticsDictionary.Add(ApplicationPortalStatisticsCd.Completed, pssoUserStatistics.Completed);
            return new ApplicationStatisticsResp { Statistics = statisticsDictionary };
        }
        return null;
    }


}



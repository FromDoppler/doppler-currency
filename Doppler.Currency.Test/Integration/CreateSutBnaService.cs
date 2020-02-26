using System.Net.Http;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Logger;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Moq;

namespace Doppler.Currency.Test.Integration
{
    public static class CreateSutBnaService
    {
        public static BnaService CreateSut(
            IHttpClientFactory httpClientFactory = null,
            HttpClientPoliciesSettings httpClientPoliciesSettings = null,
            BnaSettings bnaSettings = null,
            ISlackHooksService slackHooksService = null,
            ILoggerAdapter<BnaService> logger = null)
        {
            return new BnaService(
                httpClientFactory ?? Mock.Of<IHttpClientFactory>(),
                httpClientPoliciesSettings ?? new HttpClientPoliciesSettings(),
                bnaSettings ?? new BnaSettings(),
                slackHooksService ?? Mock.Of<ISlackHooksService>(),
                logger ?? Mock.Of<ILoggerAdapter<BnaService>>());
        }
    }
}

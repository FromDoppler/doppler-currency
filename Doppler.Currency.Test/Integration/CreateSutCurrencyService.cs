using System.Collections.Generic;
using System.Net.Http;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Logger;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace Doppler.Currency.Test.Integration
{
    public static class CreateSutCurrencyService
    {
        public static CurrencyService CreateSut(
            IHttpClientFactory httpClientFactory = null,
            HttpClientPoliciesSettings httpClientPoliciesSettings = null,
            IOptionsMonitor<UsdCurrencySettings> bnaSettings = null,
            ISlackHooksService slackHooksService = null,
            ILoggerAdapter<CurrencyService> logger = null,
            ILoggerAdapter<BnaHandler> loggerBna = null,
            ILoggerAdapter<DofHandler> loggerDof = null)
        {
            var bnaHandler = new BnaHandler(
                httpClientFactory,
                httpClientPoliciesSettings,
                bnaSettings,
                slackHooksService,
                loggerBna ?? Mock.Of<ILoggerAdapter<BnaHandler>>());

            var dofHandler = new DofHandler(
                httpClientFactory,
                httpClientPoliciesSettings,
                bnaSettings,
                slackHooksService,
                loggerDof ?? Mock.Of<ILoggerAdapter<DofHandler>>());

            var handler = new Dictionary<CurrencyType, ICurrencyHandler>
            {
                { CurrencyType.Arg, bnaHandler },
                { CurrencyType.Mex, dofHandler }
            };

            return new CurrencyService(
                logger ?? Mock.Of<ILoggerAdapter<CurrencyService>>(),
                handler);
        }
    }
}

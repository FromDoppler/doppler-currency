using System.Collections.Generic;
using System.Net.Http;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Enums;
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
            IOptionsMonitor<CurrencySettings> bnaSettings = null,
            ISlackHooksService slackHooksService = null,
            ILoggerAdapter<CurrencyHandler> loggerHandler = null,
            ILoggerAdapter<CurrencyService> loggerService = null,
            ILoggerAdapter<DofHandler> loggerDof = null)
        {
            var bnaHandler = new BnaHandler(
                httpClientFactory,
                httpClientPoliciesSettings,
                bnaSettings,
                slackHooksService,
                loggerHandler ?? Mock.Of<ILoggerAdapter<CurrencyHandler>>());

            var dofHandler = new DofHandler(
                httpClientFactory,
                httpClientPoliciesSettings,
                bnaSettings,
                slackHooksService,
                loggerHandler ?? Mock.Of<ILoggerAdapter<CurrencyHandler>>());

            var handler = new Dictionary<CurrencyCodeEnum, CurrencyHandler>
            {
                { CurrencyCodeEnum.Ars, bnaHandler },
                { CurrencyCodeEnum.Mxn, dofHandler }
            };

            return new CurrencyService(
                loggerService ?? Mock.Of<ILoggerAdapter<CurrencyService>>(),
                handler);
        }
    }
}

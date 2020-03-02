using System;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Dtos;
using Doppler.Currency.Logger;
using Doppler.Currency.Settings;

namespace Doppler.Currency.Services
{
    public abstract class CurrencyHandler
    {
        protected readonly HttpClient HttpClient;
        protected readonly UsdCurrencySettings ServiceSettings;
        protected readonly ISlackHooksService SlackHooksService;
        protected readonly ILoggerAdapter<CurrencyHandler> Logger;

        protected CurrencyHandler(
            HttpClient httpClient,
            UsdCurrencySettings serviceSettings,
            ISlackHooksService slackHooksService, 
            ILoggerAdapter<CurrencyHandler> logger)
        {
            HttpClient = httpClient;
            ServiceSettings = serviceSettings;
            SlackHooksService = slackHooksService;
            Logger = logger;
        }

        public abstract Task<EntityOperationResult<UsdCurrency>> Handle(DateTime date);
    }
}

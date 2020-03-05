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

        protected async Task SendSlackNotification(
            string htmlPage,
            DateTime dateTime,
            CurrencyType countryCode,
            Exception e = null)
        {
            Logger.LogError(e ?? new Exception("Error getting HTML"),
                $"Error getting HTML, title is not valid, please check HTML: {htmlPage}");
            await SlackHooksService.SendNotification(HttpClient, $"Can't get the USD currency from {countryCode} code country, please check Html in the log or if the date is holiday {dateTime}");
        }
    }
}

using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;
using Doppler.Currency.Settings;
using Microsoft.Extensions.Logging;

namespace Doppler.Currency.Services
{
    public abstract class CurrencyHandler
    {
        protected readonly IHttpClientFactory HttpClientFactory;
        protected readonly CurrencySettings ServiceSettings;
        protected readonly ISlackHooksService SlackHooksService;
        protected readonly ILogger<CurrencyHandler> Logger;
        protected readonly HttpClientPoliciesSettings HttpClientPoliciesSettings;

        protected CurrencyHandler(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings httpClientPoliciesSettings,
            CurrencySettings serviceSettings,
            ISlackHooksService slackHooksService, 
            ILogger<CurrencyHandler> logger)
        {
            HttpClientFactory = httpClientFactory;
            HttpClientPoliciesSettings = httpClientPoliciesSettings;
            ServiceSettings = serviceSettings;
            SlackHooksService = slackHooksService;
            Logger = logger;
        }

        public abstract Task<EntityOperationResult<CurrencyDto>> Handle(DateTime date);

        protected async Task SendSlackNotification(
            string htmlPage,
            DateTime dateTime,
            CurrencyCodeEnum currencyCode,
            Exception e = null)
        {
            Logger.LogError(e ?? new Exception("Error getting HTML"), "Error getting HTML, title is not valid, please check HTML: {Page}", htmlPage);
            await SlackHooksService.SendNotification($"Can't get currency from {currencyCode} currency code, please check Html in the log or if the date is holiday {dateTime.ToUniversalTime():yyyy-MM-dd}");
        }

        protected async Task SendNotificationToSlack(string message)
        {
            await SlackHooksService.SendNotification(message);
        }

        protected CurrencyDto CreateCurrency(
            DateTime date,
            string sale,
            string currencyCode,
            string buy = null)
        {
            var cultureInfo = CultureInfo.CreateSpecificCulture("es-AR");
            var saleDecimal = Convert.ToDecimal(sale, cultureInfo);
            var buyDecimal = Convert.ToDecimal(buy, cultureInfo);

            return new CurrencyDto
            {
                Date = $"{date.ToUniversalTime():yyyy-MM-dd}",
                SaleValue = saleDecimal == 0 ? (decimal?)null : saleDecimal,
                BuyValue = buyDecimal == 0 ? (decimal?) null : buyDecimal,
                CurrencyName = ServiceSettings.CurrencyName,
                CurrencyCode = currencyCode.ToUpper()
            };
        }
    }
}

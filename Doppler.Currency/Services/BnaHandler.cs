using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CrossCutting;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;
using Doppler.Currency.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.Currency.Services
{
    public class BnaHandler : CurrencyHandler
    {
        public BnaHandler(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings bnaClientPoliciesSettings,
            IOptionsMonitor<CurrencySettings> bnaSettings,
            ISlackHooksService slackHooksService,
            ILogger<CurrencyHandler> logger) : base(httpClientFactory, bnaClientPoliciesSettings,
            bnaSettings.Get("BnaService"), slackHooksService, logger)
        {
        }

        public override async Task<EntityOperationResult<CurrencyDto>> Handle(DateTime date)
        {
            Logger.LogInformation("building url to get html data.");
            var dateUrl = System.Web.HttpUtility.UrlEncode($"{date:dd/MM/yyyy}");

            var uri = new Uri(ServiceSettings.Url + "&fecha=" + dateUrl);

            Logger.LogInformation("Building http request with url {uri}", uri);
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("GET")
            };

            Logger.LogInformation("Sending request to Bna server.");
            var client = HttpClientFactory.CreateClient();
            var httpResponse = await client.SendAsync(httpRequest).ConfigureAwait(false);

            Logger.LogInformation("Getting Html content of the Bna.");
            var htmlPage = await httpResponse.Content.ReadAsStringAsync();

            return await GetDataFromHtmlAsync(htmlPage, date);
        }

        private async Task<EntityOperationResult<CurrencyDto>> GetDataFromHtmlAsync(string htmlPage, DateTime date)
        {
            var result = new EntityOperationResult<CurrencyDto>();
            var parser = new HtmlParser();
            var document = parser.ParseDocument(htmlPage);

            if (document.GetElementsByClassName("sinResultados").Any())
            {
                Logger.LogInformation("Not exist currency USD for date {date}.", date);
                result.AddError("No USD for this date", ServiceSettings.NoCurrency);
                return result;
            }

            var titleValidation = document.GetElementsByTagName("tr").ElementAtOrDefault(1);
            if (titleValidation == null)
            {
                await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Ars);
                result.AddError("Html error", $"Error getting HTML, not exist currency USD. Check Date {date.ToUniversalTime().ToShortDateString()}.");
                return result;
            }

            var tableRows = document.QuerySelectorAll("table > tbody > tr");
            var usdCurrency = GetCurrencyByDate(tableRows, date);

            if (usdCurrency == null)
            {
                Logger.LogError(new Exception("Error getting HTML"),
                        "Error getting HTML, please check is holiday : {htmlPage}", htmlPage);
                result.AddError("Holiday Error", "Error getting date is holiday, please check Bna page.");

                return result;
            }

            var columns = usdCurrency.GetElementsByTagName("td");
            var buyColumn = columns.ElementAtOrDefault(1);
            var saleColumn = columns.ElementAtOrDefault(2);
            var dateColumn = columns.ElementAtOrDefault(3);

            if (buyColumn != null && saleColumn != null && dateColumn != null)
            {
                Logger.LogInformation("Creating Currency object to returned to the client.");
                result.Entity = CreateCurrency(date, saleColumn.InnerHtml, ServiceSettings.CurrencyCode, buyColumn.InnerHtml);

                return result;
            }

            await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Ars);
            result.AddError("Error Bna", "Error getting currency, please check HTML.");
            return result;
        }

        private static IElement GetCurrencyByDate(IEnumerable<IElement> htmlData, DateTime dateTime)
        {
            foreach (var node in htmlData)
            {
                var date = node.GetElementsByTagName("td").ElementAtOrDefault(3);

                if (date != null && date.InnerHtml.Equals($"{dateTime:d/M/yyyy}"))
                    return node;
            }

            return null;
        }
    }
}

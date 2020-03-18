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
using Doppler.Currency.Logger;
using Doppler.Currency.Settings;
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
            ILoggerAdapter<CurrencyHandler> logger) : base(httpClientFactory.CreateClient(bnaClientPoliciesSettings.ClientName),
            bnaSettings.Get("BnaService"), slackHooksService, logger)
        {
        }

        public override async Task<EntityOperationResult<CurrencyDto>> Handle(DateTime date)
        {
            // Construct URL
            Logger.LogInformation("building url to get html data.");
            var dateUrl = System.Web.HttpUtility.UrlEncode($"{date:dd/MM/yyyy}");

            var uri = new Uri(ServiceSettings.Url + "&fecha=" + dateUrl);

            Logger.LogInformation($"Building http request with url {uri}");
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("GET")
            };

            Logger.LogInformation("Sending request to Bna server.");
            var httpResponse = await HttpClient.SendAsync(httpRequest).ConfigureAwait(false);

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
                Logger.LogInformation($"Does not exist currency USD for date {date}");
                result.AddError("No USD for this date", ServiceSettings.NoCurrency);
                return result;
            }

            var titleValidation = document.GetElementsByTagName("tr").ElementAtOrDefault(1);
            if (titleValidation == null)
            {
                await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Ars);
                result.AddError("Html Error Bna", $"Error getting HTML, not exist currency USD. Check Date {date.ToUniversalTime().ToShortDateString()}.");
                return result;
            }

            var titleText = titleValidation.GetElementsByTagName("td").ElementAtOrDefault(0);
            if (titleText != null && !titleText.InnerHtml.Equals(ServiceSettings.ValidationHtml))
            {
                await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Ars);
                result.AddError("Html Error Bna", $"Error getting HTML, not exist currency USD. Check date {date.ToUniversalTime().ToShortDateString()}.");
                return result;
            }

            var tableRows = document.QuerySelectorAll("table > tbody > tr");
            var usdCurrency = GetCurrencyByDate(tableRows, date);

            if (usdCurrency == null)
            {
                Logger.LogError(new Exception("Error getting HTML"),
                        $"Error getting HTML, please check HTML and date is holiday : {htmlPage}");
                result.AddError("Html Error Bna", "Error getting HTML or date is holiday, please check Bna page.");
                return result;
            }

            var columns = usdCurrency.GetElementsByTagName("td");
            var buyColumn = columns.ElementAtOrDefault(1);
            var saleColumn = columns.ElementAtOrDefault(2);
            var dateColumn = columns.ElementAtOrDefault(3);

            if (buyColumn != null && saleColumn != null && dateColumn != null)
            {
                Logger.LogInformation("Creating Currency object to returned to the client.");
                return CreateCurrency(date, saleColumn.InnerHtml, buyColumn.InnerHtml);
            }

            await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Ars);
            result.AddError("Html Error Bna", "Error getting HTML, please check HTML.");
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

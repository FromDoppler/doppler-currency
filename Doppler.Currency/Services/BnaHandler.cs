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
            IOptionsMonitor<UsdCurrencySettings> bnaSettings,
            ISlackHooksService slackHooksService,
            ILoggerAdapter<CurrencyHandler> logger) : base(httpClientFactory.CreateClient(bnaClientPoliciesSettings.ClientName),
            bnaSettings.Get("BnaService"), slackHooksService, logger)
        {
        }

        public override async Task<EntityOperationResult<UsdCurrency>> Handle(DateTime date)
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

        private async Task<EntityOperationResult<UsdCurrency>> GetDataFromHtmlAsync(
            string htmlPage,
            DateTime dateTime)
        {
            var result = new EntityOperationResult<UsdCurrency>();
            var parser = new HtmlParser();
            var document = parser.ParseDocument(htmlPage);

            if (document.GetElementsByClassName("sinResultados").Any())
            {
                Logger.LogInformation($"Does not exist currency USD for date {dateTime}");
                result.AddError("No USD for this date", ServiceSettings.NoCurrency);
                return result;
            }

            var titleValidation = document.GetElementsByTagName("tr").ElementAtOrDefault(1);
            if (titleValidation == null)
            {
                await SendSlackNotification(htmlPage, dateTime, CurrencyType.Arg);
                result.AddError("Html Error Bna", $"Error getting HTML, currently does not exist currency USD. Check Date {dateTime.ToShortDateString()}.");
                return result;
            }

            var titleText = titleValidation.GetElementsByTagName("td").ElementAtOrDefault(0);
            if (titleText != null && !titleText.InnerHtml.Equals(ServiceSettings.ValidationHtml))
            {
                await SendSlackNotification(htmlPage, dateTime, CurrencyType.Arg);
                result.AddError("Html Error Bna", $"Error getting HTML, currently does not exist currency USD. Check date {dateTime.ToShortDateString()}.");
                return result;
            }

            var tableRows = document.QuerySelectorAll("table > tbody > tr");
            var usdCurrency = GetCurrencyByDate(tableRows, dateTime);

            if (usdCurrency == null)
            {
                Logger.LogError(new Exception("Error getting HTML"),
                        $"Error getting HTML, please check HTML and date is holiday : {htmlPage}");
                result.AddError("Html Error Bna", "Error getting HTML or date is holiday, please check Bna page.");
                return result;
            }

            var columns = usdCurrency.GetElementsByTagName("td");
            var buy = columns.ElementAtOrDefault(1);
            var sale = columns.ElementAtOrDefault(2);
            var date = columns.ElementAtOrDefault(3);

            if (buy != null && sale != null && date != null)
            {
                Logger.LogInformation("Creating UsdCurrency object to returned to the client.");
                return new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    Date = date.InnerHtml,
                    SaleValue = sale.InnerHtml,
                    BuyValue = buy.InnerHtml,
                    CurrencyName = ServiceSettings.CurrencyName
                });
            }

            await SendSlackNotification(htmlPage, dateTime, CurrencyType.Arg);
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

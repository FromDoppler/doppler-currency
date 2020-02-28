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
    public class BnaHandler : ICurrencyHandler
    {
        private readonly HttpClient _httpClient;
        private readonly UsdCurrencySettings _bnaSettings;
        private readonly ISlackHooksService _slackHooksService;
        private readonly ILoggerAdapter<BnaHandler> _logger;

        public BnaHandler(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings bnaClientPoliciesSettings,
            IOptionsMonitor<UsdCurrencySettings> bnaSettings,
            ISlackHooksService slackHooksService,
            ILoggerAdapter<BnaHandler> logger) =>
            (_httpClient, _bnaSettings, _slackHooksService, _logger) =
            (httpClientFactory.CreateClient(bnaClientPoliciesSettings.ClientName), bnaSettings.Get("BnaService"), slackHooksService, logger);

        public async Task<EntityOperationResult<UsdCurrency>> Handle(DateTime date)
        {
            // Construct URL
            _logger.LogInformation("building url to get html data.");
            var dateUrl = System.Web.HttpUtility.UrlEncode($"{date:dd/MM/yyyy}");

            var uri = new Uri(_bnaSettings.Url + "&fecha=" + dateUrl);

            _logger.LogInformation($"Building http request with url {uri}");
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("GET")
            };

            _logger.LogInformation("Sending request to Bna server.");
            var httpResponse = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);

            _logger.LogInformation("Getting Html content of the Bna.");
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
                _logger.LogInformation($"Does not exist currency USD for date {dateTime}");
                result.AddError("No USD for this date", _bnaSettings.NoCurrency);
                return result;
            }

            var titleValidation = document.GetElementsByTagName("tr").ElementAtOrDefault(1);
            if (titleValidation == null)
            {
                _logger.LogError(new Exception("Error getting HTML"),
                    $"Error getting HTML, title is not valid, please check HTML: {htmlPage}");
                await _slackHooksService.SendNotification(_httpClient, $"Doppler.Currency - Can't get the USD currency from ARG code country, please check Html or date is holiday {dateTime}");
                result.AddError("Html Error Bna", "Error getting HTML, currently does not exist currency USD.");
                return result;
            }

            var titleText = titleValidation.GetElementsByTagName("td").ElementAtOrDefault(0);
            if (titleText != null && !titleText.InnerHtml.Equals(_bnaSettings.ValidationHtml))
            {
                _logger.LogError(new Exception("Error getting HTML"),
                    $"Error getting HTML, currently does not exist currency USD: {htmlPage}");
                await _slackHooksService.SendNotification(_httpClient, $"Doppler.Currency - Can't get the USD currency from ARG code country, please check Html and date is holiday {dateTime}");
                result.AddError("Html Error Bna", "Error getting HTML, currently does not exist currency USD.");
                return result;
            }

            var usdCurrency = GetCurrencyByDate(document.GetElementsByTagName("tbody").FirstOrDefault()?.GetElementsByTagName("tr"), dateTime);

            if (usdCurrency == null)
            {
                _logger.LogError(new Exception("Error getting HTML"),
                        $"Error getting HTML, please check HTML and date is holiday : {htmlPage}");
                result.AddError("Html Error Bna", "Error getting HTML or date is holiday, please check HTML.");
                return result;
            }

            var buy = usdCurrency.GetElementsByTagName("td").ElementAtOrDefault(1);
            var sale = usdCurrency.GetElementsByTagName("td").ElementAtOrDefault(2);
            var date = usdCurrency.GetElementsByTagName("td").ElementAtOrDefault(3);

            if (buy != null && sale != null && date != null)
            {
                _logger.LogInformation("Creating UsdCurrency object to returned to the client.");

                return new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    Date = date.InnerHtml,
                    SaleValue = sale.InnerHtml,
                    BuyValue = buy.InnerHtml,
                    CurrencyName = _bnaSettings.CurrencyName
                });
            }

            _logger.LogError(new Exception("Error getting HTML"), $"Error getting HTML, please check HTML: {htmlPage}");
            await _slackHooksService.SendNotification(_httpClient, $"Doppler.Currency - Can't get the USD currency from ARG code country, please check Html and date is holiday {dateTime}");
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

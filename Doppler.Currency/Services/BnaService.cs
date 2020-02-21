using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Doppler.Currency.Services
{
    public class BnaService : IBnaService
    {
        private readonly HttpClient _httpClient;
        private readonly BnaSettings _bnaSettings;
        private readonly ISlackHooksService _slackHooksService;
        private readonly ILoggerAdapter<BnaService> _logger;

        public BnaService(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings bnaClientPoliciesSettings,
            BnaSettings bnaSettings,
            ISlackHooksService slackHooksService,
            ILoggerAdapter<BnaService> logger) =>
            (_httpClient,_bnaSettings, _slackHooksService, _logger) =
            (httpClientFactory.CreateClient(bnaClientPoliciesSettings.ClientName), bnaSettings, slackHooksService, logger);

        public async Task<EntityOperationResult<UsdCurrency>> GetUsdToday(DateTimeOffset? date)
        {
            // Construct URL
            _logger.LogInformation("building url to get html data.");
            var dateUrl = date == null ? System.Web.HttpUtility.UrlEncode(
                    $"{DateTimeOffset.UtcNow.ToOffset(new TimeSpan(-3, 0, 0)):dd/MM/yyyy}") :
                System.Web.HttpUtility.UrlEncode($"{date:dd/MM/yyyy}");

            var uri = new Uri(_bnaSettings.EndPoint + "&fecha=" + dateUrl);

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

        private async Task<EntityOperationResult<UsdCurrency>> GetDataFromHtmlAsync(string htmlPage, DateTimeOffset? dateTime)
        {
            var result = new EntityOperationResult<UsdCurrency>();
            var parser = new HtmlParser();
            var document = parser.ParseDocument(htmlPage);

            if (document.GetElementsByClassName("sinResultados").Any())
            {
                _logger.LogInformation($"Does not exist quotation USD for date {dateTime}");
                result.AddError("No USD for this date",_bnaSettings.NoCurrency);
                return result;
            }

            var titleValidation = document.GetElementsByTagName("tr").ElementAtOrDefault(1);
            if (titleValidation == null)
            {
                _logger.LogError(new Exception("Error getting HTML"), $"Error getting HTML, title is not valid, please check HTML: {htmlPage}");
                await _slackHooksService.SendNotification(_httpClient);
                result.AddError("Html Error Bna", "Error getting HTML, currently does not exist quotation USD.");
                return result;
            }

            var titleText = titleValidation.GetElementsByTagName("td").ElementAtOrDefault(0);
            if (titleText != null && !titleText.InnerHtml.Equals(_bnaSettings.ValidationHtml))
            {
                _logger.LogError(new Exception("Error getting HTML"), $"Error getting HTML, currently does not exist quotation USD: {htmlPage}");
                await _slackHooksService.SendNotification(_httpClient);
                result.AddError("Html Error Bna", "Error getting HTML, currently does not exist quotation USD.");
                return result;
            }

            var usdQuotation = dateTime == null ? document.GetElementsByTagName("tr").LastOrDefault() : 
                GetQuotationByDate(document.GetElementsByTagName("tbody").FirstOrDefault()?.GetElementsByTagName("tr"), dateTime);
            

            if (usdQuotation == null)
            {
                _logger.LogError(new Exception("Error getting HTML"), $"Error getting HTML, please check HTML: {htmlPage}");
                await _slackHooksService.SendNotification(_httpClient);
                result.AddError("Html Error Bna", "Error getting HTML, please check HTML.");
                return result;
            }

            var buy = usdQuotation.GetElementsByTagName("td").ElementAtOrDefault(1);
            var sale = usdQuotation.GetElementsByTagName("td").ElementAtOrDefault(2);
            var date = usdQuotation.GetElementsByTagName("td").ElementAtOrDefault(3);

            if (buy != null && sale != null && date != null)
            {
                var dt = DateTime.Parse(date.InnerHtml, CultureInfo.CreateSpecificCulture("es-AR"));
                return new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    Date = dt.ToString("yyyy-MM-dd"),
                    SaleValue = sale.InnerHtml,
                    BuyValue = buy.InnerHtml
                });
            }

            _logger.LogError(new Exception("Error getting HTML"), $"Error getting HTML, please check HTML: {htmlPage}");
            await _slackHooksService.SendNotification(_httpClient);
            result.AddError("Html Error Bna", "Error getting HTML, please check HTML.");
            return result;
        }

        private static IElement GetQuotationByDate(IEnumerable<IElement> htmlData, DateTimeOffset? dateTime)
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

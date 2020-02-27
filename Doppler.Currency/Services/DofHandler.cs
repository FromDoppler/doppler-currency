using System;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using CrossCutting;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Dtos;
using Doppler.Currency.Logger;
using Doppler.Currency.Settings;
using Microsoft.Extensions.Options;

namespace Doppler.Currency.Services
{
    public class DofHandler : ICurrencyHandler
    {
        private readonly HttpClient _httpClient;
        private readonly UsdCurrencySettings _dofSettings;
        private readonly ISlackHooksService _slackHooksService;
        private readonly ILoggerAdapter<DofHandler> _logger;

        public DofHandler(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings dofClientPoliciesSettings,
            IOptionsMonitor<UsdCurrencySettings> dofSettings,
            ISlackHooksService slackHooksService,
            ILoggerAdapter<DofHandler> logger) =>
        (_httpClient, _dofSettings, _slackHooksService, _logger) =
        (httpClientFactory.CreateClient(dofClientPoliciesSettings.ClientName), dofSettings.Get("DofService"), slackHooksService, logger);


        public async Task<EntityOperationResult<UsdCurrency>> Handle(DateTime date)
        {
            // Construct URL
            _logger.LogInformation("building url to get html data.");
            var dateUrl = System.Web.HttpUtility.UrlEncode($"{date:dd/MM/yyyy}");

            var uri = new Uri(_dofSettings.Url + "&dfecha=" + dateUrl + "&hfecha=" + dateUrl);

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

            return await GetDataFromHtmlAsync(htmlPage);
        }

        private async Task<EntityOperationResult<UsdCurrency>> GetDataFromHtmlAsync(string htmlPage)
        {
            var result = new EntityOperationResult<UsdCurrency>();
            var parser = new HtmlParser();
            var document = parser.ParseDocument(htmlPage);

            try
            {
                var table = document.GetElementsByClassName("Tabla_borde");

                return new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    Date = table[0].GetElementsByTagName("td")[2].InnerHtml,
                    SaleValue = table[0].GetElementsByTagName("td")[3].InnerHtml,
                    BuyValue = table[0].GetElementsByTagName("td")[3].InnerHtml,
                    CurrencyName = _dofSettings.CurrencyName
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting HTML, please check HTML and date not holiday : {htmlPage}");
                await _slackHooksService.SendNotification(_httpClient);
                result.AddError("Html Error Dof", "Error getting HTML or date not holiday, please check HTML.");
                return result;
            }
        }
    }
}

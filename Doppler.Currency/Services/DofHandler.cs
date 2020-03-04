using System;
using System.Linq;
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
    public class DofHandler : CurrencyHandler
    {
        public DofHandler(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings dofClientPoliciesSettings,
            IOptionsMonitor<UsdCurrencySettings> dofSettings,
            ISlackHooksService slackHooksService,
            ILoggerAdapter<CurrencyHandler> logger) : base(httpClientFactory.CreateClient(dofClientPoliciesSettings.ClientName), dofSettings.Get("DofService"),
            slackHooksService, logger)
        {
        }

        public override async Task<EntityOperationResult<UsdCurrency>> Handle(DateTime date)
        {
            // Construct URL
            Logger.LogInformation("building url to get html data.");
            var dateUrl = System.Web.HttpUtility.UrlEncode($"{date:dd/MM/yyyy}");

            var uri = new Uri(ServiceSettings.Url + "&dfecha=" + dateUrl + "&hfecha=" + dateUrl);

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

            return await GetDataFromHtmlAsync(htmlPage);
        }

        private async Task<EntityOperationResult<UsdCurrency>> GetDataFromHtmlAsync(string htmlPage)
        {
            var result = new EntityOperationResult<UsdCurrency>();
            var parser = new HtmlParser();
            var document = parser.ParseDocument(htmlPage);

            try
            {
                var table = document.GetElementsByClassName("Tabla_borde").FirstOrDefault();

                if (table != null)
                {
                    var columns = table.GetElementsByTagName("td");
                    if (columns.Any() && columns.Length == 4)
                    {
                        return new EntityOperationResult<UsdCurrency>(new UsdCurrency
                        {
                            Date = $"{columns.ElementAtOrDefault(2)?.InnerHtml}",
                            SaleValue = columns.ElementAtOrDefault(3)?.InnerHtml,
                            CurrencyName = ServiceSettings.CurrencyName
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error getting Mex currency, please check HTML or date is not holiday : {htmlPage}");
                await SlackHooksService.SendNotification(HttpClient, "Can't get the USD currency from MEX code country, please check Html and date is holiday");
                result.AddError("Html Error Mex currency", "Error getting HTML or date is not holiday, please check HTML.");
                return result;
            }

            Logger.LogError(new Exception(), $"Error getting Mex currency, please check HTML or date is not holiday : {htmlPage}");
            await SlackHooksService.SendNotification(HttpClient, "Can't get the USD currency from MEX code country, please check Html or date is holiday");
            result.AddError("Html Error Mex currency", "Error getting HTML or date is not holiday, please check HTML.");
            return result;
        }
    }
}

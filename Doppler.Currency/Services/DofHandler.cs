using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
    public class DofHandler : CurrencyHandler
    {
        public DofHandler(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings dofClientPoliciesSettings,
            IOptionsMonitor<CurrencySettings> dofSettings,
            ISlackHooksService slackHooksService,
            ILogger<CurrencyHandler> logger) : base(httpClientFactory.CreateClient(dofClientPoliciesSettings.ClientName), dofSettings.Get("DofService"),
            slackHooksService, logger)
        {
        }

        public override async Task<EntityOperationResult<CurrencyDto>> Handle(DateTime date)
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

            return await GetDataFromHtmlAsync(htmlPage, date);
        }

        private async Task<EntityOperationResult<CurrencyDto>> GetDataFromHtmlAsync(string htmlPage, DateTime date)
        {
            var result = new EntityOperationResult<CurrencyDto>();
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
                        var columnTime = $"{columns.ElementAtOrDefault(2)?.InnerHtml}";

                        if (columnTime == $"{date:dd-MM-yyyy}")
                        {
                            var saleValue = columns.ElementAtOrDefault(3)?.InnerHtml.Replace(".", ",");
                            return CreateCurrency(date, saleValue);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Mxn, e);
                result.AddError("Html Error Mxn currency", "Error getting HTML or date is holiday, please check HTML.");
                return result;
            }

            await SendSlackNotification(htmlPage, date, CurrencyCodeEnum.Mxn);
            result.AddError("Html Error Mxn currency", "Error getting HTML or date is holiday, please check HTML.");
            return result;
        }
    }
}

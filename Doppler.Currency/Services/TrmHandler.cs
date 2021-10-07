using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;
using Doppler.Currency.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Doppler.Currency.Services
{
    public class TrmHandler : CurrencyHandler
    {
        public TrmHandler(
            IHttpClientFactory httpClientFactory, 
            HttpClientPoliciesSettings httpClientPoliciesSettings,
            IOptionsMonitor<CurrencySettings> serviceSettings, 
            ISlackHooksService slackHooksService, 
            ILogger<CurrencyHandler> logger) : base(httpClientFactory, httpClientPoliciesSettings, serviceSettings.Get("TrmService"), slackHooksService, logger) { }

        public override async Task<EntityOperationResult<CurrencyDto>> Handle(DateTime date)
        {
            // Construct URL
            Logger.LogInformation("building url to get html data.");
            var dateUrl = System.Web.HttpUtility.UrlEncode($"{date:yyyy-MM-dd}");
            var uri = new Uri($"{ServiceSettings.Url}{dateUrl}");

            Logger.LogInformation("Building http request with url {uri}", uri);
            using var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("GET")
            };

            Logger.LogInformation("Sending request to Trm server.");
            var client = HttpClientFactory.CreateClient();
            var httpResponse = await client.SendAsync(httpRequest).ConfigureAwait(false);

            Logger.LogInformation("Getting Html content of the Trm.");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();

            return await GetDataFromHtmlAsync(jsonContent, date);
        }

        private Task<EntityOperationResult<CurrencyDto>> GetDataFromHtmlAsync(string jsonContent, DateTime date)
        {
            var result = new EntityOperationResult<CurrencyDto>();
            var data = JsonConvert.DeserializeObject<IList<TrmResponse>>(jsonContent);

            if (data.Count() > 0)
            {
                result.Entity = new CurrencyDto
                {
                    Date = $"{date.ToUniversalTime():yyyy-MM-dd}",
                    SaleValue = data.FirstOrDefault().Valor,
                    CurrencyName = ServiceSettings.CurrencyName,
                    CurrencyCode = ServiceSettings.CurrencyCode.ToUpper()
                };
            }
            else
            {
                result.Entity = CreateCurrency(date, "0", ServiceSettings.CurrencyCode);
            }

            return Task.FromResult(result);
        }
    }
}

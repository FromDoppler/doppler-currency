using Newtonsoft.Json;

namespace Doppler.Currency.Dtos
{
    public class UsdCurrency
    {
        public string Date { get; set; }
        public string SaleValue { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BuyValue { get; set; }
        public string CurrencyName { get; set; } 
    }
}

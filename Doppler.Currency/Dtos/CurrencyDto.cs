using Newtonsoft.Json;

namespace Doppler.Currency.Dtos
{
    public class CurrencyDto
    {
        public string Date { get; set; }

        public decimal SaleValue { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? BuyValue { get; set; }
        public string CurrencyName { get; set; } 
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Doppler.Currency.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CurrencyCodeEnum
    {
        [JsonProperty("ars")]
        Ars = 1,
        [JsonProperty("mxn")]
        Mxn = 2
    }
}

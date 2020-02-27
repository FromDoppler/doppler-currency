using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Doppler.Currency.Services
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CurrencyType
    {
        [JsonProperty("arg")]
        Arg = 1,
        [JsonProperty("mex")]
        Mex = 2,
    }
}

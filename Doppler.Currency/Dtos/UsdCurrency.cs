using System.Security.Policy;

namespace Doppler.Currency.Dtos
{
    public class UsdCurrency
    {
        public string Date { get; set; }
        public string SaleValue { get; set; }
        public string BuyValue { get; set; }
        public string CurrencyName { get; set; } 
    }
}

namespace Doppler.Currency.Responses
{
    public class GetOfficalDollarResponse
    {
        public string Moneda { get; set; }
        public string Casa { get; set; }
        public decimal Venta { get; set; }
        public decimal Compra { get; set; }
    }
}

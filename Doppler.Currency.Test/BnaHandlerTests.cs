using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Enums;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Doppler.Currency.Test.Helper;
using Doppler.Currency.Test.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.Currency.Test
{
    public class BnaHandlerTests
    {
        private readonly Mock<IOptionsMonitor<CurrencySettings>> _mockUsdCurrencySettings;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public BnaHandlerTests()
        {
            _mockUsdCurrencySettings = new Mock<IOptionsMonitor<CurrencySettings>>();
            _mockUsdCurrencySettings.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new CurrencySettings
                {
                    Url = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0",
                    NoCurrency = "",
                    CurrencyName = "Peso Argentino",
                    ValidationHtml = "Dolar U.S.A",
                    CurrencyCode = "ARS",
                    OfficialDollarApi= "https://dolarapi.com/v1/dolares/oficial"
                });

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeReturnCurrencyOk_WhenHtmlHaveTwoCurrency()
        {
            var dateTime = new DateTime(2020, 02, 05);

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"<div id='cotizacionesCercanas'>
                    <table class='table table-bordered cotizador' style='float:none; width:100%; text-align: center;'>
                    <thead>
                    <tr>
                    <th>Monedas</th>
                    <th>Compra</th>
                    <th>Venta</th>
                    <th>Fecha</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr>
                    <td>Dolar U.S.A</td>
                    <td class='dest'>58,0000</td>
                    <td class='dest'>63,0000</td>
                    <td>4/2/2020</td>
                    </tr>
                    <tr>
                    <td>Dolar U.S.A</td>
                    <td class='dest'>58,0000</td>
                    <td class='dest'>63,0000</td>
                    <td>5/2/2020</td>
                    </tr>
                    </tbody>
                    </table>
                    </div>")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var bnaHandler = new BnaHandler(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                Mock.Of<ISlackHooksService>(),
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await bnaHandler.Handle(dateTime);

            Assert.Equal("2020-02-05", result.Entity.Date);
            Assert.Equal(58.0000M, result.Entity.BuyValue);
            Assert.Equal(63.0000M, result.Entity.SaleValue);
            Assert.Equal("Peso Argentino", result.Entity.CurrencyName);
            Assert.Equal("ARS", result.Entity.CurrencyCode);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeReturnCurrencyOk_WhenHtmlHaveOneCurrency()
        {
            var dateTime = new DateTime(2020, 02, 04);

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"<div id='cotizacionesCercanas'>
                    <table class='table table-bordered cotizador' style='float:none; width:100%; text-align: center;'>
                    <thead>
                    <tr>
                    <th>Monedas</th>
                    <th>Compra</th>
                    <th>Venta</th>
                    <th>Fecha</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr>
                    <td>Dolar U.S.A</td>
                    <td class='dest'>58,0000</td>
                    <td class='dest'>63,0000</td>
                    <td>4/2/2020</td>
                    </tr>
                    </tbody>
                    </table>
                    </div>")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var bnaHandler = new BnaHandler(
                 _httpClientFactoryMock.Object,
                 new HttpClientPoliciesSettings
                 {
                     ClientName = "test"
                 },
                 _mockUsdCurrencySettings.Object,
                 Mock.Of<ISlackHooksService>(),
                 Mock.Of<ILogger<CurrencyHandler>>());

            var result = await bnaHandler.Handle(dateTime);

            Assert.Equal("2020-02-04", result.Entity.Date);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeReturnNoProceAndStatusOk_WhenNoProceButHtmlContainsPreviousPrices()
        {
            var currentDate = DateTime.Now;
            var date = System.Web.HttpUtility.UrlEncode($"{currentDate:dd/MM/yyyy}");
            var bnaSiteUrl = $"https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0&fecha={date}";
            var dollarApiUrl = "https://dolarapi.com/v1/dolares/oficial";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                //BELOW IS PREDICATE
                ItExpr.Is<HttpRequestMessage>(match =>
                    match.Method == HttpMethod.Get && match.RequestUri == new Uri(bnaSiteUrl)),
                //END OF PREDICATE
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"<div id='cotizacionesCercanas'>
                    <table class='table table-bordered cotizador' style='float:none; width:100%; text-align: center;'>
                    <thead>
                    <tr>
                    <th>Monedas</th>
                    <th>Compra</th>
                    <th>Venta</th>
                    <th>Fecha</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr>
                    <td>Dolar U.S.A</td></div>")
                });

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                //BELOW IS PREDICATE
                ItExpr.Is<HttpRequestMessage>(match =>
                    match.Method == HttpMethod.Get && match.RequestUri == new Uri(dollarApiUrl)),
                //END OF PREDICATE
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{
                        ""moneda"": ""USD"",
                        ""casa"": ""oficial"",
                        ""nombre"": ""Oficial"",
                        ""compra"": 1400,
                        ""venta"": 1450,
                        ""fechaActualizacion"": ""2025-10-02T09:47:00.000Z""
                    }")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(_httpClient);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();

            var bnaHandler = new BnaHandler(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await bnaHandler.Handle(currentDate);

            Assert.True(result.Success);
            Assert.True(result.Entity.CotizationAvailable);
        }
    }
}

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

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                Mock.Of<ISlackHooksService>(),
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(dateTime, CurrencyCodeEnum.Ars);

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

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                Mock.Of<ISlackHooksService>(),
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(dateTime, CurrencyCodeEnum.Ars);

            Assert.Equal("2020-02-04", result.Entity.Date);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeSendSlackNotificationError_WhenHtmlTitleIsNotCorrect()
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"<div id='cotizacionesCercanas'></div>")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(
                    It.IsAny<HttpClient>(),
                    It.IsAny<string>()))
                .Verifiable();

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                Mock.Of<ILogger<CurrencyHandler>>());

            await service.GetCurrencyByCurrencyCodeAndDate(DateTime.Now, CurrencyCodeEnum.Ars);

            slackHooksServiceMock.Verify(x => x.SendNotification(
                    It.IsAny<HttpClient>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeSendSlackNotificationError_WhenHtmlTableIsNotCorrect()
        {
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
                    <td>Dolar U.S.A</td></div>")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(DateTime.Now, CurrencyCodeEnum.Ars);

            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(),
                It.IsAny<string>()), Times.Never);

            Assert.False(result.Success);
            Assert.Equal(1, result.Errors.Count);
            Assert.True(result.Errors.ContainsKey("Holiday Error"));

            result.Errors.TryGetValue("Holiday Error", out var value);

            Assert.True(result.Errors.Values.Contains(value));
        }

        [Fact]
        public async Task GetCurrency_ShouldBeNotSendSlackNotificationErrorAndReturnBadRequest_WhenThereIsNoCurrency()
        {
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
                     <div class='sinResultados'>No hay cotizaciones pendientes para esa fecha.</div>
                    </ tbody > ")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(
                    It.IsAny<HttpClient>(),
                    It.IsAny<string>()))
                .Verifiable();

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(DateTime.UtcNow.AddYears(1), CurrencyCodeEnum.Ars);

            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(),
                It.IsAny<string>()), Times.Never);

            Assert.Equal(1, result.Errors.Count);
            Assert.False(result.Success);
            Assert.True(result.Errors.ContainsKey("No USD for this date"));
        }

        [Fact]
        public async Task GetCurrency_ShouldBeTraceLogInformationWithUrl_WhenCallBnaServiceOk()
        {
            var dateTime = DateTime.UtcNow;

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
                     <div class='sinResultados'>No hay cotizaciones pendientes para esa fecha.</div>
                    </ tbody > ")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var loggerMock = new Mock<ILogger<CurrencyHandler>>();

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                loggerService: Mock.Of<ILogger<CurrencyService>>(),
                loggerHandler: loggerMock.Object);

            var result = await service.GetCurrencyByCurrencyCodeAndDate(dateTime, CurrencyCodeEnum.Ars);

            Assert.False(result.Success);

            var month = dateTime.Month.ToString("d2");
            var day = dateTime.Day.ToString("d2");

            var urlCheck =
                $"https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0&fecha={day}%2f{month}%2f{dateTime.Year}";

            loggerMock.VerifyLogger(LogLevel.Information, $"Building http request with url {urlCheck}",Times.Once());
        }
    }
}

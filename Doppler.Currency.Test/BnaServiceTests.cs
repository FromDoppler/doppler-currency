using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Logger;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Doppler.Currency.Test.Integration;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.Currency.Test
{
    public class BnaServiceTests
    {
        [Fact]
        public async Task GetUsdToday_ShouldBeReturnUsdQuotationOfBna_WhenHtmlHaveTwoQuotationUsdToReturnOk()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
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
                    </div>"),
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var service = CreateSutBnaService.CreateSut(
                httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                new BnaSettings
                {
                    EndPoint = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes",
                    ValidationHtml = "Dolar U.S.A"
                },
                Mock.Of<ISlackHooksService>(),
                Mock.Of<ILoggerAdapter<BnaService>>());

            var result = await service.GetUsdToday(null);

            Assert.Equal("2020-02-05", result.Entity.Date);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeReturnUsdQuotationOfBna_WhenHtmlHaveOneQuotationUsdToReturnOk()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
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
                    </div>"),
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var service = CreateSutBnaService.CreateSut(
                httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                new BnaSettings
                {
                    EndPoint = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes",
                    ValidationHtml = "Dolar U.S.A"
                },
                Mock.Of<ISlackHooksService>(),
                Mock.Of<ILoggerAdapter<BnaService>>());

            var result = await service.GetUsdToday(null);

            Assert.Equal("2020-02-04", result.Entity.Date);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeSendSlackNotificationError_WhenHtmlTitleIsNotCorrect()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"<div id='cotizacionesCercanas'></div>")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var service = CreateSutBnaService.CreateSut(
                httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                new BnaSettings
                {
                    EndPoint = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes",
                    ValidationHtml = "Dolar U.S.A"
                },
                slackHooksServiceMock.Object,
                Mock.Of<ILoggerAdapter<BnaService>>());

            await service.GetUsdToday(null);

            slackHooksServiceMock.Verify(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeSendSlackNotificationError_WhenHtmlTableIsNotCorrect()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
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

            var client = new HttpClient(mockHttpMessageHandler.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var service = CreateSutBnaService.CreateSut(
                httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                new BnaSettings
                {
                    EndPoint = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes",
                    ValidationHtml = "Dolar U.S.A"
                },
                slackHooksServiceMock.Object,
                Mock.Of<ILoggerAdapter<BnaService>>());

            await service.GetUsdToday(null);

            slackHooksServiceMock.Verify(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeSendSlackNotificationError_WhenThereIsNoQuotation()
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
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

            var client = new HttpClient(mockHttpMessageHandler.Object);
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var service = CreateSutBnaService.CreateSut(
                httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                new BnaSettings
                {
                    EndPoint = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes",
                    ValidationHtml = "Dolar U.S.A",
                    NoCurrency = "Check this value"
                },
                slackHooksServiceMock.Object,
                Mock.Of<ILoggerAdapter<BnaService>>());

            var result = await service.GetUsdToday(DateTimeOffset.UtcNow.AddYears(1));

            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(), 
                It.IsAny<string>()), Times.Never);

            Assert.Equal(1,result.Errors.Count);
            Assert.False(result.Success);
            Assert.True(result.Errors.ContainsKey("No USD for this date"));
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeLogginInformationWithUrl_WhenCallBnaServiceOk()
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
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

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var loggerMock = new Mock<ILoggerAdapter<BnaService>>();

            var service = CreateSutBnaService.CreateSut(
                httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                new BnaSettings
                {
                    EndPoint = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0",
                    ValidationHtml = "Dolar U.S.A",
                    NoCurrency = "Check this value"
                },
                slackHooksServiceMock.Object,
                loggerMock.Object);
            
            var result = await service.GetUsdToday(null);

            Assert.False(result.Success);

            var dateNow = DateTime.Now;
            var month = dateNow.Month.ToString("d2");

            var urlCheck =
                $"https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0&fecha={dateNow.Day}%2f{month}%2f{dateNow.Year}";
            
            loggerMock.Verify(x => x.LogInformation(
                $"Building http request with url {urlCheck}"), Times.Once);
        }
    }
}
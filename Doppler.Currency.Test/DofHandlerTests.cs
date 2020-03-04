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
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.Currency.Test
{
    public class DofHandlerTests
    {
        private readonly Mock<IOptionsMonitor<UsdCurrencySettings>> _mockUsdCurrencySettings;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public DofHandlerTests()
        {
            _mockUsdCurrencySettings = new Mock<IOptionsMonitor<UsdCurrencySettings>>();
            _mockUsdCurrencySettings.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new UsdCurrencySettings
                {
                    Url = "http://www.dof.gob.mx/indicadores_detalle.php?cod_tipo_indicador=158",
                    NoCurrency = "",
                    CurrencyName = "",
                    ValidationHtml = "Dolar U.S.A"
                });

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeReturnUsdCurrencyOfDofOk_WhenHtmlHaveCurrencyUsd()
        {
            var dateTime = new DateTime(2020, 2, 5);
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"
                        <table width='70%' border='0' cellspacing='0' cellpadding='0' class='Tabla_borde' align='center' style='border: 1px solid #b2b2b2' bgcolor='#FFFFFF'>
                        <tr class='txt_blanco' bgcolor='#b2b2b2'> 
                        <td height = '17' width='48%' align='center' style='padding: 5px;'>Fecha</td>
                        <td height = '17' width='52%' align='center' style='padding: 5px;'>Valor</td>
                        </tr>
                        <tr class='Celda 1'> 
                        <td height = '17' width='48%' align='center' class='txt' style='padding: 3px;'>05-02-2020</td>
                        <td width = '52%' align='center' class='txt'>18.679700</td>
                        </tr>
                        </table>
                        <br />
                        </td>
                        </tr>
                        </table>")
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
                Mock.Of<ILoggerAdapter<CurrencyHandler>>());

            var result = await service.GetUsdCurrencyByCountryAndDate(dateTime, "mex");
            
            Assert.Equal($"{dateTime:dd-MM-yyyy}", result.Entity.Date);
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeSendSlackNotificationError_WhenHtmlClassNameIsNotCorrect()
        {
            // Arrange
            const string classCheck = "Tabla_borde1";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($@"
                        <table width='70%' border='0' cellspacing='0' cellpadding='0' class='{classCheck}' align='center' style='border: 1px solid #b2b2b2' bgcolor='#FFFFFF'>
                        <tr class='txt_blanco' bgcolor='#b2b2b2'> 
                        <td height = '17' width='48%' align='center' style='padding: 5px;'>Fecha</td>
                        <td height = '17' width='52%' align='center' style='padding: 5px;'>Valor</td>
                        </tr>
                        <tr class='Celda 1'> 
                        <td height = '17' width='48%' align='center' class='txt' style='padding: 3px;'>05-02-2020</td>
                        <td width = '52%' align='center' class='txt'>18.679700</td>
                        </tr>
                        </table>
                        <br />
                        </td>
                        </tr>
                        </table>")
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
                Mock.Of<ILoggerAdapter<CurrencyHandler>>());

            var result = await service.GetUsdCurrencyByCountryAndDate(DateTime.Now, "Mex");

            Assert.False(result.Success);
            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(),
                It.IsAny<string>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeSendSlackNotificationError_WhenHtmlDoesNotColumnsTableCorrectly()
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"
                        <table width='70%' border='0' cellspacing='0' cellpadding='0' class='Tabla_borde' align='center' style='border: 1px solid #b2b2b2' bgcolor='#FFFFFF'>
                        <tr class='txt_blanco' bgcolor='#b2b2b2'> 
                        <td height = '17' width='48%' align='center' style='padding: 5px;'>Fecha</td>
                        <td height = '17' width='52%' align='center' style='padding: 5px;'>Valor</td>
                        </tr>
                        <tr class='Celda 1'> 
                        </tr>
                        </table>
                        <br />
                        </td>
                        </tr>
                        </table>")
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
                Mock.Of<ILoggerAdapter<CurrencyHandler>>());

            var result = await service.GetUsdCurrencyByCountryAndDate(DateTime.Now, "Mex");

            Assert.False(result.Success);

            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(), 
                It.IsAny<string>()),
                Times.Once);

            Assert.Equal(1, result.Errors.Count);
            Assert.True(result.Errors.ContainsKey("Html Error Mex currency"));

            result.Errors.TryGetValue("Html Error Mex currency", out var value);

            Assert.True(result.Errors.Values.Contains(value));
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeLoginInformationWithUrl_WhenCallBnaServiceOk()
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"
                        <table width='70%' border='0' cellspacing='0' cellpadding='0' class='Tabla_borde' align='center' style='border: 1px solid #b2b2b2' bgcolor='#FFFFFF'>
                        <tr class='txt_blanco' bgcolor='#b2b2b2'> 
                        <td height = '17' width='48%' align='center' style='padding: 5px;'>Fecha</td>
                        <td height = '17' width='52%' align='center' style='padding: 5px;'>Valor</td>
                        </tr>
                        <tr class='Celda 1'> 
                        </tr>
                        </table>
                        <br />
                        </td>
                        </tr>
                        </table>")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var slackHooksServiceMock = new Mock<ISlackHooksService>();
            slackHooksServiceMock.Setup(x => x.SendNotification(It.IsAny<HttpClient>(), It.IsAny<string>()))
                .Verifiable();

            var loggerMock = new Mock<ILoggerAdapter<CurrencyHandler>>();

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                loggerMock.Object);

            var result = await service.GetUsdCurrencyByCountryAndDate(DateTime.Now, "Mex");

            Assert.False(result.Success);

            var dateNow = DateTime.Now;
            var month = dateNow.Month.ToString("d2");
            var day = dateNow.Day.ToString("d2");

            var urlCheck =
                $"http://www.dof.gob.mx/indicadores_detalle.php?cod_tipo_indicador=158&dfecha=04%2f03%2f2020&hfecha={day}%2f{month}%2f{dateNow.Year}";
            
            loggerMock.Verify(x => x.LogInformation(
                $"Building http request with url {urlCheck}"), Times.Once);
        }
    }
}

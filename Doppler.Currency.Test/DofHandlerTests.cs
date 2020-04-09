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
    public class DofHandlerTests
    {
        private readonly Mock<IOptionsMonitor<CurrencySettings>> _mockUsdCurrencySettings;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public DofHandlerTests()
        {
            _mockUsdCurrencySettings = new Mock<IOptionsMonitor<CurrencySettings>>();
            _mockUsdCurrencySettings.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new CurrencySettings
                {
                    Url = "http://www.dof.gob.mx/indicadores_detalle.php?cod_tipo_indicador=158",
                    NoCurrency = "",
                    CurrencyName = "Peso Mexicano",
                    ValidationHtml = "Dolar U.S.A",
                    CurrencyCode = "MXN"
                });

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeReturnCurrencyOfDofOk_WhenHtmlHaveCurrency()
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
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(dateTime, CurrencyCodeEnum.Mxn);
            
            Assert.Equal("2020-02-05", result.Entity.Date);
            Assert.Equal(18.679700M, result.Entity.SaleValue);
            Assert.Equal("Peso Mexicano", result.Entity.CurrencyName);
            Assert.Equal("MXN", result.Entity.CurrencyCode);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeSendSlackNotificationError_WhenHtmlClassNameIsNotCorrect()
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
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(DateTime.Now, CurrencyCodeEnum.Mxn);

            Assert.False(result.Success);
            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(),
                It.IsAny<string>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeSendSlackNotificationError_WhenHtmlDoesNotColumnsTableCorrectly()
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
                Mock.Of<ILogger<CurrencyHandler>>());

            var result = await service.GetCurrencyByCurrencyCodeAndDate(DateTime.Now, CurrencyCodeEnum.Mxn);

            Assert.False(result.Success);

            slackHooksServiceMock.Verify(x => x.SendNotification(
                It.IsAny<HttpClient>(), 
                It.IsAny<string>()),
                Times.Once);

            Assert.Equal(1, result.Errors.Count);
            Assert.True(result.Errors.ContainsKey("Html Error Mxn currency"));

            result.Errors.TryGetValue("Html Error Mxn currency", out var value);

            Assert.True(result.Errors.Values.Contains(value));
        }

        [Fact]
        public async Task GetCurrency_ShouldBeLoginInformationWithUrl_WhenCallDofServiceOk()
        {
            var dateTime = DateTime.UtcNow;

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

            var loggerMock = new Mock<ILogger<CurrencyHandler>>();

            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                slackHooksServiceMock.Object,
                loggerMock.Object);

            var result = await service.GetCurrencyByCurrencyCodeAndDate(dateTime, CurrencyCodeEnum.Mxn);

            Assert.False(result.Success);

            var month = dateTime.Month.ToString("d2");
            var day = dateTime.Day.ToString("d2");

            var urlCheck =
                $"http://www.dof.gob.mx/indicadores_detalle.php?cod_tipo_indicador=158&dfecha={day}%2f{month}%2f{dateTime.Year}&hfecha={day}%2f{month}%2f{dateTime.Year}";

            loggerMock.VerifyLogger(LogLevel.Information, $"Building http request with url {urlCheck}", Times.Once());
        }
    }
}

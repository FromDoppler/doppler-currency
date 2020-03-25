using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Enums;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Doppler.Currency.Test.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.Currency.Test
{
    public class CurrencyServiceTests
    {
        private readonly Mock<IOptionsMonitor<CurrencySettings>> _mockUsdCurrencySettings;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public CurrencyServiceTests()
        {
            _mockUsdCurrencySettings = new Mock<IOptionsMonitor<CurrencySettings>>();
            _mockUsdCurrencySettings.Setup(x =>x.Get(It.IsAny<string>()))
                .Returns(new CurrencySettings
                {
                    Url = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0",
                    NoCurrency = "",
                    CurrencyName = "",
                    ValidationHtml = "Dolar U.S.A"
                });

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [Theory]
        [ClassData(typeof(CalculatorTestData))]
        public async Task GetCurrency_ShouldBeHttpStatusCodeOk_WhenCurrencyCodeAndHtmlAreValid(string currencyCode, string html)
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(html)
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

            Enum.TryParse(typeof(CurrencyCodeEnum), currencyCode, true, out var parseResult);

            var result = await service.GetCurrencyByCurrencyCodeAndDate(new DateTime(2020, 2, 4), (CurrencyCodeEnum) parseResult);

            Assert.True(result.Success);
            Assert.Equal(0, result.Errors.Count);
            Assert.False(result.Errors.ContainsKey("Currency code invalid"));
        }

        public class CalculatorTestData : IEnumerable<object[]>
        {
            private const string ArsHtml = @"<div id='cotizacionesCercanas'>
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
            </div>";

            private const string MxnHtml = @"
                        <table width='70%' border='0' cellspacing='0' cellpadding='0' class='Tabla_borde' align='center' style='border: 1px solid #b2b2b2' bgcolor='#FFFFFF'>
                        <tr class='txt_blanco' bgcolor='#b2b2b2'> 
                        <td height = '17' width='48%' align='center' style='padding: 5px;'>Fecha</td>
                        <td height = '17' width='52%' align='center' style='padding: 5px;'>Valor</td>
                        </tr>
                        <tr class='Celda 1'> 
                        <td height = '17' width='48%' align='center' class='txt' style='padding: 3px;'>04-02-2020</td>
                        <td width = '52%' align='center' class='txt'>18.679700</td>
                        </tr>
                        </table>
                        <br />
                        </td>
                        </tr>
                        </table>";

            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "ARS", ArsHtml };
                yield return new object[] { "ars", ArsHtml };
                yield return new object[] { "Ars", ArsHtml };
                yield return new object[] { "aRs", ArsHtml };
                yield return new object[] { "arS", ArsHtml };
                yield return new object[] { "ArS", ArsHtml };
                yield return new object[] { "ARs", ArsHtml };
                yield return new object[] { "arS", ArsHtml };
                yield return new object[] { "aRS", ArsHtml };

                yield return new object[] { "MXN", MxnHtml };
                yield return new object[] { "mxn", MxnHtml };
                yield return new object[] { "Mxn", MxnHtml };
                yield return new object[] { "mXn", MxnHtml };
                yield return new object[] { "mxN", MxnHtml };
                yield return new object[] { "MxN", MxnHtml };
                yield return new object[] { "MXn", MxnHtml };
                yield return new object[] { "mxN", MxnHtml };
                yield return new object[] { "mXN", MxnHtml };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
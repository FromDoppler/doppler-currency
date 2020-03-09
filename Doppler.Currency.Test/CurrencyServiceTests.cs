using System;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Enums;
using Doppler.Currency.Logger;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Doppler.Currency.Test.Integration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Doppler.Currency.Test
{
    public class CurrencyServiceTests
    {
        private readonly Mock<IOptionsMonitor<CurrencySettings>> _mockUsdCurrencySettings;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

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
        }

        [Theory]
        [InlineData("ARS")]
        [InlineData("ars")]
        [InlineData("Ars")]
        [InlineData("aRs")]
        [InlineData("arS")]
        [InlineData("MXN")]
        [InlineData("mxn")]
        [InlineData("Mxn")]
        [InlineData("mXn")]
        [InlineData("mxN")]
        public async Task GetCurrency_ShouldBeHttpStatusCodeBadRequest_WhenCurrencyCodeIsValid(string currencyCode)
        {
            var service = CreateSutCurrencyService.CreateSut(
                _httpClientFactoryMock.Object,
                new HttpClientPoliciesSettings
                {
                    ClientName = "test"
                },
                _mockUsdCurrencySettings.Object,
                Mock.Of<ISlackHooksService>(),
                Mock.Of<ILoggerAdapter<CurrencyHandler>>());

            Enum.TryParse(typeof(CurrencyCodeEnum), currencyCode, true, out var parseResult);

            var result = await service.GetCurrencyByCurrencyCodeAndDate(DateTime.Now,(CurrencyCodeEnum) parseResult);

            Assert.False(result.Success);
            Assert.Equal(1, result.Errors.Count);
            Assert.True(result.Errors.ContainsKey("Currency code invalid"));
        }
    }
}
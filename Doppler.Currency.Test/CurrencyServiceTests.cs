using System;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting.SlackHooksService;
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
        private readonly Mock<IOptionsMonitor<UsdCurrencySettings>> _mockUsdCurrencySettings;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public CurrencyServiceTests()
        {
            _mockUsdCurrencySettings = new Mock<IOptionsMonitor<UsdCurrencySettings>>();
            _mockUsdCurrencySettings.Setup(x =>x.Get(It.IsAny<string>()))
                .Returns(new UsdCurrencySettings
                {
                    Url = "https://bna.com.ar/Cotizador/HistoricoPrincipales?id=billetes&filtroDolar=1&filtroEuro=0",
                    NoCurrency = "",
                    CurrencyName = "",
                    ValidationHtml = "Dolar U.S.A"
                });

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        }

        [Theory]
        [InlineData("TEST")]
        [InlineData("ARGentina")]
        [InlineData("mexico")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("/")]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeBadRequest_WhenCountryCodeIsInvalid(string countryCode)
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

            var result = await service.GetUsdCurrencyByCountryAndDate(DateTime.Now, countryCode);
            
            Assert.False(result.Success);
            Assert.Equal(1, result.Errors.Count);
            Assert.True(result.Errors.ContainsKey("Country code invalid"));
        }

        [Theory]
        [InlineData("ARG")]
        [InlineData("arg")]
        [InlineData("Arg")]
        [InlineData("aRg")]
        [InlineData("arG")]
        [InlineData("MEX")]
        [InlineData("mex")]
        [InlineData("Mex")]
        [InlineData("mEx")]
        [InlineData("meX")]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeBadRequest_WhenCountryCodeIsValid(string countryCode)
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

            var result = await service.GetUsdCurrencyByCountryAndDate(DateTime.Now, countryCode);

            Assert.False(result.Success);
            Assert.Equal(1, result.Errors.Count);
            Assert.True(result.Errors.ContainsKey("Country code invalid"));
        }
    }
}
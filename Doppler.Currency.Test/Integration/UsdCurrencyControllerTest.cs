using System;
using System.Net;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.Currency.Dtos;
using Moq;
using Xunit;

namespace Doppler.Currency.Test.Integration
{
    public class UsdCurrencyControllerTest : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _testServer;

        public UsdCurrencyControllerTest(TestServerFixture testServerFixture) => _testServer = testServerFixture;

        [Fact]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeOk_WhenCurrencyServiceReturnUsdCurrencyCorrectly()
        {
            //Arrange
            _testServer.CurrencyServiceMock.Setup(x => x.GetUsdTodayByCountry(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    BuyValue = "10",
                    SaleValue = "30",
                    Date = "21/12/2012"
                }));

            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency/Mex/12-21-2020");                          
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(responseString);
            Assert.Contains("21/12/2012", responseString);
            Assert.Contains("30", responseString);
            Assert.Contains("10", responseString);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeBadRequest_WhenCurrencyServiceReturnUsdCurrencyInCorrectly()
        {
            //Arrange
            var result = new EntityOperationResult<UsdCurrency>();
            result.AddError("Error","Html error");
            _testServer.CurrencyServiceMock.Setup(x => x.GetUsdTodayByCountry(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>()))
                .ReturnsAsync(result);

            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency/Arg/20-20-2020");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesNotHaveDateTime()
        {
            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency/Arg");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesNotHaveCountryCode()
        {
            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency/02-02-2020");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeBadRequest_WhenUrlDoesHaveInvalidCountryCode()
        {
            //Arrange
            const string countryCode = "Test";
            var result = new EntityOperationResult<UsdCurrency>();
            result.AddError("Country code invalid", $"Currency country invalid: {countryCode}");
            _testServer.CurrencyServiceMock.Setup(x => x.GetUsdTodayByCountry(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>()))
                .ReturnsAsync(result);

            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency/TEST/02-02-2020");

            // Assert
            Assert.False(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData("02-223223-2020")]
        [InlineData("02-aa-2020")]
        [InlineData("3030-1-50")]
        [InlineData("02-2019-2020")]
        [InlineData("0202-220-2020")]
        [InlineData("2020-20-02")]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeBadRequest_WhenUrlDoesHaveInvalidDateTime(string dateTime)
        {
            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync($"UsdCurrency/Arg/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("02/02/2020")]
        [InlineData("02\\02\\2020")]
        [InlineData("2020/02/02")]
        [InlineData("2020/02/0Z")]
        public async Task GetUsdToday_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesHaveInDateTimeInvalidCharacter(string dateTime)
        {
            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync($"UsdCurrency/Arg/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

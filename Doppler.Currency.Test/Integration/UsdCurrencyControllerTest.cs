using System;
using System.Net;
using System.Net.Http;
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
        private readonly HttpClient _client;

        public UsdCurrencyControllerTest(TestServerFixture testServerFixture)
        {
            _testServer = testServerFixture;
            _client = _testServer.Client;
        }

        [Theory]
        [InlineData("21-12-2012", "Arg")]
        [InlineData("1-12-2012", "ARG")]
        [InlineData("1-2-2012", "arg")]
        [InlineData("21-2-2012","Mex")]
        [InlineData("01-02-2012", "mex")]
        [InlineData("01-2-2012", "MEX")]
        [InlineData("1-02-2012", "mEx")]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeOk_WhenDateAndCountryCodeAreCorrectly(string dateTime, string countryCode)
        {
            //Arrange
            _testServer.CurrencyServiceMock.Setup(x => x.GetUsdCurrencyByCountryAndDate(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    BuyValue = "10",
                    SaleValue = "30",
                    Date = dateTime
                }));

            // Act
            var response = await _client.GetAsync($"UsdCurrency/{countryCode}/{dateTime}");                          
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(responseString);
            Assert.Contains(dateTime, responseString);
            Assert.Contains("30", responseString);
            Assert.Contains("10", responseString);
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesNotHaveDateTime()
        {
            // Act
            var response = await _client.GetAsync("UsdCurrency/Arg");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesNotHaveCountryCode()
        {
            // Act
            var response = await _client.GetAsync("UsdCurrency/02-02-2020");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeBadRequest_WhenUrlDoesHaveInvalidCountryCode()
        {
            //Arrange
            const string countryCode = "Test";
            var result = new EntityOperationResult<UsdCurrency>();
            result.AddError("Country code invalid", $"Currency country invalid: {countryCode}");
            _testServer.CurrencyServiceMock.Setup(x => x.GetUsdCurrencyByCountryAndDate(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>()))
                .ReturnsAsync(result);

            // Act
            var response = await _client.GetAsync("UsdCurrency/TEST/02-02-2020");

            // Assert
            Assert.False(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData("02-223223-2020", "Arg")]
        [InlineData("02-aa-2020", "ArG")]
        [InlineData("3030-1-50", "arg")]
        [InlineData("02-2019-2020", "Mex")]
        [InlineData("0202-220-2020", "mEX")]
        [InlineData("2020-20-02", "MEX")]
        [InlineData("12-22-2010", "MeX")]
        [InlineData("31-2-2015", "MeX")]
        [InlineData("20-20-2020", "mEx")]
        [InlineData("20-20-2160", "mEx")]
        [InlineData("null", "mEx")]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeBadRequest_WhenUrlDoesHaveInvalidDateTime(string dateTime, string countryCode)
        {
            // Act
            var response = await _client.GetAsync($"UsdCurrency/{countryCode}/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = response.Content.ReadAsStringAsync().Result;
            Assert.Contains($"Invalid Date {dateTime}", text);
        }

        [Theory]
        [InlineData(null, "Arg")]
        [InlineData(null, "mex")]
        [InlineData("", "mEx")]
        [InlineData(" ", "arg")]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesHaveNullAndEmptyDateTime(string dateTime, string countryCode)
        {
            // Act
            var response = await _client.GetAsync($"UsdCurrency/{countryCode}/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("02/02/2020")]
        [InlineData("02\\02\\2020")]
        [InlineData("2020/02/02")]
        [InlineData("2020/02/0Z")]
        public async Task GetUsdCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesHaveInDateTimeInvalidCharacter(string dateTime)
        {
            // Act
            var response = await _client.GetAsync($"UsdCurrency/Arg/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting;
using Moq;
using Xunit;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;

namespace Doppler.Currency.Test.Integration
{
    public class CurrencyControllerTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _testServer;
        private readonly HttpClient _client;

        public CurrencyControllerTests(TestServerFixture testServerFixture)
        {
            _testServer = testServerFixture;
            _client = _testServer.Client;
        }

        [Theory]
        [InlineData("1-2-2012", "ARS")]
        [InlineData("01-02-2012", "mxn")]
        [InlineData("01-2-2012", "MXN")]
        [InlineData("1-02-2012", "mXn")]
        public async Task GetCurrency_ShouldBeHttpStatusCodeOk_WhenDateAndCurrencyCodeAreCorrectly(string dateTime, string currencyCode)
        {
            //Arrange
            _testServer.CurrencyServiceMock.Setup(x => x.GetCurrencyByCurrencyCodeAndDate(
                    It.IsAny<DateTime>(),
                    It.IsAny<CurrencyCodeEnum>()))
                .ReturnsAsync(new EntityOperationResult<CurrencyDto>(new CurrencyDto
                {
                    BuyValue = 10.3434M,
                    SaleValue = 30.34M,
                    Date = $"{DateTime.Parse(dateTime):yyyy-MM-dd}"
                }));

            // Act
            var response = await _client.GetAsync($"Currency/{currencyCode}/{dateTime}");                          
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(responseString);
            Assert.Contains("2012-01-02", responseString);
            Assert.Contains("30.34", responseString);
            Assert.Contains("10.3434", responseString);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesNotHaveDateTime()
        {
            // Act
            var response = await _client.GetAsync("Currency/Ars");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesNotHaveCurrencyCode()
        {
            // Act
            var response = await _client.GetAsync("Currency/02-02-2020");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrency_ShouldBeHttpStatusCodeBadRequest_WhenUrlDoesHaveInvalidCurrencyCode()
        {
            //Arrange
            const string currencyCode = "Test";
            var result = new EntityOperationResult<CurrencyDto>();
            result.AddError("Currency code invalid", $"Currency code invalid: {currencyCode}");
            _testServer.CurrencyServiceMock.Setup(x => x.GetCurrencyByCurrencyCodeAndDate(
                    It.IsAny<DateTime>(),
                    It.IsAny<CurrencyCodeEnum>()))
                .ReturnsAsync(result);

            // Act
            var response = await _client.GetAsync("Currency/TEST/02-02-2020");

            // Assert
            Assert.False(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData("02-223223-2020", "Ars")]
        [InlineData("02-aa-2020", "ArS")]
        [InlineData("3030-1-50", "ars")]
        [InlineData("02-2019-2020", "Mxn")]
        [InlineData("0202-220-2020", "mXN")]
        [InlineData("2020-20-02", "MXN")]
        [InlineData("31-2-2015", "MxN")]
        [InlineData("20-20-2020", "mXn")]
        [InlineData("20-20-2160", "mXn")]
        [InlineData("null", "mXn")]
        [InlineData("21-12-2012", "Ars")]
        [InlineData("21-2-2012", "Mxn")]
        public async Task GetCurrency_ShouldBeHttpStatusCodeBadRequest_WhenUrlDoesHaveInvalidDateTime(string dateTime, string currencyCode)
        {
            // Act
            var response = await _client.GetAsync($"Currency/{currencyCode}/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains($"The value '{dateTime}' is not valid", text);
        }

        [Theory]
        [InlineData(null, "Ars")]
        [InlineData(null, "mxn")]
        [InlineData("", "mXn")]
        [InlineData(" ", "ars")]
        public async Task GetCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesHaveNullAndEmptyDateTime(string dateTime, string currencyCode)
        {
            // Act
            var response = await _client.GetAsync($"Currency/{currencyCode}/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("02/02/2020")]
        [InlineData("02\\02\\2020")]
        [InlineData("2020/02/02")]
        [InlineData("2020/02/0Z")]
        public async Task GetCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesHaveInDateTimeInvalidCharacter(string dateTime)
        {
            // Act
            var response = await _client.GetAsync($"Currency/Arg/{dateTime}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("TEST")]
        [InlineData("ARGentina")]
        [InlineData("mexico")]
        [InlineData(" ")]
        public async Task GetCurrency_ShouldBeHttpStatusCodeNotFound_WhenUrlDoesHaveCurrencyCodeInvalid(
            string currencyCode)
        {
            // Act
            var response = await _client.GetAsync($"Currency/{currencyCode}/2020-2-7");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

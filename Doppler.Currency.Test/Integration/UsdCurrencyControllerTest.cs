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
        public async Task GetUsdToday_ShouldBeHttpStatusCodeOk_WhenBnaServiceReturnCorrectly()
        {
            //Arrange
            _testServer.BnaServiceMock.Setup(x => x.GetUsdToday(It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(new EntityOperationResult<UsdCurrency>(new UsdCurrency
                {
                    BuyValue = "10",
                    SaleValue = "30",
                    Date = "21/12/2012"
                }));

            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency");                          
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
        public async Task GetUsdToday_ShouldBeHttpStatusCodeBadRequest_WhenBnaServiceReturnUsdCurrencyInCorrectly()
        {
            //Arrange
            var result = new EntityOperationResult<UsdCurrency>();
            result.AddError("Error","Html error");
            _testServer.BnaServiceMock.Setup(x => x.GetUsdToday(It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(result);

            // Act
            var client = _testServer.Client;
            var response = await client.GetAsync("UsdCurrency");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

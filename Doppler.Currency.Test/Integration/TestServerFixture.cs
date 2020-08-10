using System;
using System.Net.Http;
using System.Net.Http.Headers;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Services;
using Doppler.Currency.Test.Helper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Doppler.Currency.Test.Integration
{
    public class TestServerFixture : IDisposable
    {
        public TestServer Server { get; }

        public HttpClient Client { get; }

        public Mock<ICurrencyService> CurrencyServiceMock { get; }
        public Mock<ISlackHooksService> SlackHookServiceMock { get; }

        public TestServerFixture()
        {
            CurrencyServiceMock = new Mock<ICurrencyService>();
            SlackHookServiceMock = new Mock<ISlackHooksService>();

            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .ConfigureTestServices(services =>
                {
                    services.AddSingleton(CurrencyServiceMock.Object);
                    services.AddSingleton(SlackHookServiceMock.Object);
                    services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();
                });

            Server = new TestServer(builder);
            Client = Server.CreateClient();

            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
        }
    }
}
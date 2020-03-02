using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using CrossCutting;
using CrossCutting.SlackHooksService;
using Doppler.Currency.Logger;
using Doppler.Currency.Services;
using Doppler.Currency.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;

namespace Doppler.Currency
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<UsdCurrencySettings>("BnaService", Configuration.GetSection("BnaService"));
            services.Configure<UsdCurrencySettings>("DofService", Configuration.GetSection("DofService"));

            services.AddControllers()
                .AddJsonOptions(options => { options.JsonSerializerOptions.IgnoreNullValues = true; });

            var httpClientPolicies = new HttpClientPoliciesSettings();
            Configuration.GetSection("HttpClient:Client").Bind(httpClientPolicies);
            services.AddSingleton(httpClientPolicies);

            var handlerHttpClient = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                SslProtocols = SslProtocols.Tls12
            };

            services.AddHttpClient(httpClientPolicies.ClientName, c => { })
                .ConfigurePrimaryHttpMessageHandler(() => handlerHttpClient)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy(httpClientPolicies.Policies.RetryAttemps));

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Doppler Currency API",
                    Version = "v1",
                    Description = "API for Doppler Currency"
                });
            });

            services.AddTransient<ICurrencyService, CurrencyService>();

            AddServiceSettings(services);

            services.AddTransient<ISlackHooksService, SlackHooksService>();

            services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

            services.AddTransient<DofHandler>();
            services.AddTransient<BnaHandler>();
            services.AddTransient<IReadOnlyDictionary<CurrencyType, CurrencyHandler>>(sp => 
                new Dictionary<CurrencyType, CurrencyHandler>
                {
                    { CurrencyType.Arg, sp.GetRequiredService<BnaHandler>() },
                    { CurrencyType.Mex, sp.GetRequiredService<DofHandler>() }
                });
        }

        private void AddServiceSettings(IServiceCollection services)
        {
            var dofSettings = new UsdCurrencySettings();
            Configuration.GetSection("DofService").Bind(dofSettings);
            services.AddSingleton(dofSettings);

            var bnaSettings = new UsdCurrencySettings();
            Configuration.GetSection("BnaService").Bind(bnaSettings);
            services.AddSingleton(bnaSettings);

            var slackHookSettings = new SlackHookSettings();
            Configuration.GetSection("SlackHook").Bind(slackHookSettings);
            services.AddSingleton(slackHookSettings);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retry)
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying =
            {
                HttpStatusCode.RequestTimeout,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            };
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var cultureInfo = new CultureInfo("es-AR");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            app.UseStaticFiles();

            loggerFactory.AddFile("Logs/app-{Date}.log");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "BNA API V1");
            });
        }
    }
}

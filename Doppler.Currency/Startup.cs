using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using Doppler.Currency.DopplerSecurity;
using Doppler.Currency.Enums;
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
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CurrencySettings>("BnaService", Configuration.GetSection("CurrencyCode:BnaService"));
            services.Configure<CurrencySettings>("DofService", Configuration.GetSection("CurrencyCode:DofService"));
            services.Configure<CurrencySettings>("TrmService", Configuration.GetSection("CurrencyCode:TrmService"));

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
                .AddTransientHttpErrorPolicy(builder => GetRetryPolicy(httpClientPolicies.Policies.RetryAttemps));

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Doppler Currency API",
                    Version = "v1",
                    Description = "API for Doppler Currency"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.AddTransient<ICurrencyService, CurrencyService>();

            services.AddSlackHook();

            services.AddTransient<DofHandler>();
            services.AddTransient<BnaHandler>();
            services.AddTransient<TrmHandler>();
            services.AddTransient<IReadOnlyDictionary<CurrencyCodeEnum, CurrencyHandler>>(sp => 
                new Dictionary<CurrencyCodeEnum, CurrencyHandler>
                {
                    { CurrencyCodeEnum.Ars, sp.GetRequiredService<BnaHandler>() },
                    { CurrencyCodeEnum.Mxn, sp.GetRequiredService<DofHandler>() },
                    { CurrencyCodeEnum.Cop, sp.GetRequiredService<TrmHandler>() }
                });

            services.AddDopplerSecurity();
            services.AddCors();
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
            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(policy => policy
                .SetIsOriginAllowed(isOriginAllowed: _ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Swagger is disabled for int QA and prod because need set up for a reverse proxy
            if (env.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("v1/swagger.json", "Doppler Currency API V1");
                });
            }
        }
    }
}

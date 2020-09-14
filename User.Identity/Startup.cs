using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DnsClient;
using IdentityServer4.Services;
using IdentityServerCenter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resilience;
using User.Identity.Authentication;
using User.Identity.Dtos;
using User.Identity.Infrastructure;
using User.Identity.Services;
namespace User.Identity {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            //配置IdentityServer
            services.AddIdentityServer ()
                .AddExtensionGrantValidator<Authentication.SmsAuthCodeValidator> ()
                .AddDeveloperSigningCredential ()
                .AddInMemoryIdentityResources (Config.GetIdentityResources ())
                .AddInMemoryClients (Config.GetClients ())
                .AddInMemoryApiResources (Config.GetApiResource ());

            //获取Consul配置,映射为ServiceDisvoveryOptions对象
            services.Configure<ServiceDiscoveryOptions> (Configuration.GetSection ("ServiceDiscovery"));
            services.AddSingleton<IDnsQuery> (p => {
                var serviceConfiguration = p.GetRequiredService<IOptions<ServiceDiscoveryOptions>> ().Value;
                return new LookupClient (serviceConfiguration.Consul.DnsEndpoint.ToIPEndPoint ()); //IPAddress.Parse("127.0.0.1"), 8600
            });

            //注册全局单例 重试融断工厂
            services.AddSingleton (typeof (ResilienceClientFactory), sp => {
                var logger = sp.GetRequiredService<ILogger<ResilienceHttpClient>> ();
                var httpContextAccesser = sp.GetRequiredService<IHttpContextAccessor> ();
                var retryCount = 5;
                var exceptionCountAllowedBeforeBreaking = 5;
                return new ResilienceClientFactory (logger, httpContextAccesser, retryCount, exceptionCountAllowedBeforeBreaking);
            });

            //注册全局单例IHttpClient
            services.AddSingleton<IHttpClient> (sp => {
                return sp.GetRequiredService<ResilienceClientFactory> ().GetResilienceHttpClient ();
            });
            services.AddScoped<IUserService, UserService> ();
            services.AddScoped<IAuthCodeService, AuthCodeService> ();
            //用户配置Claims注册
            services.AddTransient<IProfileService, ProfileService> ();

            services.AddControllers ();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            }
            app.UseIdentityServer ();

            app.UseHttpsRedirection ();

            app.UseRouting ();

            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });
        }
    }
}
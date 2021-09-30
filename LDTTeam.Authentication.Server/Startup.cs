using System;
using FluffySpoon.AspNet.LetsEncrypt;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Extensions;
using LDTTeam.Authentication.Modules.Api.Logging;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Server.Config;
using LDTTeam.Authentication.Server.Data;
using LDTTeam.Authentication.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LDTTeam.Authentication.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddHttpClient();

            services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("postgres"),
                    b => b.MigrationsAssembly("LDTTeam.Authentication.Server")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedEmail = false;

                    options.User.RequireUniqueEmail = false;
                })
                .AddEntityFrameworkStores<DatabaseContext>();

            AuthenticationBuilder authBuilder = services.AddAuthentication();

            foreach (IModule module in Modules.List)
            {
                authBuilder = module.ConfigureAuthentication(Configuration, authBuilder);
                services = module.ConfigureServices(Configuration, services);
            }

            services.AddMemoryCache();
            
            services.AddTransient<IConditionService, ConditionService>();
            services.AddTransient<IRewardService, RewardService>();

            services.AddSingleton<EventsService>();
            services.AddStartupTask<EventsStartupTask>();
            services.AddStartupTask<DatabaseMigrationTask>();

            services.AddSingleton<IBackgroundEventsQueue>(_ =>
            {
                if (!int.TryParse(Configuration["EventsQueueCapacity"], out int queueCapacity))
                    queueCapacity = 10;
                return new BackgroundEventsQueue(queueCapacity);
            });
            
            services.AddSingleton<ILoggingQueue>(new LoggingQueue());

            services.AddHostedService<MetricsHistoryService>();
            services.AddHostedService<EventsQueueService>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            LetsEncryptConfig? config = Configuration.GetSection("LetsEncrypt").Get<LetsEncryptConfig>();

            if (config?.Enabled == true)
            {
                //the following line adds the automatic renewal service.
                services.AddFluffySpoonLetsEncrypt(new LetsEncryptOptions()
                {
                    Email = config.Email,
                    UseStaging = config.Staging,
                    Domains = new[] {config.Domain},
                    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30), //renew automatically 30 days before expiry
                    CertificateSigningRequest = config.Csr
                });

                services.AddFluffySpoonLetsEncryptFileCertificatePersistence();
                services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedProto
                });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            LetsEncryptConfig? config = Configuration.GetSection("LetsEncrypt").Get<LetsEncryptConfig>();

            if (config?.Enabled == true)
            {
                app.UseFluffySpoonLetsEncrypt();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
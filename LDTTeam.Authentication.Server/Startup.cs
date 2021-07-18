using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Extensions;
using LDTTeam.Authentication.Modules.Api.Webhook;
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

            services.AddSingleton<EventsService>();
            services.AddStartupTask<EventsStartupTask>();
            services.AddStartupTask<DatabaseMigrationTask>();

            services.AddSingleton<IBackgroundEventsQueue>(_ =>
            {
                if (!int.TryParse(Configuration["EventsQueueCapacity"], out int queueCapacity))
                    queueCapacity = 10;
                return new BackgroundEventsQueue(queueCapacity);
            });

            services.AddSingleton<IWebhookQueue>(_ =>
            {
                if (!int.TryParse(Configuration["LoggingQueueCapacity"], out int queueCapacity))
                    queueCapacity = 100;
                return new WebhookQueue(queueCapacity);
            });
            
            services.AddHostedService<EventsQueueService>();
            services.AddHostedService<WebhookLoggingQueueService>();
            
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = 
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedProto
                });
            
            if (false)//env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
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
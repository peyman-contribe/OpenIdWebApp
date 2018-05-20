using System;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Core;
using OpenIddict.Models;
using WebApiSocial.Data;
using WebApiSocial.Models;
using WebApiSocial.Services;

namespace WebApiSocial
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
            services.AddMvc();


            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                options.UseOpenIddict();
            });


            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();



            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });


            // Register the OpenIddict services.
            services.AddOpenIddict(options =>
            {
                // Register the Entity Framework stores.
                options.AddEntityFrameworkCoreStores<ApplicationDbContext>();

                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                options.AddMvcBinders();

                // Enable the authorization, logout, token and userinfo endpoints.
                options.EnableAuthorizationEndpoint("/connect/authorize")
                    .EnableLogoutEndpoint("/connect/logout")
                    .EnableTokenEndpoint("/connect/token");
                       //.EnableUserinfoEndpoint("/api/userinfo");

                // Note: the Mvc.Client sample only uses the authorization code flow but you can enable
                // the other flows if you need to support implicit, password or client credentials.
                options.AllowAuthorizationCodeFlow();
                //options.AllowImplicitFlow();

                // When request caching is enabled, authorization and logout requests
                // are stored in the distributed cache by OpenIddict and the user agent
                // is redirected to the same page with a single parameter (request_id).
                // This allows flowing large OpenID Connect requests even when using
                // an external authentication provider like Google, Facebook or Twitter.
                options.EnableRequestCaching();

                // During development, you can disable the HTTPS requirement.
                options.DisableHttpsRequirement();

                // Note: to use JWT access tokens instead of the default
                // encrypted format, the following lines are required:
                //
                //options.UseJsonWebTokens();
                //options.AddEphemeralSigningKey();
            });


            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                })

                .AddOAuthValidation();


            services.AddCors();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }


            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            InitializeAsync(app.ApplicationServices).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync(IServiceProvider services)
        {
            // Create a new service scope to ensure the database context is correctly disposed when this methods returns.
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();

                var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication>>();

                if (await manager.FindByClientIdAsync("MobileApp") == null)
                {
                    var descriptor = new OpenIddictApplicationDescriptor
                    {
                        ClientId = "MobileApp",
                        ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
                        DisplayName = "Mobile client application",
                        PostLogoutRedirectUris = { new Uri("https://localhost:44342/signout-callback-oidc") },
                        RedirectUris = { new Uri("https://localhost:44342/signin-oidc") },
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Authorization,
                            OpenIddictConstants.Permissions.Endpoints.Logout,
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                            OpenIddictConstants.Permissions.GrantTypes.RefreshToken
                        }
                    };

                    await manager.CreateAsync(descriptor);
                }

                if (await manager.FindByClientIdAsync("angular-app") == null)
                {
                    var descriptorTest = new OpenIddictApplicationDescriptor
                    {
                        ClientId = "test-app",
                        DisplayName = "Angular Application",
                        ClientSecret = "901564A5-E7GE-42CB-B10D-61EF6A8F3654",
                        PostLogoutRedirectUris = { new Uri("https://oidcdebugger.com/debug") },
                        RedirectUris = { new Uri("https://oidcdebugger.com/debug") },
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Authorization,
                            OpenIddictConstants.Permissions.Endpoints.Logout,
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                            OpenIddictConstants.Permissions.GrantTypes.RefreshToken
                        }
                    };


                    await manager.CreateAsync(descriptorTest);
                }
            }
        }
    }
}

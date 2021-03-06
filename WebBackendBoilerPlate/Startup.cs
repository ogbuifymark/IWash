﻿using BoilerPlate.DataLayer;
using BoilerPlate.DataLayer.Interface;
using BoilerPlate.ModelLayer;
using BoilerPlate.ModelLayer.Identity;
using BoilerPlate.ServiceLayer.EntityServices;
using BoilerPlate.ServiceLayer.EntityServices.Interfaces;
using BoilerPlate.ServiceLayer.Security;
using BoilerPlate.ServiceLayer.Security.Interface;
using BoilerPlate.ServiceLayer.Security.Models;
using Couchbase.Extensions.Caching;
using Couchbase.Extensions.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebBackendBoilerPlate.Infrastructure;

namespace WebBackendBoilerPlate
{
    public class Startup
    {
        public IHostingEnvironment Env { get; set; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddJsonOptions(
            options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        );


            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyMethod();
                    builder.AllowAnyHeader();
                    builder.AllowCredentials();
                });
            });

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory("AllowAllOrigins"));
            });

            // Register the Swagger generator, defining one or more Swagger documents           
            services.AddSwaggerGen(p =>
            {
                p.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info { Title = "Boiler Plate API", Version = "v1" });

                Dictionary<string, IEnumerable<string>> security = new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                };

                p.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                p.AddSecurityRequirement(security);

            });

            //Sql Server
            services.AddDbContext<BoilerPlateDbContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseLazyLoadingProxies().UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("WebBackendBoilerPlate"));

                options.ConfigureWarnings(c => c.Log(CoreEventId.DetachedLazyLoadingWarning));

            }
            );


            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("WebBackendBoilerPlate")));
            services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;

                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 1;
                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;
                // User settings
                options.User.RequireUniqueEmail = true;

            });

            JwtConfiguration jwtConfig = new JwtConfiguration();

            CouchyBaseConfig couchyBaseConfig = new CouchyBaseConfig();

            AuthMessageSenderOptions authMessageSenderOptions = new AuthMessageSenderOptions();


            Configuration.GetSection("SendGrid").Bind(authMessageSenderOptions);

            Configuration.GetSection("CouchyBaseCacheConfig").Bind(couchyBaseConfig);

            Configuration.GetSection("JWT").Bind(jwtConfig);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            })
            .AddCookie(options =>
            {

            })
           .AddJwtBearer(options =>
           {
               options.RequireHttpsMetadata = false;
               options.SaveToken = true;
               SymmetricSecurityKey serverSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.ServerSecret));
               options.TokenValidationParameters = new TokenValidationParameters
               {
                   IssuerSigningKey = serverSecret,
                   ValidIssuer = jwtConfig.Issuer,
                   ValidAudience = jwtConfig.Audience,
                   ClockSkew = TimeSpan.Zero,
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidateLifetime = true,
                   ValidateIssuerSigningKey = true,

               };
           });


            services.AddCouchbase(opt =>
            {
                opt.Servers = new List<Uri>
            {
                new Uri("http://127.0.0.1:8091")
            };

                opt.Username = couchyBaseConfig.Username;
                opt.Password = couchyBaseConfig.Password;
            });

            services.AddDistributedCouchbaseCache(couchyBaseConfig.BucketName, opt => { });




            if (Env.IsDevelopment())
            {

            }
            else if (Env.IsProduction())
            {

            }
            else if (Env.IsStaging())
            {

            }
            IServiceScopeFactory scopeFactory = services
                  .BuildServiceProvider()
                  .GetRequiredService<IServiceScopeFactory>();

            services.AddHangfire(
              x =>
              {
                  x.UseActivator(new ContainerJobActivator(scopeFactory));
                  x.UseSqlServerStorage(Configuration.GetConnectionString("DefaultConnection"));

              }
              );


            //D.I
            services.AddTransient<JwtAuthenticator>();
            services.AddSingleton(jwtConfig);
            services.AddSingleton(authMessageSenderOptions);

            services.AddTransient<IEmailConfirmationService, EmailConfirmationService>();

            services.AddTransient<IRegistrationService, RegistrationService>();
            services.AddTransient<ILoginService, LoginService>();
            services.AddTransient<IPasswordService, PasswordService>();

            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IIdentityUserService, IdentityUserService>();
            services.AddTransient<IJWTService, JWTService>();
            services.AddTransient<IRefreshTokenService, RefreshTokenService>();

            services.AddTransient<IIdentityUserService, IdentityUserService>();

            services.AddScoped<IUnitOfWork, UnitOfWork<BoilerPlateDbContext>>();
            services.AddTransient<IEmailSender, EmailSender>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            app.UseCors("AllowAllOrigins");
            app.UseAuthentication();


            applicationLifetime.ApplicationStarted.Register(() =>
            {

            });

            applicationLifetime.ApplicationStopped.Register(() =>
            {
                app.ApplicationServices.GetRequiredService<ICouchbaseLifetimeService>().Close();
            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    //swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://gateway.xend.tk/invoice" } };
                });
            });
            //Enable middleware to serve swagger - ui(HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Boiler Plate  Api V1");

                c.DocumentTitle = "Title Documentation";
                c.DocExpansion(DocExpansion.None);
            });

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            //app.UseHangfireServer();
            //app.UseHangfireDashboard();



            app.UseHttpsRedirection();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                 name: "areas",
                 template: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
               );
                routes.MapAreaRoute(
                    name: "AshirinContributor",
                    areaName: "Contributor",
                    template: "Services/{controller=Home}/{action=Index}/{id?}");

                routes.MapAreaRoute(
                   name: "AshirinAdministrator",
                   areaName: "Admin",
                   template: "Services/{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

           
        }
    }
}

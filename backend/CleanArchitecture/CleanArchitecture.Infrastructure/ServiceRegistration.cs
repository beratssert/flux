using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Wrappers;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using CleanArchitecture.Infrastructure.Repositories;
using CleanArchitecture.Infrastructure.Repository;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Security.Claims;
using System.Text;

namespace CleanArchitecture.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("ApplicationDb"));
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   configuration.GetConnectionString("DefaultConnection"),
                   b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }



            services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            #region Services
            services.AddTransient<IAccountService, AccountService>();
            #endregion
            services.Configure<JWTSettings>(configuration.GetSection("JWTSettings"));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = configuration["JWTSettings:Issuer"],
                        ValidAudience = configuration["JWTSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWTSettings:Key"]))
                    };
                    o.Events = new JwtBearerEvents()
                    {
                        OnAuthenticationFailed = c =>
                        {
                            c.NoResult();
                            c.Response.StatusCode = 500;
                            c.Response.ContentType = "text/plain";
                            return c.Response.WriteAsync(c.Exception.ToString());
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var result = "You are not Authorized";
                            return context.Response.WriteAsync(result);
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            var result = "You are not authorized to access this resource";
                            return context.Response.WriteAsync(result);
                        },
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Users.Read.Self", policy => policy.RequireAuthenticatedUser());
                options.AddPolicy("Users.Update.Self", policy => policy.RequireAuthenticatedUser());
                options.AddPolicy("Users.Read.Team", policy => policy.RequireRole(Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Users.Manage.All", policy => policy.RequireRole(Roles.Admin.ToString()));

                options.AddPolicy("Projects.Read.Assigned", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Projects.Manage.Own", policy => policy.RequireRole(Roles.Manager.ToString()));
                options.AddPolicy("Projects.Reassign.Manager", policy => policy.RequireRole(Roles.Admin.ToString()));

                options.AddPolicy("Assignments.Read.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Assignments.Manage.OwnProject", policy => policy.RequireRole(Roles.Manager.ToString()));

                options.AddPolicy("TimeEntries.Manage.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString()));
                options.AddPolicy("TimeEntries.Read.Team", policy => policy.RequireRole(Roles.Manager.ToString(), Roles.Admin.ToString()));

                options.AddPolicy("RunningTimers.Manage.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString()));

                options.AddPolicy("Expenses.Manage.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString()));
                options.AddPolicy("Expenses.Read.Team", policy => policy.RequireRole(Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Expenses.Reject.Team", policy => policy.RequireRole(Roles.Manager.ToString()));

                options.AddPolicy("Reports.Read.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Reports.Read.Team", policy => policy.RequireRole(Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Reports.Read.All", policy => policy.RequireRole(Roles.Admin.ToString()));

                options.AddPolicy("Reports.Export.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Reports.Export.Team", policy => policy.RequireRole(Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Reports.Export.All", policy => policy.RequireRole(Roles.Admin.ToString()));

                options.AddPolicy("Calendar.Read.Self", policy => policy.RequireRole(Roles.Employee.ToString(), Roles.Manager.ToString(), Roles.Admin.ToString()));
                options.AddPolicy("Calendar.Manage.OwnProject", policy => policy.RequireRole(Roles.Manager.ToString()));

                options.AddPolicy("Audit.Read.All", policy => policy.RequireRole(Roles.Admin.ToString()));
            });




            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.AddTransient<IDateTimeService, DateTimeService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IAuditService, AuditService>();


            #region Repositories
            services.AddTransient(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));
            services.AddTransient<IProductRepositoryAsync, ProductRepositoryAsync>();
            services.AddTransient<ICategoryRepositoryAsync, CategoryRepositoryAsync>();
            services.AddTransient<ITimeEntryRepositoryAsync, TimeEntryRepositoryAsync>();
            services.AddTransient<IExpenseRepositoryAsync, ExpenseRepositoryAsync>();
            services.AddTransient<IRunningTimerRepositoryAsync, RunningTimerRepositoryAsync>();
            services.AddTransient<IProjectAssignmentRepositoryAsync, ProjectAssignmentRepositoryAsync>();
            #endregion
        }
    }
}

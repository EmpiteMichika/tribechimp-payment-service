﻿using System;
using System.Data;
using System.IO;
using AutoMapper;
using Empite.Core.Infrastructure.Constant;
using Empite.Core.Middleware.Hmac;
using Empite.Core.Resilience;
using Empite.PaymentService.Data;
using Empite.PaymentService.Infrastructure.Filter;
using Empite.PaymentService.Interface;
using Empite.PaymentService.Interface.Service;
using Empite.PaymentService.Interface.Service.Zoho;
using Empite.PaymentService.Models.Configs;
using Empite.PaymentService.Services.PaymentService;

using Empite.PaymentService.Services.PaymentService.Zoho;
using Hangfire;
using Hangfire.MySql.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Queue;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.Polly;
using RawRabbit.Enrichers.Polly.Services;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using Swashbuckle.AspNetCore.Swagger;
using HangfireAuthorizationFilter = Empite.PaymentService.Infrastructure.Filter.HangfireAuthorizationFilter;
using MessageContext = Empite.PaymentService.Models.RabbitMq.MessageContext;

namespace Empite.PaymentService.Infrastructure
{
    public static class ApplicationBuilderExtension
    {
        private static IConfiguration Configuration { get; set; }
        private static Settings _settings;
        public static void ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            Configuration = configuration;
            _settings = Configuration.GetSection("Settings").Get<Settings>();
            services.Configure<Settings>(configuration.GetSection("Settings"));
            services.AddHttpClient();
            services.AddAutoMapper();
            services.InjectServices();
            services.ConfigureRabbitMq();
            services.AddEntitiyFramwork();
            services.AddHangfire(x => { });
            services.ConfigureAuthentication();
            services.AddSwaggerDocumentation();
            services.AddMvc();
            
        }

        public static void InjectServices(this IServiceCollection services)
        {
            /*====== INJECT APPLICATION UTILITIES =======*/
            services.AddTransient<IConnectionFactory, ConnectionFactory>();
            services.AddTransient<IHttpClient, ResilientHttpClient>();


            /*====== INJECT APPLICATION SERVICES =======*/
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            
            services.AddTransient<IZohoInvoiceSingleton, ZohoInvoiceSingletonTokenService>();
            services.AddTransient<IInvoiceWorkerService<ZohoInvoiceWorkerService>, ZohoInvoiceWorkerService>();
            services.AddTransient<IInvoceService, InvoceService>();
            services.AddTransient<IRecurringInvoiceService<ZohoRecurringInvoiceService>, ZohoRecurringInvoiceService>();
            /*====== INJECT APPLICATION REPOSITORIES =======*/
            //services.AddTransient<IUserRepository, UserRepository>();

            services.AddScoped<IDbInitializer, DbInitializer>();


            services.Configure<Settings>(Configuration.GetSection("Settings"));
            
        }

        public static void ConfigureRabbitMq(this IServiceCollection services)
        {
            services
                 .AddRawRabbit(new RawRabbitOptions
                 {
                     ClientConfiguration = new ConfigurationBuilder()
                         .SetBasePath(Directory.GetCurrentDirectory())
                         .AddJsonFile("Opt/Conf/rawrabbit.json")
                         .Build()
                         .Get<RawRabbitConfiguration>(),
                     Plugins = builder => builder
                         .UseGlobalExecutionId()
                         .UseHttpContext()
                         .UseAttributeRouting()
                         .UsePolly(new PolicyOptions
                         {
                             PolicyAction = context => context
                                 .UsePolicy(GetDefaultPolicy())
                                 .UsePolicy(GetQueuePolicy(), PolicyKeys.QueueBind),
                             ConnectionPolicies = new ConnectionPolicies
                             {
                                 Connect = Policy.Handle<BrokerUnreachableException>()
                                     .WaitAndRetryAsync(new[]
                                     {
                                        TimeSpan.FromSeconds(1),
                                        TimeSpan.FromSeconds(2),
                                        TimeSpan.FromSeconds(4),
                                        TimeSpan.FromSeconds(8)
                                     })
                             }
                         })
                         .UseMessageContext(context => new MessageContext
                         {
                             Source = context.GetHttpContext().Request.GetDisplayUrl()
                         })
                 });
        }

        public static IServiceCollection AddEntitiyFramwork(this IServiceCollection services)
        {
            var config = Configuration.GetSection(nameof(Settings)).Get<Settings>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));
            //services.AddSingleton<Func<ApplicationDbContext>>(() =>
            //{
            //    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //    optionsBuilder.UseMySql(Configuration.GetConnectionString("DefaultConnection"));
            //    return new ApplicationDbContext(optionsBuilder.Options);
            //});
            return services;
        }

        public static void ConfigureAuthentication(this IServiceCollection services)
        {
            var config = Configuration.GetSection(nameof(Settings)).Get<Settings>();
            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = config.IdentityUrl;
                    options.RequireHttpsMetadata = false;

                    options.ApiName = "tribechimpapi";
                }).AddHmac(ApplicationConstant.HMAC_AUTH_SCHEMA, options =>
                {
                    options.AppId = Configuration["Settings:AppId"];
                    options.SecretKey = Configuration["Settings:SecretKey"];
                }); 

        }

        private static Policy GetDefaultPolicy()
        {
            return Policy
                .Handle<Exception>()
                .RetryAsync((exception, retryCount, pollyContext) =>
                {
                    Console.WriteLine($"Default Called :  {exception.Message}");
                });
        }

        private static Policy GetQueuePolicy()
        {
            return Policy
                .Handle<OperationInterruptedException>()
                .RetryAsync(async (e, retryCount, context) =>
                {
                    var defaultQueueCfg = context.GetPipeContext().GetClientConfiguration().Queue;
                    var topology = context.GetTopologyProvider();
                    var queue = new QueueDeclaration(defaultQueueCfg) { Name = context.GetQueueName() };
                    await topology.DeclareQueueAsync(queue);
                });
        }

        public static void ConfigureApplicationPipeline(this IApplicationBuilder app, IHostingEnvironment env, IDbInitializer dbInitializer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            //Configuration = app.ApplicationServices.GetService<IConfiguration>();
            var config = Configuration.GetSection(nameof(Settings)).Get<Settings>();
            

            app.UseHttpsRedirection();

            app.Use(next => async context =>
            {
                var body = context.Request.Body;
                try
                {
                    context.Request.EnableRewind();
                    await next(context);
                }
                finally { context.Request.Body = body; }
            });
            GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(_settings.HangFireConnectionSettings.ToString(),
                new MySqlStorageOptions
                {
                    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    
                }));
            app.UseHangfireServer();
            app.UseHangfireDashboard("/schedules", new DashboardOptions { Authorization = new[] { new HangfireAuthorizationFilter() } });
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {

                IRecurringInvoiceService<ZohoRecurringInvoiceService> sender = serviceScope.ServiceProvider.GetRequiredService<IRecurringInvoiceService<ZohoRecurringInvoiceService>>();
                IInvoceService jobs = serviceScope.ServiceProvider.GetRequiredService<IInvoceService>();
                if (_settings.HangfireServiceConfig.CreateRecurringPayment)
                    RecurringJob.AddOrUpdate(() => sender.CreateRecurringInvoice(), Cron.Daily);
                if (_settings.HangfireServiceConfig.RunJobs)
                    RecurringJob.AddOrUpdate(() => jobs.RunJobs(), Cron.MinuteInterval(1));

            }
            app.UseSwagger();
            app.UseSwaggerUI(option =>
            {
                option.SwaggerEndpoint("/swagger/v1/swagger.json", "Tribechimp API V1");
                //option.InjectStylesheet("/themes/theme-material.css");
            });

            dbInitializer.Initialize().Wait();

            app.UseMvc();
        }

        
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            var config = Configuration.GetSection(nameof(Settings)).Get<Settings>();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(config.ApiSettings.Version, new Info
                {
                    Title = config.ApiSettings.Title,
                    Version = config.ApiSettings.Version,
                    Contact = new Swashbuckle.AspNetCore.Swagger.Contact { Email = config.ApiSettings.Contact, Name = "Empite" },
                    Description = config.ApiSettings.Description,
                    TermsOfService = config.ApiSettings.Toc
                });
                options.DescribeAllEnumsAsStrings();
                options.DocumentFilter<LowercaseDocumentFilter>();
                //options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new ApiKeyScheme()
                //{
                //    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                //    Name = "Authorization",
                //    In = "header",
                //    Type = "apiKey"
                //});
            });

            return services;
        }

    }
}

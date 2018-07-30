using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Empite.PaymentService.Infrastructure;
using Empite.TribechimpService.PaymentService.Infrastructure;
using Empite.TribechimpService.PaymentService.Domain.Interface.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Empite.TribechimpService.PaymentService
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
            services.ConfigureApplicationServices(Configuration);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IDbInitializer dbInitializer)
        {
            app.ConfigureApplicationPipeline(env, dbInitializer);
        }
    }
}

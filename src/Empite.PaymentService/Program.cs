using Empite.PaymentService.Services.PaymentService;
using Empite.PaymentService.Services.PaymentService.Zoho;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Empite.PaymentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("Opt/Conf/config.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    var isZohoRecurringInvoiceService = Matching.FromSource<ZohoRecurringInvoiceService>();
                    var isZohoInvoiceWorkerService = Matching.FromSource<ZohoInvoiceWorkerService>();
                    var isInvoiceService = Matching.FromSource<InvoceService>();
                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .WriteTo.Console(LogEventLevel.Information)
                        .WriteTo.RollingFile("Opt/Logs/log-{Date}.txt", fileSizeLimitBytes: null)
                        .WriteTo.Logger(x => x.Filter.ByIncludingOnly(isZohoRecurringInvoiceService).WriteTo.RollingFile("Opt/Logs/ZohoRecurringInvoiceService-{Date}.txt", LogEventLevel.Verbose))
                        .WriteTo.Logger(x => x.Filter.ByIncludingOnly(isZohoRecurringInvoiceService).WriteTo.RollingFile("Opt/Logs/ZohoRecurringInvoiceServiceErr-{Date}.txt", LogEventLevel.Error))
                        .WriteTo.Logger(x => x.Filter.ByIncludingOnly(isZohoInvoiceWorkerService).WriteTo.RollingFile("Opt/Logs/ZohoInvoiceWorkerService-{Date}.txt", LogEventLevel.Verbose))
                        .WriteTo.Logger(x => x.Filter.ByIncludingOnly(isZohoInvoiceWorkerService).WriteTo.RollingFile("Opt/Logs/ZohoInvoiceWorkerServiceErr-{Date}.txt", LogEventLevel.Error))
                        .WriteTo.Logger(x => x.Filter.ByIncludingOnly(isInvoiceService).WriteTo.RollingFile("Opt/Logs/InvoceService-{Date}.txt", LogEventLevel.Verbose))
                        .WriteTo.Logger(x => x.Filter.ByIncludingOnly(isInvoiceService).WriteTo.RollingFile("Opt/Logs/InvoceServiceErr-{Date}.txt", LogEventLevel.Error))
                        .CreateLogger();
                })
                .ConfigureLogging((hostingContext, logging) => { logging.AddSerilog(dispose: true); })
                .UseStartup<Startup>()
                .Build();
    }
}

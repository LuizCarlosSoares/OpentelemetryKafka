using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Trace;
using Utils.Messaging;
namespace Worker {
    public class Program {
        public static void Main (string[] args) {
            CreateHostBuilder (args).Build ().Run ();
        }

        public static IHostBuilder CreateHostBuilder (string[] args) =>
            Host.CreateDefaultBuilder (args)
            .ConfigureServices ((hostContext, services) => {

                services.AddHostedService<Worker> ();
                services.AddSingleton<MessageConsumer> ();

                services.AddOpenTelemetry ((builder) => builder
                    .AddActivitySource (nameof (MessageConsumer))
                    .UseZipkinExporter (b => {
                        var zipkinHostName = Environment.GetEnvironmentVariable ("ZIPKIN_HOSTNAME") ?? "localhost";
                        b.ServiceName = nameof (Worker);
                        b.Endpoint = new Uri ($"http://{zipkinHostName}:9411/api/v2/spans");
                    }));

            });
    }
}
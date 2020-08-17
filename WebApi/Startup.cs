using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Trace;
using Utils.Messaging;

namespace WebApi {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            services.AddControllers ();

            services.AddSingleton<MessageProducer> ();

            var exporter = this.Configuration.GetValue<string> ("UseExporter").ToLowerInvariant ();

            services.AddOpenTelemetry ((builder) => builder
                .AddAspNetCoreInstrumentation ()
                .AddActivitySource (nameof (MessageProducer))
                .UseZipkinExporter (b => {
                    var zipkinHostName = Environment.GetEnvironmentVariable ("ZIPKIN_HOSTNAME") ?? "localhost";
                    b.ServiceName = nameof (WebApi);
                    b.Endpoint = new Uri ($"http://{zipkinHostName}:9411/api/v2/spans");
                }));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            }

            app.UseHttpsRedirection ();

            app.UseRouting ();

            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });
        }
    }
}
using dotnetMySQL.Api.Installer;
using dotnetMySQL.Api.Middleware;
using dotnetMySQL.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using ElmahCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using dotnetMySQL.Api.OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
namespace dotnetMySQL.Api
{
    public class Startup
{
    public Startup(IConfiguration configuration, IHostEnvironment webHostEnvironment)
    {
        Configuration = configuration;
        WebHostEnvironment = webHostEnvironment;
    }


    public IConfiguration Configuration { get; }
    public IHostEnvironment WebHostEnvironment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
         if (Configuration["OpenTelemetry:isEnabled"] == "true")
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            services.AddOpenTelemetryTracing(
                    builder =>
                    {
                        builder
                            .SetResourceBuilder(OpentelemetryConfiguration.GetResourceBuilder(WebHostEnvironment))
                            .AddAspNetCoreInstrumentation(
                                options =>
                                {
                                    options.Enrich = OpentelemetryConfiguration.Enrich;
                                    options.RecordException = true;
                                })
                                 .AddHttpClientInstrumentation()
                                 .AddOtlpExporter(otlpOptions =>
                                    {
                                        otlpOptions.Endpoint = new Uri(Configuration["OpenTelemetry:OtlpExporterEndpoint"]);
                                        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                                    });
                        //For Directly exporting traces to jaeger
                        //    .AddJaegerExporter(opts =>
                        // {
                        //     opts.AgentHost = "0.0.0.0";
                        //     opts.AgentPort = 6831;
                        // })
                        //For Directly exporting traces to Application insights
                        //     .AddAzureMonitorTraceExporter(o =>
                        // {
                        //     o.ConnectionString = "<connectionstring>";
                        // });
                    });
        }
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(options =>
            {
                options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        string mySqlConnectionStr = Configuration.GetConnectionString("MySqlDBConnectionString");
        services.AddDbContextPool<DataContext>(options => options.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr)));

        APIInstaller apiInstaller = new APIInstaller(services, Configuration);
        apiInstaller.Install();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger(
            c => c.SerializeAsV2 = true
        );
        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "dotnetMySQL V1");
        });

        app.UseHttpsRedirection();

        app.UseCors();

        app.UseRouting();

        app.UseElmah();

        app.UseRequestLoggingMiddleWare();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
}

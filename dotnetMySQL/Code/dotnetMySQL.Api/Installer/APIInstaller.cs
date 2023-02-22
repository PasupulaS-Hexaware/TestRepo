using 
using System;
using ElmahCore;
using ElmahCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.OpenTelemetryProtocol;
using OpenTelemetry.Logs;
dotnetMySQL.Api.Filters;
using dotnetMySQL.Business.Installer;
using System;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.OpenTelemetryProtocol;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;

namespace dotnetMySQL.Api.Installer
{
    public class APIInstaller
{
    private IServiceCollection _service;
    public IConfiguration _configuration { get; }

    public APIInstaller(IServiceCollection service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    public void Install()
    {
        _service.AddControllers(options =>
            options.Filters.Add<HttpResponseExceptionFilter>());

        string version = _configuration.GetValue("BuildVersion", "0.0.0.0");
        string branch = _configuration.GetValue("BuildBransh", "local");
        // Register the Swagger generator, defining 1 or more Swagger documents
        _service.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "dotnetMySQL API", Version = version + "-" + branch });
        });

        _service.AddElmah<XmlFileErrorLog>(options =>
        {
            options.LogPath = "./log";
        });
        if (_configuration["OpenTelemetry:isEnabled"] == "true")
        {
            SetupOpenTelemetryLogging();
        }
        
        var serviceInstaller = new ServiceInstaller(_service);
        serviceInstaller.Install();
    }

    void SetupOpenTelemetryLogging()
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
   {
       builder
       .AddOpenTelemetry(options =>
       {
           options.AddConsoleExporter();
           options.AddOtlpExporter(otlpExporterOptions =>
                {
                    otlpExporterOptions.Endpoint = new Uri(_configuration["OpenTelemetry:OtlpExporterEndpoint"]);
                    otlpExporterOptions.Protocol = OtlpExportProtocol.Grpc;
                });

           options.IncludeFormattedMessage = true;
           options.IncludeScopes = true;
           options.ParseStateValues = true;
       });
   });
        _service.AddSingleton(loggerFactory);
    }
}
}
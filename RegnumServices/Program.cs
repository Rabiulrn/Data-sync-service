using RegnumServices;
using Serilog;
var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<RegWorkServ>();
//builder.Services.AddHostedService<RHDNTMCMigrationService>();
builder.Services.AddHostedService<RHDMigrationService>();
//builder.Services.AddHostedService<RHDImageService>();
//builder.Services.AddHostedService<UpayService>();
//builder.Services.AddHostedService<FTPFileTransferService>();
builder.Services.AddSerilog();
//builder.Services.AddSystemd(); for Linux
builder.Services.AddWindowsService();
var host = builder.Build();
var configsetting = new ConfigurationBuilder().
	AddJsonFile("appsettings.json").Build();
Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.MinimumLevel.Override("microsoft", Serilog.Events.LogEventLevel.Warning)
	.Enrich.FromLogContext()
	.WriteTo.File(configsetting["Logging:Logpath"])
	.CreateLogger();
host.Run();

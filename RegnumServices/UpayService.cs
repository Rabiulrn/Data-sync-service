using ApiFetcher;
using ApiFetcher.Ntmc;
using Entities.Models;
using ServiceManager;

namespace RegnumServices
{
    public class UpayService : BackgroundService
    {
        private readonly ILogger<UpayService> _logger;


        public UpayService(ILogger<UpayService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    UpayMigration altran = new UpayMigration();
                    await altran.Sync(LoadApiUrls("Upay"));

                    _logger.LogInformation("Service Started ALL TRANS");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
                ConfigSettings settings = new ConfigSettings();
                int InterValsofTime = Convert.ToInt32(settings.TimersSetting("Intervals"));
 
                await Task.Delay(1000 * InterValsofTime, stoppingToken);

            }
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped ALL TRANS");
            return base.StopAsync(cancellationToken);
        }
        private static List<string> LoadApiUrls(string bankName)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfiguration configuration = configurationBuilder.Build();
            var rocketApiUrlsSection = configuration.GetSection($@"API_Urls:{bankName}");
            var rocketApiUrls = new List<string>();
            rocketApiUrlsSection.Bind(rocketApiUrls);

            return rocketApiUrls;
        }

    }
}

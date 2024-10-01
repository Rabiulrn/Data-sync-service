using ApiFetcher;
using Entities.Models;
using ServiceManager;

namespace RegnumServices
{
    public class RHDImageService : BackgroundService
    {
        private readonly ILogger<RegWorkServ> _logger;


        public RHDImageService(ILogger<RegWorkServ> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConfigSettings settingss = new ConfigSettings();
                    string RHDSource = settingss.configSetting("RHDSource");
                    string RHDDestination = settingss.configSetting("RHDDestination");


                    // ImagesMigrationGomoti altran = new ImagesMigrationGomoti();
                    // await altran.Sync(RHDSource, RHDDestination);

                    ImagesMigrationCharsindur altran = new ImagesMigrationCharsindur();
                    await altran.Sync(RHDSource, RHDDestination);


                    //  ImagesMigration altran = new ImagesMigration();
                    //  await altran.Sync(RHDSource, RHDDestination);


                    _logger.LogInformation("Service Started");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
                ConfigSettings settings = new ConfigSettings();
                int InterValsofTime = Convert.ToInt32(settings.TimersSetting("Intervals"));
                //_logger.LogInformation("Service executed");
                await Task.Delay(1000 * InterValsofTime, stoppingToken);
            }
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                ConfigSettings settingss = new ConfigSettings();
                string RHDSource = settingss.configSetting("RHDSource");
                string RHDDestination = settingss.configSetting("RHDDestination");


              //  ImagesMigrationGomotiStart altran = new ImagesMigrationGomotiStart();
               // await altran.Sync(RHDSource, RHDDestination);
              //  AllImagesMeghnaStart altran = new AllImagesMeghnaStart();
              //  await altran.Sync(RHDSource, RHDDestination);

                //ImagesMigrationGomotiStart altran = new ImagesMigrationGomotiStart();
                // altran.Sync(RHDSource, RHDDestination);
                 // await altran.Sync(RHDSource, RHDDestination);

                AllImagesCharshindurStart altran = new AllImagesCharshindurStart();
                 altran.Sync(RHDSource, RHDDestination);

            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                throw;
            }

            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped");
            return base.StopAsync(cancellationToken);
        }


    }
}

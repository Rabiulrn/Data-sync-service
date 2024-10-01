using ApiFetcher;
using ApiFetcher.Ntmc;
using Entities.Models;
using ServiceManager;

namespace RegnumServices
{
    public class RHDNTMCMigrationService : BackgroundService
    {
        private readonly ILogger<RegWorkServ> _logger;


        public RHDNTMCMigrationService(ILogger<RegWorkServ> logger)
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
                    string tableName = settingss.ntmcSetting("Table_Name");
                    int plazaId = Convert.ToInt32(settingss.ntmcSetting("Plaza_id"));

                    NTMCMigration altran = new NTMCMigration();
                    await altran.Sync(RHDSource, RHDDestination, tableName, plazaId);
 
                    _logger.LogInformation("Service Started ALL NTMC");
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
            try
            {
                ConfigSettings settingss = new ConfigSettings();
                string RHDSource = settingss.configSetting("RHDSource");
                string RHDDestination = settingss.configSetting("RHDDestination");
                string tableName = settingss.ntmcSetting("Table_Name");
                int plazaId = Convert.ToInt32(settingss.ntmcSetting("Plaza_id"));


                NTMCMigrationStart altran = new NTMCMigrationStart();
                altran.Sync(RHDSource, RHDDestination, tableName,plazaId);


            }
            catch (Exception)
            {

                throw;
            }


            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped ALL NTMC");
            return base.StopAsync(cancellationToken);
        }


    }
}

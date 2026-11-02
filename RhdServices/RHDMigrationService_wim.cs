using ApiFetcher;
using ApiFetcher.Ntmc;
using Entities.Models;
using ServiceManager;

namespace RhdServices
{
    public class RHDMigrationService_wim : BackgroundService
    {
        //private readonly ILogger<RegWorkServ> _logger;

        //public RHDMigrationService(ILogger<RegWorkServ> logger)
        //{
        //    _logger = logger;
        //}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConfigSettings settingss = new ConfigSettings();
                    string RHDSource = settingss.configSetting("RHDSource");
                    string RHDDestination = settingss.configSetting("RHDDestination");
                    string MonthlyDestination = settingss.configSetting("MonthlyDestination");

                    //////////////////////// Plaza_trans_id///////////////////////////
                    string tableName = settingss.Plaza_Table_Setting("Table_Name");
                    string Bridge_Name = settingss.Plaza_Table_Setting("Bridge_Name");
                    int plazaId = Convert.ToInt32(settingss.Plaza_Table_Setting("Plaza_id"));
                    //Console.WriteLine(tableName);
                    //Console.WriteLine(plazaId);
                   // Paksey paksey = new Paksey();
                    using var allTransactionV4_wim = new AllTransactionV4_wim();
                    await allTransactionV4_wim.Sync(
                        RHDSource,
                        RHDDestination,
                        MonthlyDestination,
                        tableName,
                        plazaId,
                        Bridge_Name
                    );

                    //_logger.LogInformation("Service Started ");
                }
                catch (Exception ex)
                {
                   // _logger.LogError(ex, ex.Message);
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
                string MonthlyDestination = settingss.configSetting("MonthlyDestination");

                //////////////////////// Plaza_trans_id///////////////////////////
                string tableName = settingss.Plaza_Table_Setting("Table_Name");
                string Bridge_Name = settingss.Plaza_Table_Setting("Bridge_Name");
                int plazaId = Convert.ToInt32(settingss.Plaza_Table_Setting("Plaza_id"));
               // PakseyStart pakseyStart = new PakseyStart();
                 AllTransactionSrartV3_wim allTransactionSrartV3_wim = new AllTransactionSrartV3_wim();
                    allTransactionSrartV3_wim.Sync( RHDSource, RHDDestination, MonthlyDestination,tableName, plazaId,  Bridge_Name  );
                // pakseyStart.Sync(RHDSource, RHDDestination, MonthlyDestination, tableName, plazaId, Bridge_Name);
            }
            catch (Exception)
            {
                throw;
            }

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // _logger.LogInformation("Service stopped ALL TRANS");
           // _logger.LogInformation("Service stopped ");
            return base.StopAsync(cancellationToken);
        }
    }
}

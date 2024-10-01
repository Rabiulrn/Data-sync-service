using ApiFetcher;
using ApiFetcher.Ntmc;
using Entities.Models;
using ServiceManager;

namespace RegnumServices
{
    public class RHDMigrationService : BackgroundService
    {
        private readonly ILogger<RegWorkServ> _logger;


        public RHDMigrationService(ILogger<RegWorkServ> logger)
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

                    //////////////////////// Plaza_trans_id///////////////////////////
                    //string tableName = settingss.Plaza_Table_Setting("Table_Name");
                    //string Bridge_Name = settingss.Plaza_Table_Setting("Bridge_Name");
                    //int plazaId = Convert.ToInt32(settingss.Plaza_Table_Setting("Plaza_id"));
                    ////Console.WriteLine(tableName);
                    ////Console.WriteLine(plazaId);

                    //TransIdAllTransaction trans = new TransIdAllTransaction();
                    //await trans.Sync(RHDSource, RHDDestination, tableName, plazaId, Bridge_Name);
                    //////////////////////////////////////////////////////////////////////////////////



                    /////////////////////////////////////////////////////Cancel//////////////////////////////////
                    //string tableName = settingss.Plaza_Table_Setting("Table_Name");
                    //string Bridge_Name = settingss.Plaza_Table_Setting("Bridge_Name");
                    //int plazaId = Convert.ToInt32(settingss.Plaza_Table_Setting("Plaza_id"));

                    //CancelAllTransactionMigrations cancelAllTransactionExec = new CancelAllTransactionMigrations();
                    //await cancelAllTransactionExec.Sync(RHDSource, RHDDestination, tableName, plazaId, Bridge_Name);





                    //ConfigSettings settingss = new ConfigSettings();
                    //string RHDSource = settingss.configSetting("RHDSource"); ////RHDSource json string in json file
                    //string RHDDestination = settingss.configSetting("RHDDestination"); 


                    //string[] RHDSourceArray = settingss.configSettingArray("RHDSource2"); //RHDSource2 json array in json file

                    //foreach (var connString in RHDSourceArray)
                    //{
                    //    VehicleRegistrationMigration vehicleRegistrationID = new VehicleRegistrationMigration();
                    //    await vehicleRegistrationID.Sync(connString, RHDDestination);

                    //}


                    //AllTransactionMigrationsVanga vanga = new AllTransactionMigrationsVanga();
                    //vanga.Sync(RHDSource, RHDDestination);


                    //AllTransactionMigrationsDhaleshwari altranID = new AllTransactionMigrationsDhaleshwari();
                    //altranID.Sync(RHDSource, RHDDestination);


                    VehicleRegistrationMigration vehicle = new VehicleRegistrationMigration();
                    vehicle.Sync(RHDSource, RHDDestination);
                    //AllTransactionMigrationsMeghna altran = new AllTransactionMigrationsMeghna();
                    //altran.Sync(RHDSource, RHDDestination);




                    //  _logger.LogInformation("Service Started SHITALOKHYA");
                    // _logger.LogInformation("Service Started ALL TRANS");
                    _logger.LogInformation("Service Started ");
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

                //////////////////////// Plaza_trans_id///////////////////////////
                //string tableName = settingss.Plaza_Table_Setting("Table_Name");
                //string Bridge_Name = settingss.Plaza_Table_Setting("Bridge_Name");
                //int plazaId = Convert.ToInt32(settingss.Plaza_Table_Setting("Plaza_id"));


                //TransIdAllTransactionSrart transIdAll = new TransIdAllTransactionSrart();
                //transIdAll.Sync(RHDSource, RHDDestination, tableName, plazaId, Bridge_Name);
                ////         //////////////////////////////////////////////////////////////////////////////////

                /////////////////// IS_UPDATED_CANCEL//////////////////////////
                ///
                //string tableName = settingss.Plaza_Table_Setting("Table_Name");
                //string Bridge_Name = settingss.Plaza_Table_Setting("Bridge_Name");
                //int plazaId = Convert.ToInt32(settingss.Plaza_Table_Setting("Plaza_id"));
                //CancelAllTransactionMigrationsStart cancelAllTransaction = new CancelAllTransactionMigrationsStart();
                //cancelAllTransaction.Sync(RHDSource, RHDDestination, tableName, plazaId, Bridge_Name);






                //string[] RHDSourceArray = settingss.configSettingArray("RHDSource2");

                //foreach (var connString in RHDSourceArray)
                //{
                //    VehicleRegistrationMigrationStart vehicleRegistration = new VehicleRegistrationMigrationStart();
                //    vehicleRegistration.Sync(connString, RHDDestination);

                //}

                //AllTransactionMigrationsVangaStart allTransactionMigrationsVangaStart = new AllTransactionMigrationsVangaStart();
                //allTransactionMigrationsVangaStart.Sync(RHDSource, RHDDestination); 

                //AllTransactionMigrationsDhaleshwariStart altranD = new AllTransactionMigrationsDhaleshwariStart();
                //altranD.Sync(RHDSource, RHDDestination);

                VehicleRegistrationMigrationStart vehicle = new VehicleRegistrationMigrationStart();
                vehicle.Sync(RHDSource,RHDDestination);





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
            _logger.LogInformation("Service stopped ");
            return base.StopAsync(cancellationToken);
        }
    }
}

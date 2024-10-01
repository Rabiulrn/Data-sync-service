using ApiFetcher;
using Entities.Models;
using ServiceManager;

namespace RegnumServices
{
    public class RegWorkServ : BackgroundService
    {
        private readonly ILogger<RegWorkServ> _logger;


        public RegWorkServ(ILogger<RegWorkServ> logger)
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
                    string conString = settingss.configSetting("DBBackup");

                    QuerySettingDTO obj = new QuerySettingDTO()
                    {
                        JobName = settingss.QuerySetting("JobName"),
                        DMPFileName = settingss.QuerySetting("DMPFileName"),
                        LogFileName = settingss.QuerySetting("LogFileName"),
                        DirectoryName = settingss.QuerySetting("DirectoryName"),
                        DBUserName = settingss.QuerySetting("DBUserName"),

                    };

                    DBBackUpModule regWork = new DBBackUpModule();
                    regWork.SyncDBBackups(conString, obj);

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
            ConfigSettings settings = new ConfigSettings();
            string conString = settings.configSetting("DBBackup");

            QuerySettingDTO obj = new QuerySettingDTO()
            {
                JobName = settings.QuerySetting("JobName"),
                DMPFileName = settings.QuerySetting("DMPFileName"),
                LogFileName = settings.QuerySetting("LogFileName"),
                DirectoryName = settings.QuerySetting("DirectoryName"),
                DBUserName = settings.QuerySetting("DBUserName"),

            };

            DBBackUpModule regWork = new DBBackUpModule();
            regWork.SyncDBBackups(conString, obj);

            _logger.LogInformation("Service Started");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped");
            return base.StopAsync(cancellationToken);
        }


    }
}

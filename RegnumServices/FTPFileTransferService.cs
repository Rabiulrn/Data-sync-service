using ApiFetcher;
using Entities.Models;
using ServiceManager;

namespace RegnumServices
{
    public class FTPFileTransferService : BackgroundService
    {
        private readonly ILogger<UpayService> _logger;


        public FTPFileTransferService(ILogger<UpayService> logger)
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

                    FTPSettingsDTO obj = new FTPSettingsDTO()
                    {
                        ftpServer = settingss.FTPSettings("ftpServer"),
                        ftpUsername = settingss.FTPSettings("ftpUsername"),
                        ftpPassword = settingss.FTPSettings("ftpPassword"),
                        directoryPath = settingss.FTPSettings("directoryPath"),

                    };

                    FtpFileTransferGomoti ftpFileTransfer = new FtpFileTransferGomoti();
                  await  ftpFileTransfer.SyncTransfer(obj.ftpServer, obj.ftpUsername, obj.ftpPassword, obj.directoryPath);

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
            

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped");
            return base.StopAsync(cancellationToken);
        }


    }
}

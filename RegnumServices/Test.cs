using ApiFetcher;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegnumServices
{
    public class Test
    {
        public void TestAllFunctions()
        {
            try
            {
                ConfigSettings settings = new ConfigSettings();
                string conString = settings.configSetting("DBBackup");
                ConfigSettings settingsqUERY = new ConfigSettings();

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

            }
            catch (Exception)
            {

                throw;
            }
           
        }
    }
}

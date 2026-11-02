using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhdServices
{
    public class NotificationConfigSettings
    {
        public string ConfigSetting(string connectionStringName)
        {
            var configsetting = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            return configsetting[$"ConnectionStrings:{connectionStringName}"];
        }
      //  public string[] configSettingArray(string ConnectionStringName)
      //  {
      //      var configsetting = new ConfigurationBuilder()
      //.AddJsonFile("appsettings.json")
      //.Build();

      //      // Retrieve the array of connection strings
      //      //var connectionStrings = configsetting.GetSection($"ConnectionStrings:{ConnectionStringName}")
      //      //                                      .Get<string[]>();
      //      //string[] connectionStrings = configsetting.GetSection($"ConnectionStrings:{ConnectionStringName}")
      //      //                                     .Get<string[]>();

      //     // return connectionStrings;

      //  }
        public string SmtpSettings(string SmtpSettings)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"SmtpSettings:{SmtpSettings}"].ToString();

        }
        public string ntmcSetting(string ConnectionStringName)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"NTMC:{ConnectionStringName}"].ToString();

        }
        public string ntmcSettingPlazaId(string ConnectionStringName)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"NTMC:{ConnectionStringName}"].ToString();

        }

        public string QuerySetting(string valueName)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"DB_Query_Values:{valueName}"].ToString();

        }
        public string FTPSettings(string valueName)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"FTPServiceSetiings:{valueName}"].ToString();

        }
        public string TimersSetting(string timeInterval)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"LoggingTimes:{timeInterval}"].ToString();

        }
        public string SMTPSetting(string SmtpSettings)
        {
            var configsetting = new ConfigurationBuilder().
            AddJsonFile("appsettings.json").Build();
            return configsetting[$@"SmtpSettings:{SmtpSettings}"].ToString();

        }

    }
}

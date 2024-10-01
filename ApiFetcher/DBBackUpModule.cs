

using Entities.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;


namespace ApiFetcher
{
    public class DBBackUpModule
    {
       protected static string connStringHQ = "";

        public async Task SyncDBBackups(string getConString, QuerySettingDTO settingDTO)
        {
            try
            {
				connStringHQ = getConString;
                SchemaBackUp(settingDTO);

                LogWritter("BackUpSuccessfull");

            }
            catch (Exception ex)
            {

                LogWritter("BackUp error:" + ex.Message);

            }

        }
        public void SchemaBackUp(QuerySettingDTO settingDTO)
        {

            using (OracleConnection connection = new OracleConnection(connStringHQ))
            {
                try
                {
                    connection.Open();

                    using (OracleCommand command = connection.CreateCommand())
                    {
                       
                        command.CommandText = $@"DECLARE
                          h1 NUMBER;
                        BEGIN
                          h1 := DBMS_DATAPUMP.OPEN(operation => 'EXPORT', job_mode => 'SCHEMA', job_name => '{settingDTO.JobName}');
                          DBMS_DATAPUMP.ADD_FILE(handle => h1, filename => '{settingDTO.DMPFileName}.dmp', directory => '{settingDTO.DirectoryName}', filetype => dbms_datapump.ku$_file_type_dump_file);
                          DBMS_DATAPUMP.ADD_FILE(handle => h1, filename => '{settingDTO.LogFileName}.log', directory => '{settingDTO.DirectoryName}', filetype => dbms_datapump.ku$_file_type_log_file);
                          DBMS_DATAPUMP.METADATA_FILTER(handle => h1, name => 'SCHEMA_EXPR', value => '= ''{settingDTO.DBUserName}''');
                          DBMS_DATAPUMP.START_JOB(handle => h1);
                          DBMS_DATAPUMP.DETACH(handle => h1);
                        END;";
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw;
                   
                }
                finally
                {
                    connection.Close();
                }
            }
        }


        public void LogWritter(string message)
        {
            string filename = DateTime.Now.ToString("yyyy_MM_dd") + "_DBBackUpLog.log";
            string appPath = $@"D:\\demolog";
            int index = appPath.LastIndexOf("\\");
            appPath = appPath.Remove(index);
            string path = Path.Combine(appPath, "Log" + DateTime.Now.Year.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            File.AppendAllText(path + "\\" + filename, "\r\n" + DateTime.Now.ToString("dd HH:mm:ss.f") + "\t" + message);

        }
	}
}

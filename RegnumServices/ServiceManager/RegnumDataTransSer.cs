using Microsoft.Extensions.Configuration;

using Oracle.ManagedDataAccess.Client;
using RegnumServices;
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


namespace ServiceManager
{
    public class RegnumDataTransSer
    {

        static string Shitolokha = "";
        static string Regnum = "";

		public void GetConStings()
		{
			ConfigSettings configSettingsA = new ConfigSettings();
            Shitolokha = configSettingsA.configSetting("Shitolokha");
			ConfigSettings configSettingsB = new ConfigSettings();
            Regnum = configSettingsB.configSetting("Regnum");
		}
       
        public void SyncData()
        {

            try
            {
				GetConStings();
				SchemaBackUp();

				//LogWritter("Data Pulling from Shitolokha");
				//var data = GetDataShitolokha();
				//if (data.Rows.Count > 0)
				//{
				//	LogWritter("Total  Count: " + data.Rows.Count);
				//}

				//BulkInsert(data, "TEMP_ALL_TRANSACTIONS_TB");
				//ProcessTempDataBehHicleReg();
				//LogWritter("Data Synced");

			}
            catch (Exception ex)
            {
				
                LogWritter("Data Pulling error:" + ex.Message);
				throw;

            }

        }

        private DataTable GetDataShitolokha()
		{// adapter.Fill(dt);
			var dataTable = new DataTable();
            string query = $@"SELECT 
    TRANSACTION_ID,
    REGISTRATION_NUMBER,
    AMOUNT,
    TRANSACTION_NUMBER,
    VEHICLE_CLASS,
    VEHICLE_ID,
    LANE_NO,
    IMAGE_NAME,
    BANK_ID,
    PAYMENT_TYPE_ID,
    USER_ID,
    RECOGNIZE_BY,
    PLAZA_ID,
    RFID_NO,
    CLASS_STATUS,
    TRANSACTIN_ID_BY_BANK,
    IS_ACTIVE,
    STATUS_REMARKS,
    CREATED_AT,
    UPDATED_AT,
    CANCELED_AT,
    CANCELED_BY
FROM 
    all_transactions_tb
";
			using (OracleConnection connection = new OracleConnection(Shitolokha))
			{
				connection.Open();

				using (OracleCommand cmd = new OracleCommand(query, connection))
				{
					using (OracleDataReader reader = cmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							// Add columns to DataTable based on reader's schema
							for (int i = 0; i < reader.FieldCount; i++)
							{
								dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
							}

							// Read data and add rows to DataTable
							while (reader.Read())
							{
								DataRow dataRow = dataTable.NewRow();

								for (int i = 0; i < reader.FieldCount; i++)
								{
									dataRow[i] = reader[i];
								}

								dataTable.Rows.Add(dataRow);
							}
						}
					}
				}
			}
			return dataTable;
        }
		private void ProcessTempDataBehHicleReg()
		{
			string query = @"MERGE INTO etc_vehicles_registration_tb dest
	USING (
    SELECT 
        MOBILE_NO AS MOBILE_NO,
        LICENCE_NO AS LICENCE_NO,
        VEHICLE_CLASS AS VEHICLE_CLASS,
        AC AS AC
    FROM temp_registration_vehicle
) src
ON (dest.id = src.id)
WHEN MATCHED THEN
    UPDATE SET
        dest.MOBILE_NUMBER = src.MOBILE_NO,
        dest.REGISTRATION_NUMBER = src.LICENCE_NO,
        dest.WALLET_NUMBER = src.AC,
        dest.RHD_CLASS = src.VEHICLE_CLASS
WHEN NOT MATCHED THEN
    INSERT (
        MOBILE_NO, 
        REGISTRATION_NUMBER,
        WALLET_NUMBER,
        RHD_CLASS
    ) VALUES (
		src.MOBILE_NO,
        src.LICENCE_NO,
        src.AC,
        src.VEHICLE_CLASS
    )";

			var connection = new OracleConnection(Regnum);

			try
			{
				//LogWritter(string.Format("Total Records : {0} for {1} Table", "AbxCustomerDetails"));

				connection.Open();
				var comm = new OracleCommand();
				comm.Connection = connection;
				comm.CommandText = query;
				comm.CommandType = CommandType.Text;
				comm.ExecuteNonQuery();

			}
			catch (Exception ex)
			{

				throw ex;
			}
			finally
			{
				connection.Close();
			}

		}
		public void BulkInsert(DataTable dataTable, string tablname)
		{
			if (dataTable == null)
			{
				LogWritter(string.Format("Data Not found for {0} Table", tablname));

				return;
			}
			var connection = new OracleConnection(Regnum);
			try
			{
				LogWritter(string.Format("Total Records : {0} for {1} Table", dataTable.Rows.Count, tablname));

				connection.Open();
				var comm = new OracleCommand();
				comm.Connection = connection;
				comm.CommandText = "Delete from " + tablname;
				comm.CommandType = CommandType.Text;
				comm.ExecuteNonQuery();
				comm.CommandTimeout = 1000 * 60 * 5;

				using (var bulk = new OracleBulkCopy(connection))
				{
					bulk.DestinationTableName = tablname;
					bulk.BulkCopyTimeout = 300000;
					bulk.WriteToServer(dataTable);

				}

			}
			catch (Exception ex)
			{

				LogWritter(string.Format("{1} Table: {0}", ex.Message, tablname));

			}
			finally
			{
				connection.Close();
			}
		}
		
		public void LogWritter(string message)
        {

            string filename = DateTime.Now.ToString("yyyy_MM_dd") + "_serviceEquipment.log";
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

		public void SchemaBackUp()
		{
			try
			{
                string dmpName = DateTime.Now.ToString() + ".dmp";
                string backupFilePath = "D:\\dumptest_two\\" + dmpName;

                string schemaName = "rhdtest";
                GetConStings();
                // Create OracleConnection
                using (OracleConnection connection = new OracleConnection(Regnum))
                {
                    try
                    {
                        connection.Open();

                        // Create OracleCommand to execute the EXPDP (Data Pump) command
                        using (OracleCommand command = connection.CreateCommand())
                        {
                            // Set the command text with EXPDP command
                            //command.CommandText = $"EXPDP '{schemaName}'/'rhdtest'@10.0.1.26:1521/ORCL SCHEMAS='{schemaName}' DIRECTORY='DUMPTEST_TWODIR' DUMPFILE={backupFilePath};";
                            command.CommandText = "host expdp rhdtest/rhdtest@10.0.1.26:1521/ORCL DIRECTORY = DUMPTEST_TWODIR  DUMPFILE =exp_schm_scott.dmp  LOGFILE=scott_lg.log Rows=y;";

                            // Execute the EXPDP command
                            command.ExecuteNonQuery();

                            Console.WriteLine($"Backup completed successfully. Backup file saved at: {backupFilePath}");
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
			catch (Exception)
			{

				throw;
			}
		
		}
		private void asdas()
		{
            string connectionString = "your_connection_string_here";

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                // Open the connection
                connection.Open();

                // Create a command with an INSERT statement that returns the generated primary key
                string insertQuery = "INSERT INTO YourTableName (column1, column2) VALUES (:value1, :value2) RETURNING yourPrimaryKeyColumn INTO :primaryKey";
                OracleCommand command = new OracleCommand(insertQuery, connection);

                // Set the parameter values
                command.Parameters.Add("value1", OracleDbType.Varchar2).Value = "some_value";
                command.Parameters.Add("value2", OracleDbType.Varchar2).Value = "another_value";

                // Define the output parameter for the generated primary key
                OracleParameter primaryKeyParameter = new OracleParameter("primaryKey", OracleDbType.Int32);
                primaryKeyParameter.Direction = System.Data.ParameterDirection.Output;
                command.Parameters.Add(primaryKeyParameter);

                // Execute the command
                command.ExecuteNonQuery();

                // Retrieve the generated primary key value
                int generatedPrimaryKey = Convert.ToInt32(command.Parameters["primaryKey"].Value);
                Console.WriteLine("Generated Primary Key: " + generatedPrimaryKey);
            }
        }
	}
}

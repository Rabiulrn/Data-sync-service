

using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
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
using System.Transactions;


namespace ApiFetcher
{
    public class VehicleRegistrationMigrationShitalokhya
    {

        static string _Source = "";
        static string _Destination = "";

        public async Task Sync(string Source,string Destination)
        {
           

            try
            {
                LogWritter("Vehicle Registration Data Pulling from Shitalokhya");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;
                    if (Convert.ToInt32(GetLastSyncTime()) > 0)
                    {

                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter("Total Vehicle Registration Count From Shitalokhya: " + data.Rows.Count);
                        }

                        BulkInsert(data, "TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA");
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("Vehicle Registration New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }
                        InsertDataTableToDatabase(insertData);
                        LogWritter("Data Synced SHITALOKHYA Vehicle Registrations");
                    }
                    else
                    {
                        LogWritter("No Data Found in TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA");
                    }
                        

                }
                

            }
            catch (Exception ex)
            {

                LogWritter("Data Pulling error:" + ex.Message);
				throw;
            }

        }

        private DataTable GetDataLocal()
		{// adapter.Fill(dt);
			try
			{
                var lastSyncTime = Convert.ToInt32(GetLastSyncTime());
                var dataTable = new DataTable();
                string query = $@"SELECT 
                                VEHICLE_REG_ID,
                                REGISTRATION_NUMBER,
                                WALLET_NUMBER,
                                MOBILE_NUMBER,
                                BANK_ID,
                                COMPANY_NAME,
                                BRTA_SERIES,
                                BRTA_SERIES_BN_NAME,
                                BRTA_CLASS,
                                BRTA_TYPE,
                                RHD_SERIES,
                                RHD_SERIES_BN_NAME,
                                RHD_CLASS,
                                RFID_NO,
                                UNLADEN_WEIGHT,
                                LADEN_WEIGHT,
                                TOTAL_AXLE,
                                OWNER_NAME,
                                VEHICLE_CC,
                                PAYMENT_TYPE_ID,
                                CREATED_AT,
                                UPDATED_AT
                            FROM 
                                VEHICLES_REGISTRATION_TB";

                if (lastSyncTime > 0)
                {
                    query += $" WHERE VEHICLE_REG_ID > :lastSyncTime";
                    LogWritter("Get Local and inside if  + " + lastSyncTime);
                }

                using (OracleConnection connection = new OracleConnection(_Source))
                {
                    connection.Open();

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        if (lastSyncTime > 0)
                        {
                            cmd.Parameters.Add(new OracleParameter("lastSyncTime", lastSyncTime));
                        }
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
                    connection.Close();
                }

                return dataTable;
            }
			catch (Exception)
			{
                throw;
			}
		
        }
        private DataTable ProcessTempDataBehHicleRegGet()
        {
          try
            {
                var lastId = Convert.ToInt32(GetLastSyncTime());
                var dataTable = new DataTable();
                string query = $@"SELECT
    TVR.VEHICLE_REG_ID,
    TVR.REGISTRATION_NUMBER,
    TVR.WALLET_NUMBER,
    TVR.MOBILE_NUMBER,
    TVR.BANK_ID,
    TVR.COMPANY_NAME,
    TVR.BRTA_SERIES,
    TVR.BRTA_SERIES_BN_NAME,
    TVR.BRTA_CLASS,
    TVR.BRTA_TYPE,
    TVR.RHD_SERIES,
    TVR.RHD_SERIES_BN_NAME,
    TVR.RHD_CLASS,
    TVR.RFID_NO,
    TVR.UNLADEN_WEIGHT,
    TVR.LADEN_WEIGHT,
    TVR.TOTAL_AXLE,
    TVR.OWNER_NAME,
    TVR.VEHICLE_CC,
    TVR.PAYMENT_TYPE_ID,
    TVR.CREATED_AT,
    TVR.UPDATED_AT
FROM
    TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA TVR
LEFT JOIN
    VEHICLES_REGISTRATION_TB VR
ON
    TVR.VEHICLE_REG_ID = VR.VEHICLE_REG_ID
WHERE
     TVR.IS_INSERTED IS NULL OR TVR.IS_INSERTED = 0
  AND VR.REGISTRATION_NUMBER IS NULL";
                using (OracleConnection connection = new OracleConnection(_Destination))
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
                    connection.Close();
                }
               return dataTable;
            }
			catch (Exception)
			{

				throw;
			}
        }

        public void InsertDataTableToDatabase(DataTable dataTable)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_Destination))
                {
                    connection.Open();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string query = @"INSERT INTO VEHICLES_REGISTRATION_TB
                (
                    REGISTRATION_NUMBER,
                    WALLET_NUMBER,
                    MOBILE_NUMBER,
                    BANK_ID,
                    COMPANY_NAME,
                    BRTA_SERIES,
                    BRTA_SERIES_BN_NAME,
                    BRTA_CLASS,
                    BRTA_TYPE,
                    RHD_SERIES,
                    RHD_SERIES_BN_NAME,
                    RHD_CLASS,
                    RFID_NO,
                    UNLADEN_WEIGHT,
                    LADEN_WEIGHT,
                    TOTAL_AXLE,
                    OWNER_NAME,
                    VEHICLE_CC,
                    PAYMENT_TYPE_ID,
                    CREATED_AT,
                    UPDATED_AT
                )
                VALUES
                (
                    :REGISTRATION_NUMBER,
                    :WALLET_NUMBER,
                    :MOBILE_NUMBER,
                    :BANK_ID,
                    :COMPANY_NAME,
                    :BRTA_SERIES,
                    :BRTA_SERIES_BN_NAME,
                    :BRTA_CLASS,
                    :BRTA_TYPE,
                    :RHD_SERIES,
                    :RHD_SERIES_BN_NAME,
                    :RHD_CLASS,
                    :RFID_NO,
                    :UNLADEN_WEIGHT,
                    :LADEN_WEIGHT,
                    :TOTAL_AXLE,
                    :OWNER_NAME,
                    :VEHICLE_CC,
                    :PAYMENT_TYPE_ID,
                    :CREATED_AT,
                    :UPDATED_AT
                )";

                        using (OracleCommand cmd = new OracleCommand(query, connection))
                        {
                            cmd.Parameters.Add(":REGISTRATION_NUMBER", OracleDbType.Varchar2).Value = row["REGISTRATION_NUMBER"] == DBNull.Value ? DBNull.Value : row["REGISTRATION_NUMBER"].ToString();
                            cmd.Parameters.Add(":WALLET_NUMBER", OracleDbType.Varchar2).Value = row["WALLET_NUMBER"] == DBNull.Value ? DBNull.Value : row["WALLET_NUMBER"].ToString();
                            cmd.Parameters.Add(":MOBILE_NUMBER", OracleDbType.Varchar2).Value = row["MOBILE_NUMBER"] == DBNull.Value ? DBNull.Value : row["MOBILE_NUMBER"].ToString();
                            cmd.Parameters.Add(":BANK_ID", OracleDbType.Int32).Value = row["BANK_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["BANK_ID"]);
                            cmd.Parameters.Add(":COMPANY_NAME", OracleDbType.Varchar2).Value = row["COMPANY_NAME"] == DBNull.Value ? DBNull.Value : row["COMPANY_NAME"].ToString();
                            cmd.Parameters.Add(":BRTA_SERIES", OracleDbType.Varchar2).Value = row["BRTA_SERIES"] == DBNull.Value ? DBNull.Value : row["BRTA_SERIES"].ToString();
                            cmd.Parameters.Add(":BRTA_SERIES_BN_NAME", OracleDbType.Varchar2).Value = row["BRTA_SERIES_BN_NAME"] == DBNull.Value ? DBNull.Value : row["BRTA_SERIES_BN_NAME"].ToString();
                            cmd.Parameters.Add(":BRTA_CLASS", OracleDbType.Varchar2).Value = row["BRTA_CLASS"] == DBNull.Value ? DBNull.Value : row["BRTA_CLASS"].ToString();
                            cmd.Parameters.Add(":BRTA_TYPE", OracleDbType.Varchar2).Value = row["BRTA_TYPE"] == DBNull.Value ? DBNull.Value : row["BRTA_TYPE"].ToString();
                            cmd.Parameters.Add(":RHD_SERIES", OracleDbType.Varchar2).Value = row["RHD_SERIES"] == DBNull.Value ? DBNull.Value : row["RHD_SERIES"].ToString();
                            cmd.Parameters.Add(":RHD_SERIES_BN_NAME", OracleDbType.Varchar2).Value = row["RHD_SERIES_BN_NAME"] == DBNull.Value ? DBNull.Value : row["RHD_SERIES_BN_NAME"].ToString();
                            cmd.Parameters.Add(":RHD_CLASS", OracleDbType.Varchar2).Value = row["RHD_CLASS"] == DBNull.Value ? DBNull.Value : row["RHD_CLASS"].ToString();
                            cmd.Parameters.Add(":RFID_NO", OracleDbType.Varchar2).Value = row["RFID_NO"] == DBNull.Value ? DBNull.Value : row["RFID_NO"].ToString();
                            cmd.Parameters.Add(":UNLADEN_WEIGHT", OracleDbType.Decimal).Value = row["UNLADEN_WEIGHT"] == DBNull.Value ? DBNull.Value : Convert.ToDecimal(row["UNLADEN_WEIGHT"]);
                            cmd.Parameters.Add(":LADEN_WEIGHT", OracleDbType.Decimal).Value = row["LADEN_WEIGHT"] == DBNull.Value ? DBNull.Value : Convert.ToDecimal(row["LADEN_WEIGHT"]);
                            cmd.Parameters.Add(":TOTAL_AXLE", OracleDbType.Int32).Value = row["TOTAL_AXLE"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["TOTAL_AXLE"]);
                            cmd.Parameters.Add(":OWNER_NAME", OracleDbType.Varchar2).Value = row["OWNER_NAME"] == DBNull.Value ? DBNull.Value : row["OWNER_NAME"].ToString();
                            cmd.Parameters.Add(":VEHICLE_CC", OracleDbType.Int32).Value = row["VEHICLE_CC"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["VEHICLE_CC"]);
                            cmd.Parameters.Add(":PAYMENT_TYPE_ID", OracleDbType.Int32).Value = row["PAYMENT_TYPE_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["PAYMENT_TYPE_ID"]);
                            cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value = row["CREATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CREATED_AT"]);
                            cmd.Parameters.Add(":UPDATED_AT", OracleDbType.TimeStamp).Value = row["UPDATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["UPDATED_AT"]);

                            cmd.ExecuteNonQuery();
                        }

                    }
                    if (dataTable.Rows.Count > 0)
                    {
                        LogWritter("Data Inserted For All Transaction Table is: " + dataTable.Rows.Count);
                        UpdateTempTable();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting data: " + ex.Message);
                // Handle exceptions as per your application's requirements
            }
        }

        private void UpdateTempTable()
        {
            var connection = new OracleConnection(_Destination);
            try
            {
                LogWritter(string.Format("UPDATING TABLE AFTER INSERTION TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA"));

                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = "UPDATE TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA SET IS_INSERTED=1";
                comm.CommandType = CommandType.Text;
                comm.ExecuteNonQuery();

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
        public void BulkInsert(DataTable dataTable, string tablname)
		{
            if (dataTable.Rows.Count > 0)
            {

                var connection = new OracleConnection(_Destination);
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
                    throw;

                }
                finally
                {
                    connection.Close();
                }
            }
            else
            {
                LogWritter(string.Format("Data Not found for {0} Table", tablname));
            }
		}

		public void LogWritter(string message)
		{

			string filename = "Shitalokhya_Vehivle_Registration_" + DateTime.Now.ToString("yyyy_MM_dd") + "_.log";
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


        private string GetLastSyncTime()
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                //string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT MAX(VEHICLE_REG_ID) AS VEHICLE_REG_ID FROM TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
                {
                    try
                    {
                        connection.Open();
                        OracleDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            // DateTime actualtime  = reader.GetDateTime(0); // Assuming CREATED_AT is a DateTime column

                            string nextTransId = reader.GetInt32(0).ToString(); // Assuming CREATED_AT is a DateTime column
                            if (nextTransId == null || String.IsNullOrEmpty(nextTransId))
                            {
                                nextTransId = "0";
                            }

                            // DateTime createdAt = actualtime.AddMinutes(-5);
                            //DateTime createdAt = actualtime.AddMinutes(-5);

                            LogWritter("Last inserted TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA lAST TRANS ID+1 IS : " + nextTransId.ToString());
                            //string formattedDateTime = createdAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                            //return formattedDateTime;
                            return nextTransId;
                        }
                        else
                        {
                            LogWritter("No data found in the TEMP_VEHICLES_REGISTRATION_TB_SHITALOKHYA Table.");
                            return null;
                        }

                        reader.Close();
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
        }
        private bool IsValidDbConn(string Source)
        {
            OracleConnection conn = new OracleConnection(Source);
            try
            {
                conn.Open();
                conn.Close();
                LogWritter("Connection is Valid for Shitalokhya");
                return true;
            }
            catch (Exception)
            {
                conn.Close();
                throw;
            }

        }
    }
}

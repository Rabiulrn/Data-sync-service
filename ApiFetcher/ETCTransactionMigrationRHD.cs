
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
    public class ETCTransactionMigrationRHD
    {
        static string _Source = "";
        static string _Destination = "";

        public async Task Sync(string Source, string Destination)
        {
            try
            {
                LogWritter("ETC Data Pulling from RHD");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;
                    if (Convert.ToInt32(GetLastSyncTime()) > 0)
                    {
                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter("Total ETC Data Count MEGHNA: " + data.Rows.Count);
                        }

                        BulkInsert(data, "TEMP_ETC_VEHICLES_REGISTRATION_TB_MEGHNA");
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("ETC New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }

                        GetETCDATAForDelete(insertData, "ETC_VEHICLES_REGISTRATION_TB_MEGHNA");
                        InsertDataTableToDatabase(insertData);

                        LogWritter("Data Synced ETC");
                    }
                    else
                    {
                        LogWritter("No Data Found in ETC_VEHICLES_REGISTRATION_TB_MEGHNA");
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
                                          T.VEHICLE_REG_ID,
                                          T.REGISTRATION_NUMBER,
                                          T.WALLET_NUMBER,
                                          T.MOBILE_NUMBER,
                                          T.BANK_ID,
                                          T.COMPANY_NAME,
                                          T.BRTA_SERIES,
                                          T.BRTA_SERIES_BN_NAME,
                                          T.BRTA_CLASS,
                                          T.BRTA_TYPE,
                                          T.RHD_SERIES,
                                          T.RHD_SERIES_BN_NAME,
                                          T.RHD_CLASS,
                                          T.RFID_NO,
                                          T.UNLADEN_WEIGHT,
                                          T.LADEN_WEIGHT,
                                          T.TOTAL_AXLE,
                                          T.OWNER_NAME,
                                          T.VEHICLE_CC,
                                          T.PAYMENT_TYPE_ID,
                                          T.CREATED_AT,
                                          T.UPDATED_AT,
                                          T.STATUS
                                      FROM 
                                          ETC_VEHICLES_REGISTRATION_TB T";

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
                var dataTable = new DataTable();
                string query = $@"SELECT
                                          T.VEHICLE_REG_ID,
                                          T.REGISTRATION_NUMBER,
                                          T.WALLET_NUMBER,
                                          T.MOBILE_NUMBER,
                                          T.BANK_ID,
                                          T.COMPANY_NAME,
                                          T.BRTA_SERIES,
                                          T.BRTA_SERIES_BN_NAME,
                                          T.BRTA_CLASS,
                                          T.BRTA_TYPE,
                                          T.RHD_SERIES,
                                          T.RHD_SERIES_BN_NAME,
                                          T.RHD_CLASS,
                                          T.RFID_NO,
                                          T.UNLADEN_WEIGHT,
                                          T.LADEN_WEIGHT,
                                          T.TOTAL_AXLE,
                                          T.OWNER_NAME,
                                          T.VEHICLE_CC,
                                          T.PAYMENT_TYPE_ID,
                                          T.CREATED_AT,
                                          T.UPDATED_AT,
                                          T.STATUS
                                      FROM
                                          TEMP_ETC_VEHICLES_REGISTRATION_TB_MEGHNA T
                                      left join ETC_VEHICLES_REGISTRATION_TB TB on TB.REGISTRATION_NUMBER=T.REGISTRATION_NUMBER
                                      WHERE  TB.REGISTRATION_NUMBER IS NULL and  T.IS_INSERTED is null or T.IS_INSERTED=0";
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
                        string query = @"INSERT INTO ETC_VEHICLES_REGISTRATION_TB (
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
                                                      UPDATED_AT,
                                                      STATUS
                                                  ) VALUES (
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
                                                      :UPDATED_AT,
                                                      :STATUS
                                                  )
                                                  ";

                        using (OracleCommand cmd = new OracleCommand(query, connection))
                        {
                            //cmd.Parameters.Add(":VEHICLE_REG_ID", OracleDbType.Varchar2).Value = row["VEHICLE_REG_ID"] == DBNull.Value ? DBNull.Value : row["VEHICLE_REG_ID"].ToString();
                            cmd.Parameters.Add(":REGISTRATION_NUMBER", OracleDbType.Varchar2).Value = row["REGISTRATION_NUMBER"] == DBNull.Value ? DBNull.Value : row["REGISTRATION_NUMBER"].ToString();
                            cmd.Parameters.Add(":WALLET_NUMBER", OracleDbType.Varchar2).Value = row["WALLET_NUMBER"] == DBNull.Value ? DBNull.Value : row["WALLET_NUMBER"].ToString();
                            cmd.Parameters.Add(":MOBILE_NUMBER", OracleDbType.Varchar2).Value = row["MOBILE_NUMBER"] == DBNull.Value ? DBNull.Value : row["MOBILE_NUMBER"].ToString();
                            cmd.Parameters.Add(":BANK_ID", OracleDbType.Varchar2).Value = row["BANK_ID"] == DBNull.Value ? DBNull.Value : row["BANK_ID"].ToString();
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
                            cmd.Parameters.Add(":VEHICLE_CC", OracleDbType.Varchar2).Value = row["VEHICLE_CC"] == DBNull.Value ? DBNull.Value : row["VEHICLE_CC"].ToString();
                            cmd.Parameters.Add(":PAYMENT_TYPE_ID", OracleDbType.Varchar2).Value = row["PAYMENT_TYPE_ID"] == DBNull.Value ? DBNull.Value : row["PAYMENT_TYPE_ID"].ToString();
                            cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value = row["CREATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CREATED_AT"]);
                            cmd.Parameters.Add(":UPDATED_AT", OracleDbType.TimeStamp).Value = row["UPDATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["UPDATED_AT"]);
                            cmd.Parameters.Add(":STATUS", OracleDbType.Varchar2).Value = row["STATUS"] == DBNull.Value ? DBNull.Value : row["STATUS"].ToString();

                            cmd.ExecuteNonQuery();
                        }

                    }
                    if (dataTable.Rows.Count > 0)
                    {
                        LogWritter("Total Inserted Data to ETC Transaction is: " + dataTable.Rows.Count.ToString());
                        UpdateTempTable();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting data: " + ex.Message);
                // Handle exceptions as per your application's requirements
                throw;
            }

        }

        private void UpdateTempTable()
        {
            var connection = new OracleConnection(_Destination);
            try
            {
                LogWritter(string.Format("UPDATING TABLE AFTER INSERTION TEMP_ETC_VEHICLES_REGISTRATION_TB_MEGHNA"));

                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = "UPDATE TEMP_ETC_VEHICLES_REGISTRATION_TB_MEGHNA SET IS_INSERTED=1";
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
                    LogWritter(string.Format("Total Records : {0} for {1}", dataTable.Rows.Count, tablname));

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
                    LogWritter(string.Format("{1} Table: {0}", ex.Message, tablname));

                }
                finally
                {
                    connection.Close();
                }
            }
            else
            {
                LogWritter(string.Format("Data Not found for {0} ", tablname));

            }
        }

        public void LogWritter(string message)
        {

            string filename = "MEGHNA_ETC_" + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
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

        private bool IsValidDbConn(string Source)
        {
            OracleConnection conn = new OracleConnection(Source);
            try
            {
                conn.Open();
                conn.Close();
                LogWritter("Connection is Valid for MEGHNA");
                return true;
            }
            catch (Exception)
            {
                conn.Close();
                throw;
            }

        }
        private string GetLastSyncTime()
        {
            OracleConnection connection = new OracleConnection(_Destination);

            try
            {//string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_MEGHNA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT MAX(COALESCE(VEHICLE_REG_ID, 0)) AS MAX_VEHICLE_REG_ID FROM TEMP_ETC_VEHICLES_REGISTRATION_TB";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
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

                        //DateTime createdAt = actualtime.AddMinutes(-5);
                        //DateTime createdAt = actualtime.AddMinutes(-5);

                        LogWritter("Last inserted TEMP_ETC_VEHICLES_REGISTRATION_TB_MEGHNA lAST TRANS ID IS : " + nextTransId.ToString());
                        //string formattedDateTime = createdAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                        //return formattedDateTime;
                        return nextTransId;
                    }
                    else
                    {
                        LogWritter("No data found in the TEMP_ETC_VEHICLES_REGISTRATION_TB_MEGHNA Table.");
                        return null;
                    }

                    reader.Close();
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

        private void GetETCDATAForDelete(DataTable dataTableS, string tablname)
        {
            if (dataTableS.Rows.Count > 0)
            {
                foreach (DataRow row in dataTableS.Rows)
                {
                    string vals = row["REGISTRATION_NUMBER"].ToString();
                    if (CheckIfEtcExists(vals))
                    {
                        var connection = new OracleConnection(_Destination);

                        try
                        {
                            LogWritter(string.Format("Total Records Deleted: {0} for {1}", dataTableS.Rows.Count, tablname));

                            connection.Open();
                            var comm = new OracleCommand();
                            comm.Parameters.Add(new OracleParameter("REGISTRATION_NUMBER", vals));
                            comm.Connection = connection;
                            comm.CommandText = "Delete from ETC_VEHICLES_REGISTRATION_TB where REGISTRATION_NUMBER= :REGISTRATION_NUMBER";
                            comm.CommandType = CommandType.Text;
                            comm.ExecuteNonQuery();

                        }
                        catch (Exception ex)
                        {
                            throw;
                            LogWritter(string.Format("{1} Table: {0}", ex.Message, tablname));

                        }
                        finally
                        {
                            connection.Close();
                        }
                    }

                }
            }
            else
            {
                LogWritter(string.Format("Data Not found for {0} ", tablname));

            }
        }
        private bool CheckIfEtcExists(string REGISTRATION_NUMBER)
        {
            bool isExists = false;

            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                //string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_MEGHNA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT COUNT(VEHICLE_REG_ID) AS MAX_VEHICLE_REG_ID FROM ETC_VEHICLES_REGISTRATION_TB where REGISTRATION_NUMBER= :REGISTRATION_NUMBER";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new OracleParameter("REGISTRATION_NUMBER", REGISTRATION_NUMBER));
                    try
                    {
                        connection.Open();
                        OracleDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            // DateTime actualtime  = reader.GetDateTime(0); // Assuming CREATED_AT is a DateTime column

                            string nextTransId = reader.GetInt32(0).ToString(); // Assuming CREATED_AT is a DateTime column
                            if (nextTransId == "0" || nextTransId == null)
                            {
                                return isExists;
                            }
                            else
                            {
                                return isExists = true;
                            }
                        }
                        else
                        {
                            return isExists;
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

        }
    }
}

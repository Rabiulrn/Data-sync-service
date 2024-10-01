

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
    public class AllTransactionMeghnaMigrationsStart
    {

        static string _Source = "";
        static string _Destination = "";

        public async Task Sync(string Source, string Destination)
        {
            try
            {
                LogWritter("All Transactions Data Pulling from Meghna");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;
                    if (!string.IsNullOrEmpty(GetLastSyncTime()))
                    {
                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter("Total All Transactions Data Count Meghna: " + data.Rows.Count);
                        }

                        BulkInsert(data, "TEMP_ALL_TRANSACTIONS_TB_MEGHNA");
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("All Transactions New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }
                        InsertDataTableToDatabase(insertData);
                        LogWritter("Data Synced All Transactions");
                    }
                    else
                    {
                        LogWritter("No Data Found in TEMP_ALL_TRANSACTIONS_TB_mEGHNA");
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
            OracleConnection connection = new OracleConnection(_Source);
            try
            {
                var lastSyncTime = GetLastSyncTime();

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
								ALL_TRANSACTIONS_TB";

                if (!string.IsNullOrEmpty(lastSyncTime))
                {
                    query += $" WHERE CREATED_AT >= TO_TIMESTAMP(:lastSyncTime, 'DD-MON-YY HH.MI.SS.FF9 AM')";
                    LogWritter("Get Local and inside if  + " + lastSyncTime);
                }

                connection.Open();

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                    if (!string.IsNullOrEmpty(lastSyncTime))
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
                

                return dataTable;
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }
            finally { connection.Close(); }
        }
        private DataTable ProcessTempDataBehHicleRegGet()
        {
            OracleConnection connection = new OracleConnection(_Destination);
            try
            {
                var dataTable = new DataTable();
                string query = $@"SELECT
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.REGISTRATION_NUMBER,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.AMOUNT,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.TRANSACTION_NUMBER,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.VEHICLE_CLASS,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.VEHICLE_ID,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.LANE_NO,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.IMAGE_NAME,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.BANK_ID,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.PAYMENT_TYPE_ID,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.USER_ID,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.RECOGNIZE_BY,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.PLAZA_ID,
	 							TEMP_ALL_TRANSACTIONS_TB_MEGHNA.RFID_NO,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.CLASS_STATUS,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.TRANSACTIN_ID_BY_BANK,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.IS_ACTIVE,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.STATUS_REMARKS,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.CREATED_AT,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.UPDATED_AT,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.CANCELED_AT,
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA.CANCELED_BY
							FROM
								TEMP_ALL_TRANSACTIONS_TB_MEGHNA
                  WHERE TEMP_ALL_TRANSACTIONS_TB_MEGHNA.IS_INSERTED IS NULL OR IS_INSERTED = 0";
               
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
                    
                

                return dataTable;
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        public void InsertDataTableToDatabase(DataTable dataTable)
        {
            OracleConnection connection = new OracleConnection(_Destination);
            try
            {
                connection.Open();

                foreach (DataRow row in dataTable.Rows)
                {
                    string query = @"INSERT INTO ALL_TRANSACTIONS_TB (
                               
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
                            ) VALUES (
                               
                                :REGISTRATION_NUMBER,
                                :AMOUNT,
                                :TRANSACTION_NUMBER,
                                :VEHICLE_CLASS,
                                :VEHICLE_ID,
                                :LANE_NO,
                                :IMAGE_NAME,
                                :BANK_ID,
                                :PAYMENT_TYPE_ID,
                                :USER_ID,
                                :RECOGNIZE_BY,
                                :PLAZA_ID,
                                :RFID_NO,
                                :CLASS_STATUS,
                                :TRANSACTIN_ID_BY_BANK,
                                :IS_ACTIVE,
                                :STATUS_REMARKS,
                                :CREATED_AT,
                                :UPDATED_AT,
                                :CANCELED_AT,
                                :CANCELED_BY
                            )";

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        //cmd.Parameters.Add(":TRANSACTION_ID", OracleDbType.Int32).Value = row["TRANSACTION_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["TRANSACTION_ID"]);
                        cmd.Parameters.Add(":REGISTRATION_NUMBER", OracleDbType.Varchar2).Value = row["REGISTRATION_NUMBER"] == DBNull.Value ? DBNull.Value : row["REGISTRATION_NUMBER"].ToString();
                        cmd.Parameters.Add(":AMOUNT", OracleDbType.Decimal).Value = row["AMOUNT"] == DBNull.Value ? DBNull.Value : Convert.ToDecimal(row["AMOUNT"]);
                        cmd.Parameters.Add(":TRANSACTION_NUMBER", OracleDbType.Varchar2).Value = row["TRANSACTION_NUMBER"] == DBNull.Value ? DBNull.Value : row["TRANSACTION_NUMBER"].ToString();
                        cmd.Parameters.Add(":VEHICLE_CLASS", OracleDbType.Varchar2).Value = row["VEHICLE_CLASS"] == DBNull.Value ? DBNull.Value : row["VEHICLE_CLASS"].ToString();
                        cmd.Parameters.Add(":VEHICLE_ID", OracleDbType.Varchar2).Value = row["VEHICLE_ID"] == DBNull.Value ? DBNull.Value : row["VEHICLE_ID"].ToString();
                        cmd.Parameters.Add(":LANE_NO", OracleDbType.Int32).Value = row["LANE_NO"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["LANE_NO"]);
                        cmd.Parameters.Add(":IMAGE_NAME", OracleDbType.Varchar2).Value = row["IMAGE_NAME"] == DBNull.Value ? DBNull.Value : row["IMAGE_NAME"].ToString();
                        cmd.Parameters.Add(":BANK_ID", OracleDbType.Int32).Value = row["BANK_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["BANK_ID"]);
                        cmd.Parameters.Add(":PAYMENT_TYPE_ID", OracleDbType.Int32).Value = row["PAYMENT_TYPE_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["PAYMENT_TYPE_ID"]);
                        cmd.Parameters.Add(":USER_ID", OracleDbType.Int32).Value = row["USER_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["USER_ID"]);
                        cmd.Parameters.Add(":RECOGNIZE_BY", OracleDbType.Varchar2).Value = row["RECOGNIZE_BY"] == DBNull.Value ? DBNull.Value : row["RECOGNIZE_BY"].ToString();
                        cmd.Parameters.Add(":PLAZA_ID", OracleDbType.Int32).Value = row["PLAZA_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["PLAZA_ID"]);
                        cmd.Parameters.Add(":RFID_NO", OracleDbType.Varchar2).Value = row["RFID_NO"] == DBNull.Value ? DBNull.Value : row["RFID_NO"].ToString();
                        cmd.Parameters.Add(":CLASS_STATUS", OracleDbType.Varchar2).Value = row["CLASS_STATUS"] == DBNull.Value ? DBNull.Value : row["CLASS_STATUS"].ToString();
                        cmd.Parameters.Add(":TRANSACTIN_ID_BY_BANK", OracleDbType.Varchar2).Value = row["TRANSACTIN_ID_BY_BANK"] == DBNull.Value ? DBNull.Value : row["TRANSACTIN_ID_BY_BANK"].ToString();
                        cmd.Parameters.Add(":IS_ACTIVE", OracleDbType.Int32).Value = row["IS_ACTIVE"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["IS_ACTIVE"]);
                        cmd.Parameters.Add(":STATUS_REMARKS", OracleDbType.Varchar2).Value = row["STATUS_REMARKS"] == DBNull.Value ? DBNull.Value : row["STATUS_REMARKS"].ToString();
                        cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value = row["CREATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CREATED_AT"]);
                        cmd.Parameters.Add(":UPDATED_AT", OracleDbType.TimeStamp).Value = row["UPDATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["UPDATED_AT"]);
                        cmd.Parameters.Add(":CANCELED_AT", OracleDbType.TimeStamp).Value = row["CANCELED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CANCELED_AT"]);
                        cmd.Parameters.Add(":CANCELED_BY", OracleDbType.Varchar2).Value = row["CANCELED_BY"] == DBNull.Value ? DBNull.Value : row["CANCELED_BY"].ToString();

                        cmd.ExecuteNonQuery();
                    }
                }
              

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting data: " + ex.Message);
                // Handle exceptions as per your application's requirements
            }
            finally
            {
                connection.Close();
                if (dataTable.Rows.Count > 0)
                {
                    LogWritter("Data Inserted For All Transaction Table is: " + dataTable.Rows.Count);
                    UpdateTempTable();
                }
            }
        }

        private void UpdateTempTable()
        {
            var connection = new OracleConnection(_Destination);
            try
            {
                LogWritter(string.Format("UPDATING TABLE AFTER INSERTION TEMP_ALL_TRANSACTIONS_TB_MEGHNA"));

                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = "UPDATE TEMP_ALL_TRANSACTIONS_TB_MEGHNA SET IS_INSERTED=1";
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
                    LogWritter(string.Format("Total Records : {0} for {1} All Transaction Meghna", dataTable.Rows.Count, tablname));

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
                LogWritter(string.Format("Data Not found for {0} All Transaction Meghna", tablname));

            }
        }

        public void LogWritter(string message)
        {

            string filename = "Meghna_All_Transactions_" + DateTime.Now.ToString("yyyy_MM_dd") + "_serviceEquipment.log";
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
                LogWritter("Connection is Valid for Meghna");
                return true;
            }
            catch (Exception)
            {
                conn.Close();
                throw;
            }
            finally
            {
                conn.Close();
            }

        }
        private string GetLastSyncTime()
        {
            OracleConnection connection = new OracleConnection(_Destination);
            try
            {
                //string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT MAX(CREATED_AT) AS CREATED_AT FROM ALL_TRANSACTIONS_TB WHERE PLAZA_ID = 3";
                OracleCommand command = new OracleCommand(sqlQuery, connection);


                connection.Open();
                OracleDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    // DateTime actualtime  = reader.GetDateTime(0); // Assuming CREATED_AT is a DateTime column

                    string nextTransId = reader.GetString(0).ToString();// Assuming CREATED_AT is a DateTime column
                    if (nextTransId == null || String.IsNullOrEmpty(nextTransId))
                    {
                        nextTransId = "0";
                    }

                    //DateTime createdAt = actualtime.AddMinutes(-5);
                    //DateTime createdAt = actualtime.AddMinutes(-5);

                    LogWritter("Last inserted ALL_TRANSACTIONS_TB lAST Created_at IS : " + nextTransId.ToString());
                    //string formattedDateTime = createdAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    //return formattedDateTime;
                    return nextTransId;
                }
                else
                {
                    LogWritter("No data found in the ALL_TRANSACTIONS_TB Table.");
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

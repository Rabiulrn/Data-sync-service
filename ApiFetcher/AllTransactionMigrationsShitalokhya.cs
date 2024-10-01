

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
    public class AllTransactionMigrationsShitalokhya
    {

        static string _Source = "";
        static string _Destination = "";

        public async Task Sync(string Source, string Destination)
        {
            try
            {
                LogWritter("All Transactions Data Pulling from Shitalokhya");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;
                   
                    if (Convert.ToInt32(GetLastSyncTime()) > 0)
                    {
                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter("Total All Transactions Data Count Shitalokhya: " + data.Rows.Count);
                        }

                        BulkInsert(data, "TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA");
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("All Transactions New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }
                        InsertDataTableToDatabase(insertData);
                        LogWritter("Data Synced All Transactions Shitalokhya");
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
            OracleConnection connection = new OracleConnection(_Source);
            try
            {
                var lastSyncTime = Convert.ToInt32(GetLastSyncTime());
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

                //if (!String.IsNullOrEmpty(lastSyncTime))
                //{
                //    query += $" WHERE CREATED_AT >= TO_TIMESTAMP(:lastSyncTime, 'YYYY-MM-DD HH24:MI:SS.FF')";

                //} 
                if (lastSyncTime > 0)
                {
                    query += $" WHERE TRANSACTION_ID > :lastSyncTime";
                    LogWritter("Get Local and inside if  + " + lastSyncTime);
                }
              
                    connection.Open();

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        //if (!String.IsNullOrEmpty(lastSyncTime))
                        //{
                        //    cmd.Parameters.Add(new OracleParameter("lastSyncTime", lastSyncTime));
                        //}
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
                   
                

                return dataTable;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }

        }
        private DataTable ProcessTempDataBehHicleRegGet()
        {
            try
            {
                var lastId = Convert.ToInt32(GetLastSyncTime());
                var dataTable = new DataTable();
                //         string query = $@"SELECT
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.REGISTRATION_NUMBER,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.AMOUNT,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.TRANSACTION_NUMBER,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.VEHICLE_CLASS,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.VEHICLE_ID,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.LANE_NO,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.IMAGE_NAME,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.BANK_ID,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.PAYMENT_TYPE_ID,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.USER_ID,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.RECOGNIZE_BY,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.PLAZA_ID,
                //		TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.RFID_NO,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CLASS_STATUS,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.TRANSACTIN_ID_BY_BANK,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.IS_ACTIVE,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.STATUS_REMARKS,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CREATED_AT,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.UPDATED_AT,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CANCELED_AT,
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CANCELED_BY
                //FROM
                //	TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA
                //         left join ALL_TRANSACTIONS_TB on TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.PLAZA_ID = ALL_TRANSACTIONS_TB.PLAZA_ID and
                //                                          TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CREATED_AT = ALL_TRANSACTIONS_TB.CREATED_AT
                //         where  ALL_TRANSACTIONS_TB.PLAZA_ID is null and  ALL_TRANSACTIONS_TB.CREATED_AT is null";

                string query = $@"SELECT
                                TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.TRANSACTION_ID,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.REGISTRATION_NUMBER,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.AMOUNT,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.TRANSACTION_NUMBER,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.VEHICLE_CLASS,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.VEHICLE_ID,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.LANE_NO,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.IMAGE_NAME,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.BANK_ID,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.PAYMENT_TYPE_ID,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.USER_ID,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.RECOGNIZE_BY,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.PLAZA_ID,
	 							TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.RFID_NO,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CLASS_STATUS,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.TRANSACTIN_ID_BY_BANK,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.IS_ACTIVE,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.STATUS_REMARKS,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CREATED_AT,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.UPDATED_AT,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CANCELED_AT,
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA.CANCELED_BY
							FROM
								TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA WHERE IS_INSERTED IS NULL OR IS_INSERTED = 0";

                using (OracleConnection connection = new OracleConnection(_Destination))
                {
                    connection.Open();

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {

                        cmd.Parameters.Add(new OracleParameter("lastId", lastId));

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
                LogWritter("Inside ProcessTempDataBehHicleRegGet: " + dataTable.Rows.Count);
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
                LogWritter(string.Format("UPDATING TABLE AFTER INSERTION TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA"));

                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = "UPDATE TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA SET IS_INSERTED=1";
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
                    LogWritter(string.Format("Total Records : {0} for {1} All Transaction Shitalokhya", dataTable.Rows.Count, tablname));

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
                LogWritter(string.Format("Data Not found for {0} All Transaction Shitalokhya", tablname));
            }
        }

        public void LogWritter(string message)
        {

            string filename = "Shitalokhya_All_Transactions_" + DateTime.Now.ToString("yyyy_MM_dd") + "_serviceEquipment.log";
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
                LogWritter("Connection is Valid for Shitalokhya");
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
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                //string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT MAX(COALESCE(TRANSACTION_ID, 0)) AS MAX_TRANSACTION_ID FROM TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA WHERE PLAZA_ID = 14";

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

                            LogWritter("Last inserted TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA lAST TRANS ID+1 IS : " + nextTransId.ToString());
                            //string formattedDateTime = createdAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                            //return formattedDateTime;
                            return nextTransId;
                        }
                        else
                        {
                            LogWritter("No data found in the TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA Table.");
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
    }
}

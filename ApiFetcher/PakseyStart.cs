using ApiFetcher.Notification;
using Microsoft.VisualBasic;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiFetcher
{
    public class PakseyStart
    {

        static string _Source = "";
        static string _Destination = "";
        static string _All_plaza_conn = "";
        static string _tableName = "";
        static string _bridgeName = "";
        static int _plaza_Id = 0;

        public async Task Sync(string source, string destination, string MonthlyDestination, string tableName, int plazaId, string bridgeName)
        {

            var emailNotifier = new EmailNotifier();
           // var smsService = new SmsService();


            _Source = source;
            _Destination = destination;
            _All_plaza_conn = MonthlyDestination;
            _tableName = tableName;
            _plaza_Id = plazaId;
            _bridgeName = bridgeName;

            try
            {
                LogWritter($"[INFO] Starting data synchronization for bridge: {_bridgeName}");
                if (IsValidDbConn(_Destination))
                {
                    LogWritter($"[INFO] Destination database connection is valid for {_bridgeName}");
                }
               
                if (IsValidDbConn(_Source))
                {
                    LogWritter($"[INFO] Source database connection is valid for {_bridgeName}");

                    // Step 1: Retrieve the last synchronization time
                    string lastSyncTime = GetLastSyncTime();
                    if (!string.IsNullOrEmpty(lastSyncTime))
                    {
                        LogWritter($"[INFO] Last synchronization time for {_bridgeName}: {lastSyncTime}");

                        // Step 2: Pull data from the source
                        DataTable localData = GetDataLocal();
                        if (localData.Rows.Count > 0)
                        {
                            LogWritter($"[INFO] Successfully retrieved records from source database of {_bridgeName} : {localData.Rows.Count}.");
                            // Step 4: Perform manual insertion with transaction
                            using (var connection = new OracleConnection(_Destination))
                            {
                                await connection.OpenAsync();

                                    try
                                    {
                                        // Step 1: Perform the first insert
                                        InsertDataToAllTransactionTB(localData);
                                        LogWritter($"[SUCCESS] Successfully inserted into the destination table for {_bridgeName}.");
                                        ////if last sync time is January, insert into January table
                                        //if (lastSyncTime.Contains("JAN"))
                                        //{

                                        //    InsertDataToJanuary(localData);
                                        //    LogWritter($"[SUCCESS] Successfully inserted into January table for {_bridgeName}.");
                                        //}
                                        ////if last sync time is February, insert into February table
                                        //else if (lastSyncTime.Contains("FEB"))
                                        //{
                                        //    InsertDataToFebruary(localData);
                                        //    LogWritter($"[SUCCESS] Successfully inserted into February table for {_bridgeName}.");
                                        //}
                                    InsertDataToMonthlyTable(localData, lastSyncTime);

                                    // Step 2: Perform bulk insert
                                    BulkInsert(localData, _tableName); // Insert into the temporary table
                                        LogWritter($"[SUCCESS] Successfully inserted records into temporary table for {_bridgeName} : {localData.Rows.Count}.");

                                        // Step 3: Commit transaction only if both operations succeed
                                       // transaction.Commit();
                                        LogWritter($"[SUCCESS][WITH LAST SYNC TIME] Synchronized new records for {_bridgeName} : {localData.Rows.Count}");
                                    }
                                    catch (Exception ex)
                                    {
                                        // Step 4: Rollback transaction on failure
                                      // transaction.Rollback();
                                        LogWritter($"[ERROR] Transaction rolled back due to an error: {ex.Message}");
                                        throw; // Re-throw exception for further handling
                                    }
                                }
                            
                        }
                        else
                        {
                            LogWritter($"[WARNING] No data found locally for {_bridgeName}");
                        }
                    }
                    else
                    {
                        LogWritter($"[WARNING] No synchronization timestamp found for {_bridgeName}. Skipping synchronization.");
                    }
                }
                else
                {
                    LogWritter($"[ERROR] Invalid source database connection for {_Source}. Synchronization aborted for {_bridgeName}.");

                    emailNotifier.SendEmailAlert($@"SERVER PROBLEM IN {_bridgeName}","CONNECTION LOST. CHECK SERVER" );
                   
                   // var response = await smsService.SendSmsAsync($@"CONNECTION LOST. CHECK SERVER");
                   // LogWritter($"[SMS] Sms: {response}");
                }
            }
            catch (Exception ex)
            {
                emailNotifier.SendEmailAlert($@"DATA SERVICE PROBLEM IN {_bridgeName}", ex.Message);
                LogWritter($"[CRITICAL] Data synchronization failed for {_bridgeName} with error: {ex.Message}");
                string error= ex.Message;
               
               // var response = await smsService.SendSmsAsync( $@"DATA SERVICE PROBLEM IN {_bridgeName}. The problem is : {error}");
              //  LogWritter($"[SMS] Sms: {response}");
                //  Console.WriteLine(response);

                // throw;
            }
        }



        private DataTable GetDataLocal()
        {
            OracleConnection connection = new OracleConnection(_Source);
            var dataTable = new DataTable();

            try
            {
                string lastSyncTime = GetLastSyncTime();
                string query = @"SELECT
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
                    query += " WHERE CREATED_AT > TO_TIMESTAMP(:lastSyncTime, :format)";
                    LogWritter($"[INFO] Filtering data with lastSyncTime: {lastSyncTime}");
                }

                connection.Open();

                using (OracleCommand cmd = new OracleCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(lastSyncTime))
                    {
                        cmd.Parameters.Add(new OracleParameter("lastSyncTime", lastSyncTime));
                        cmd.Parameters.Add(new OracleParameter("format", OracleDbType.Varchar2)).Value = "DD-MON-YY HH.MI.SS.FF9 AM";
                    }

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        // Add columns to DataTable based on schema
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                        }

                        // Populate DataTable
                        while (reader.Read())
                        {
                            DataRow dataRow = dataTable.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                dataRow[i] = reader[i] ?? DBNull.Value;
                            }
                            dataTable.Rows.Add(dataRow);
                        }
                    }
                }

               // LogWritter($"[INFO] Successfully retrieved {dataTable.Rows.Count} records from source database.");
            }
            catch (Exception ex)
            {
                LogWritter($"[ERROR] Failed to retrieve data from local source: {ex.Message}");
                throw;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return dataTable;
        }

        private void InsertDataToMonthlyTable(DataTable localData, string lastSyncTime)
        {
           
            string monthAbbreviation = GetMonthAbbreviationFromLastSyncTime(lastSyncTime);
            string yearSuffix = GetYearSuffixFromLastSyncTime(lastSyncTime); // "25" or "26"

            if (string.IsNullOrEmpty(monthAbbreviation))
            {
                LogWritter($"[ERROR] Invalid month abbreviation in lastSyncTime: {lastSyncTime}");
                return; 
            }

            try
            {
                
                string tableName = $"{monthAbbreviation}{yearSuffix}";
                using (OracleConnection connection = new OracleConnection(_All_plaza_conn))
                {
                    try
                    {
                        connection.Open();

                        foreach (DataRow row in localData.Rows)
                        {
                            string query = $@"INSERT INTO {tableName} (
                                    REGISTRATION_NUMBER, AMOUNT, TRANSACTION_NUMBER, VEHICLE_CLASS, VEHICLE_ID, LANE_NO,
                                    IMAGE_NAME, BANK_ID, PAYMENT_TYPE_ID, USER_ID, RECOGNIZE_BY, PLAZA_ID, RFID_NO,
                                    CLASS_STATUS, TRANSACTIN_ID_BY_BANK, IS_ACTIVE, STATUS_REMARKS, CREATED_AT,
                                    UPDATED_AT, CANCELED_AT, CANCELED_BY, PLAZA_TRANS_ID
                                ) VALUES (
                                    :REGISTRATION_NUMBER, :AMOUNT, :TRANSACTION_NUMBER, :VEHICLE_CLASS, :VEHICLE_ID, 
                                    :LANE_NO, :IMAGE_NAME, :BANK_ID, :PAYMENT_TYPE_ID, :USER_ID, :RECOGNIZE_BY, :PLAZA_ID, 
                                    :RFID_NO, :CLASS_STATUS, :TRANSACTIN_ID_BY_BANK, :IS_ACTIVE, :STATUS_REMARKS, 
                                    :CREATED_AT, :UPDATED_AT, :CANCELED_AT, :CANCELED_BY, :PLAZA_TRANS_ID
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
                                cmd.Parameters.Add(":PLAZA_TRANS_ID", OracleDbType.Int32).Value = row["TRANSACTION_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["TRANSACTION_ID"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWritter($"[ERROR] Error inserting data into {tableName} table: {ex.Message}");
                        throw; 
                    }
                    finally
                    {
                        connection.Close();

                    }
                }
         
                LogWritter($"[SUCCESS] Successfully inserted into {tableName} table for {monthAbbreviation}.");
            }
            catch (Exception ex)
            {
                LogWritter($"[ERROR] Error inserting data into {monthAbbreviation} table: {ex.Message}");
                throw; 
            }
           
        }


        private string GetYearSuffixFromLastSyncTime(string lastSyncTime)
        {
            if (lastSyncTime.Contains("2025") || lastSyncTime.Contains("25"))
                return "25";

            if (lastSyncTime.Contains("2026") || lastSyncTime.Contains("26"))
                return "26";

            return null;
        }


        private string GetMonthAbbreviationFromLastSyncTime(string lastSyncTime)
        {
            // Map of month abbreviations
            Dictionary<string, string> monthMap = new Dictionary<string, string>
    {
        { "JAN", "JANUARY" },
        { "FEB", "FEBRUARY" },
        { "MAR", "MARCH" },
        { "APR", "APRIL" },
        { "MAY", "MAY" },
        { "JUN", "JUNE" },
        { "JUL", "JULY" },
        { "AUG", "AUGUST" },
        { "SEP", "SEPTEMBER" },
        { "OCT", "OCTOBER" },
        { "NOV", "NOVEMBER" },
        { "DEC", "DECEMBER" }
    };

            foreach (var month in monthMap.Keys)
            {
                if (lastSyncTime.Contains(month, StringComparison.OrdinalIgnoreCase))
                {
                    return monthMap[month]; // Return the corresponding full month name
                }
            }

            return null; // Return null if no valid month is found
        }


        public void InsertDataToJanuary(DataTable dataTable)
        {
            using (OracleConnection connection = new OracleConnection(_All_plaza_conn))
            {
                try
                {
                    connection.Open();
                    //  LogWritter("[INFO] Database connection opened.");

             
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string query = @"INSERT INTO JANUARY25 (
                               
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
                                CANCELED_BY,
PLAZA_TRANS_ID
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
                                :CANCELED_BY,
:PLAZA_TRANS_ID
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
                            cmd.Parameters.Add(":PLAZA_TRANS_ID", OracleDbType.Int32).Value = row["TRANSACTION_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["TRANSACTION_ID"]);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Log the outcome
                    //if (rowsInserted > 0)
                    //{
                    //    LogWritter($"[SUCCESS] {rowsInserted} rows inserted into ALL_TRANSACTION_TB table.");
                    //    UpdateTempTable();
                    //}
                    //else
                    //{
                    //    LogWritter("[INFO] No rows inserted. Check the data table.");
                    //}
                }
                catch (Exception ex)
                {
                    LogWritter($"[ERROR] Error inserting data into ALL_TRANSACTION_TB table: {ex.Message}");
                    throw; // Rethrow the exception after logging it
                }
                finally
                {
                    connection.Close();
                    // LogWritter("[INFO] Database connection closed.");
                }
            }
        } 
        public void InsertDataToFebruary(DataTable dataTable)
        {
            using (OracleConnection connection = new OracleConnection(_All_plaza_conn))
            {
                try
                {
                    connection.Open();
                    //  LogWritter("[INFO] Database connection opened.");

                    int rowsInserted = 0;
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string query = @"INSERT INTO FEBRUARY25 (
                               
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
                                CANCELED_BY,
PLAZA_TRANS_ID
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
                                :CANCELED_BY,
:PLAZA_TRANS_ID
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
                            cmd.Parameters.Add(":PLAZA_TRANS_ID", OracleDbType.Int32).Value = row["TRANSACTION_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["TRANSACTION_ID"]);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Log the outcome
                    //if (rowsInserted > 0)
                    //{
                    //    LogWritter($"[SUCCESS] {rowsInserted} rows inserted into ALL_TRANSACTION_TB table.");
                    //    UpdateTempTable();
                    //}
                    //else
                    //{
                    //    LogWritter("[INFO] No rows inserted. Check the data table.");
                    //}
                }
                catch (Exception ex)
                {
                    LogWritter($"[ERROR] Error inserting data into ALL_TRANSACTION_TB table: {ex.Message}");
                    throw; // Rethrow the exception after logging it
                }
                finally
                {
                    connection.Close();
                    // LogWritter("[INFO] Database connection closed.");
                }
            }
        }

        public void InsertDataToAllTransactionTB(DataTable dataTable)
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                try
                {
                    connection.Open();
                  //  LogWritter("[INFO] Database connection opened.");

                    int rowsInserted = 0;
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
                                CANCELED_BY,
PLAZA_TRANS_ID
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
                                :CANCELED_BY,
:PLAZA_TRANS_ID
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
                            cmd.Parameters.Add(":PLAZA_TRANS_ID", OracleDbType.Int32).Value = row["TRANSACTION_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["TRANSACTION_ID"]);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Log the outcome
                    //if (rowsInserted > 0)
                    //{
                    //    LogWritter($"[SUCCESS] {rowsInserted} rows inserted into ALL_TRANSACTION_TB table.");
                    //    UpdateTempTable();
                    //}
                    //else
                    //{
                    //    LogWritter("[INFO] No rows inserted. Check the data table.");
                    //}
                }
                catch (Exception ex)
                {
                    LogWritter($"[ERROR] Error inserting data into ALL_TRANSACTION_TB table: {ex.Message}");
                    throw; // Rethrow the exception after logging it
                }
                finally
                {
                    connection.Close();
                   // LogWritter("[INFO] Database connection closed.");
                }
            }
        }

        private void UpdateTempTable()
        {
            var connection = new OracleConnection(_Destination);
            try
            {
                LogWritter(string.Format("[INFO] Updating temp table after insertion into {0}", _tableName));


                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = $@"UPDATE {_tableName} SET IS_INSERTED=1";
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
                    LogWritter(string.Format($@"Bulk insert complete"));

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
                LogWritter(string.Format($@"Data Not found  in Temp Table {_bridgeName}"));
            }
        }





        public void LogWritter(string message)
        {
            try
            {
                // Base directory for logs
                string appPath = @"D:\DataMigrationLog";

                // Construct year-based directory
                string yearDirectory = Path.Combine(appPath, "Log" + DateTime.Now.Year);

                // Construct date-based subdirectory
                string dateDirectory = Path.Combine(yearDirectory, DateTime.Now.ToString("yyyy_MM_dd"));

                // Ensure directories exist
                if (!Directory.Exists(dateDirectory))
                {
                    Directory.CreateDirectory(dateDirectory);
                }

                // Construct log file name
                string filename = $"{_bridgeName}_Transactions_{DateTime.Now:yyyy_MM_dd}.log";
                string filePath = Path.Combine(dateDirectory, filename);

                // Append log message with timestamp
                string logMessage = $"{DateTime.Now:dd HH:mm:ss.fff}\t{message}{Environment.NewLine}";
                File.AppendAllText(filePath, logMessage);
            }
            catch (Exception ex)
            {
                // Optional: Handle logging failure (e.g., write to a fallback location)
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

       


        private bool IsValidDbConn(string tableName)
        {
            OracleConnection conn = new OracleConnection(tableName); // Assuming _Destination is the connection string or source
            try
            {
                conn.Open();
                conn.Close();
                //  LogWritter($"[INFO] Connection is valid for table {tableName}.");
                return true;
            }
            catch (OracleException ex)
            {
                conn.Close();
                LogWritter($"[ERROR] OracleException occurred while validating connection for table {tableName}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                conn.Close();
                LogWritter($"[ERROR] General error occurred while validating connection for table {tableName}: {ex.Message}");
                throw;
            }
            finally
            {
                conn.Close();
            }
            }


        private string GetLastSyncTime()
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                string sqlQuery = $@"SELECT MAX(CREATED_AT) FROM ALL_TRANSACTIONS_TB";
                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
                {
                    try
                    {
                        connection.Open();
                        OracleDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            string nextTransId = reader.GetString(0).ToString(); // Assuming CREATED_AT is a DateTime column
                            if (string.IsNullOrEmpty(nextTransId))
                            {
                                nextTransId = "0";  // Handle null or empty case
                            }

                          //  LogWritter($"[INFO] Last synchronization time for Plaza {_plaza_Id}: {nextTransId}");
                            return nextTransId;
                        }
                        else
                        {
                            LogWritter($"[WARNING] No data found in the ALL_TRANSACTION_TB table for Plaza {_plaza_Id}. Returning null.");
                            return null;
                        }

                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        LogWritter($"[ERROR] An error occurred while retrieving last synchronization time for Plaza {_plaza_Id}: {ex.Message}");
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

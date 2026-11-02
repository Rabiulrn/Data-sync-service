using ApiFetcher.Notification;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
////////////////////////////////////////////////////////////////////////////////////
//                               only apply connection restore sms
//////////////////////////////////////////////////////////////////////////////////////
namespace ApiFetcher
{
    public class AllTransactionV4_wim: IDisposable
    {
        static string _Source = "";
        static string _Destination = "";
        static string _All_plaza_conn = "";
        static string _tableName = "";
        static string _bridgeName = "";
        static int _plaza_Id = 0;
        static int _smsFlag = 1;
        static int connectionFailureCount = 0;
        private static bool wasConnectionDown = false;

        // DateTime? firstFailureTime = null; // Nullable to track when the first failure occurred
        static DateTime? firstFailureTime = null; // Declare at the class level
        public async Task Sync(string source, string destination, string MonthlyDestination, string tableName, int plazaId, string bridgeName)
        {
             var emailNotifier = new EmailNotifier();
             var smsService = new SmsService();

            _Source = source;
            _Destination = destination;
            _All_plaza_conn = MonthlyDestination;
            _tableName = tableName;
            _plaza_Id = plazaId;
            _bridgeName = bridgeName;

            LogWritter($"[INFO] Starting data synchronization for bridge: {_bridgeName}");

            try
            {
              
                if (!IsValidDbConn(_Source)) 
                {
                    LogWritter($"[ERROR] Invalid source database connection . Synchronization aborted for {_bridgeName}.");
                    return;
                }
                if (!IsValidDbConn(_Destination)) 
                {
                    LogWritter($"[ERROR] Invalid Destination database connection . Synchronization aborted for {_bridgeName}.");
                    return;
                }

                int lastSyncId = Convert.ToInt32(GetLastSyncID());
                if (lastSyncId <= 0)
                {
                    LogWritter($"[WARNING] No valid last synchronization ID found for {_bridgeName}. Skipping synchronization.");
                    return;
                }



                using (DataTable localData = GetDataLocal())
                {
                    if (localData.Rows.Count > 0)
                    {
                        LogWritter($"[INFO] Successfully retrieved records from source database of {_bridgeName} : {localData.Rows.Count}.");

                        using (var connection = new OracleConnection(_Destination))
                        {
                            await connection.OpenAsync();

                                try
                                {
                                    InsertDataToAllTransactionTB(localData);
                                    BulkInsert(localData, _tableName);

                                    // Extract the month from the first row's CREATED_AT column
                                    var firstRowCreatedAt = localData.Rows[0]["CREATED_AT"];
                                if (firstRowCreatedAt != DBNull.Value &&
                                     DateTime.TryParse(firstRowCreatedAt.ToString(), out DateTime createdAt))
                                {
                                    int month = createdAt.Month;   // 1–12
                                    int year = createdAt.Year;    // 2025 / 2026

                                //    InsertDataToMonthlyTable(localData, month, year);
                                
                                //    // Conditional checks for the month
                                //    if (month == 1) // January
                                //    {
                                //        InsertDataToJanuary(localData);
                                //        LogWritter($"[SUCCESS] Successfully inserted into January table for {_bridgeName}.");
                                //    }
                                //    else if (month == 2) // February
                                //    {
                                //        InsertDataToFebruary(localData);
                                //        LogWritter($"[SUCCESS] Successfully inserted into February table for {_bridgeName}.");
                                //    }
                                //// Add additional conditions for other months if needed

                            }
                                    else
                                    {
                                        LogWritter("[ERROR] CREATED_AT column contains invalid or null data.");
                                        throw new Exception("Invalid CREATED_AT value in the data.");
                                    }

                                    // Commit transaction only if both operations succeed
                                   // transaction.Commit();
                                    LogWritter($"[SUCCESS][WITH LAST INSERTED ID] Synchronized new records for {_bridgeName} : {localData.Rows.Count}. Commit successful.");
                                if (wasConnectionDown)
                                {
                                    var message = $"CONNECTION RESTORED FOR {_bridgeName} SERVER AT {DateTime.Now}";
                                    LogWritter($"[INFO] Connection restored. Sending notification: {message}");

                                    try
                                    {
                                        await Task.WhenAll(
                                            smsService.SendBulkSmsAsync(message),
                                            smsService.SendSmsAsync(message)
                                        );
                                        LogWritter($"[SUCCESS] Connection up SMS sent successfully for {_bridgeName}.");
                                    }
                                    catch (Exception smsEx)
                                    {
                                        LogWritter($"[ERROR] Failed to send connection up SMS. Error: {smsEx.Message}");
                                    }

                                    wasConnectionDown = false; // Reset flag
                                }


                            }
                            catch (Exception ex)
                                {
                                  //  transaction.Rollback();
                                    LogWritter($"[ERROR] Transaction rolled back due to an error: {ex.Message}");
                                    throw;
                                }
                            }
                        
                    }
                    else
                    {
                        LogWritter($"[WARNING] No data found locally for {_bridgeName}");
                    }
                }



                _smsFlag = 1;
                // Force garbage collection after large operations
              //  GC.Collect();
              //  GC.WaitForPendingFinalizers();

            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, emailNotifier, smsService);
              
            }


        }

        private async Task HandleExceptionAsync(Exception ex, EmailNotifier emailNotifier, SmsService smsService)
        {
            string error = ex.Message;

            // Log exception message
            LogWritter($"[ERROR-CRITICAL] Data synchronization failed for {_bridgeName} with error: {error}");
          //  LogWritter($"[DEBUG] Exception details: {ex}");

            try
            {
                // Email alert (if enabled)
                LogWritter($"[ERROR-INFO] Attempting to send email alert for {_bridgeName}");
                // emailNotifier.SendEmailAlert($"DATA SERVICE PROBLEM IN {_bridgeName}", ex.ToString());
            }
            catch (Exception emailEx)
            {
                LogWritter($"[ERROR] Failed to send email alert for {_bridgeName}. Error: {emailEx.Message}");
            }

            if (_smsFlag == 1 && error.Contains("Connection request timed out"))
            {
                LogWritter("[ERROR-INFO] Connection timeout detected. Incrementing failure count and evaluating SMS notification criteria.");
                connectionFailureCount++;

                if (firstFailureTime == null)
                {
                    firstFailureTime = DateTime.Now;
                    LogWritter($"[ERROR-INFO] First failure time recorded: {firstFailureTime}");
                }

                var timeDifference = DateTime.Now - firstFailureTime.Value;
                LogWritter($"[ERROR-INFO] Time since first failure: {timeDifference.TotalMinutes} minutes. Failure count: {connectionFailureCount}");

                //if time difference is greater than 10 minutes and failure count is 2 don't sent sms
                if (timeDifference.TotalMinutes > 10 && connectionFailureCount >= 2)
                {

                    connectionFailureCount = 0;
                    firstFailureTime = null;
            
                    LogWritter("[ERROR-INFO] Reset connection failure counters and time greater than 10 min.");
                   // emailNotifier.SendEmailAlert($"DATA SERVICE PROBLEM IN {_bridgeName}", ex.ToString());
                }


                if (timeDifference.TotalMinutes > 2 && connectionFailureCount >= 3)
                {
                    var message = $"CONNECTION LOST. CHECK {_bridgeName} SERVER. DOWN TIME: {timeDifference}. FROM: {firstFailureTime}";
                    LogWritter($"[ERROR-ALERT] Preparing to send SMS notifications. Message: {message}");

                    try
                    {
                        await Task.WhenAll(
                            smsService.SendBulkSmsAsync(message),
                            smsService.SendSmsAsync(message)
                        );
                        LogWritter($"[ERROR-SUCCESS] SMS notifications sent successfully for {_bridgeName}.");
                    }
                    catch (Exception smsEx)
                    {
                        LogWritter($"[ERROR] Failed to send SMS notifications. Error: {smsEx.Message}");
                    }

                    connectionFailureCount = 0;
                    firstFailureTime = null;
                    _smsFlag = 0;
                    wasConnectionDown = true; // Mark that we had a downtime

                    LogWritter("[ERROR-INFO] Reset connection failure counters and SMS flag.");
                }
            }
            else
            {
                LogWritter("[ERROR-INFO] SMS notification criteria not met. No action taken.");
            }
        }

        private DataTable GetDataLocal()
        {
            OracleConnection connection = null;
            try
            {
                var dataTable = new DataTable();
                connection = new OracleConnection(_Source);
                connection.Open();
                var lastSyncID = Convert.ToInt32(GetLastSyncID());
                string query = $@"SELECT
        WIM_TRAN_ID,
TRANSACTION_NUMBER,
LANE_NUMBER,
CREATED_AT,
PLATE_ZONE,
PLATE_SERIES,
PLATE_NUMBER,
VEHICLE_ID,
NUMBER_OF_AXLE,
VEHICLE_SPEED,
AXLE_ONE,
AXLE_TWO,
AXLE_THREE,
AXLE_FOUR,
AXLE_FIVE,
AXLE_SIX,
AXLE_SEVEN,
GROSS_VEHICLE_WEIGHT,
IS_LOADED,
IS_OVERLOADED,
OVER_SIZED_MODIFIED,
WHEEL_BASE,
BILL_NUMBER,
RECEIPT_NUMBER,
PLAZA_ID,
LANE_NO,
BANK_ID,
PAYMENT_TYPE_ID,
CREATED_BY,
IS_DELETED,
DELETED_BY,
DELETED_DATE,
WIM_SEQUENCE_NUMBER,
IMAGE_NAME,
STATIC_WIM_GROSS,
PARENT_TRAN_ID
        FROM
        ALL_TRANSACTIONS_WIM";

                if (lastSyncID > 0)
                {
                    query += $" WHERE WIM_TRAN_ID > :lastSyncID";
                    LogWritter("[INFO]Get Local data from the Transaction ID :" + lastSyncID);
                }

                using (var cmd = new OracleCommand(query, connection))
                {
                    if (lastSyncID > 0)
                    {
                        cmd.Parameters.Add(new OracleParameter("lastSyncID", lastSyncID));
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
                return dataTable;
            }
            catch (Exception ex)
            {
                LogWritter($"[ERROR] Error retrieving local data: {ex.Message}");
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                }
            }
        }

        private void InsertDataToMonthlyTable(DataTable localData, int month,int year)
        {
           
            string monthName = GetMonthName(month);
            if (string.IsNullOrEmpty(monthName))
            {
                LogWritter("[ERROR] Invalid month number provided.");
                throw new Exception("Invalid month number.");
            }



            // year: 2025 / 2026 → suffix: 25 / 26
            string yearSuffix = year.ToString().Substring(2);
            string tableName = $"{monthName}{yearSuffix}";

            using (OracleConnection connection = new OracleConnection(_All_plaza_conn))
            {
                try
                {
                    connection.Open();

                    foreach (DataRow row in localData.Rows)
                    {
                        string query = $@"INSERT INTO {tableName} (
                               
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


                }
                catch (Exception ex)
                {
                    LogWritter($"[ERROR] Error inserting data into ALL_TRANSACTION_TB table: {ex.Message}");
                    throw;
                }
                finally
                {
                    connection.Close();
                }
                LogWritter($"[SUCCESS] Successfully inserted into {_tableName} table for {tableName}.");
            }
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "JANUARY",
                2 => "FEBRUARY",
                3 => "MARCH",
                4 => "APRIL",
                5 => "MAY",
                6 => "JUNE",
                7 => "JULY",
                8 => "AUGUST",
                9 => "SEPTEMBER",
                10 => "OCTOBER",
                11 => "NOVEMBER",
                12 => "DECEMBER",
                _ => string.Empty // Return empty string for invalid month numbers to avoid null reference
            };
        }


        public void InsertDataToAllTransactionTB(DataTable dataTable)
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                try
                {
                    connection.Open();
                    //  LogWritter("[INFO] Database connection opened.");

                    //int rowsInserted = 0;
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string query = @"  INSERT INTO ALL_TRANSACTIONS_WIM
                (
                    TRANSACTION_NUMBER,
                    LANE_NUMBER,
                    CREATED_AT,
                    PLATE_ZONE,
                    PLATE_SERIES,
                    PLATE_NUMBER,
                    VEHICLE_ID,
                    NUMBER_OF_AXLE,
                    VEHICLE_SPEED,
                    AXLE_ONE,
                    AXLE_TWO,
                    AXLE_THREE,
                    AXLE_FOUR,
                    AXLE_FIVE,
                    AXLE_SIX,
                    AXLE_SEVEN,
                    GROSS_VEHICLE_WEIGHT,
                    IS_LOADED,
                    IS_OVERLOADED,
                    OVER_SIZED_MODIFIED,
                    WHEEL_BASE,
                    BILL_NUMBER,
                    RECEIPT_NUMBER,
                    PLAZA_ID,
                    LANE_NO,
                    BANK_ID,
                    PAYMENT_TYPE_ID,
                    CREATED_BY,
                    IS_DELETED,
                    DELETED_BY,
                    DELETED_DATE,
                    WIM_SEQUENCE_NUMBER,
                    IMAGE_NAME,
                    STATIC_WIM_GROSS,
                    PARENT_TRAN_ID
                )
                VALUES
                (
                    :TRANSACTION_NUMBER,
                    :LANE_NUMBER,
                    :CREATED_AT,
                    :PLATE_ZONE,
                    :PLATE_SERIES,
                    :PLATE_NUMBER,
                    :VEHICLE_ID,
                    :NUMBER_OF_AXLE,
                    :VEHICLE_SPEED,
                    :AXLE_ONE,
                    :AXLE_TWO,
                    :AXLE_THREE,
                    :AXLE_FOUR,
                    :AXLE_FIVE,
                    :AXLE_SIX,
                    :AXLE_SEVEN,
                    :GROSS_VEHICLE_WEIGHT,
                    :IS_LOADED,
                    :IS_OVERLOADED,
                    :OVER_SIZED_MODIFIED,
                    :WHEEL_BASE,
                    :BILL_NUMBER,
                    :RECEIPT_NUMBER,
                    :PLAZA_ID,
                    :LANE_NO,
                    :BANK_ID,
                    :PAYMENT_TYPE_ID,
                    :CREATED_BY,
                    :IS_DELETED,
                    :DELETED_BY,
                    :DELETED_DATE,
                    :WIM_SEQUENCE_NUMBER,
                    :IMAGE_NAME,
                    :STATIC_WIM_GROSS,
                    :PARENT_TRAN_ID
          
                            )";
                        using (OracleCommand cmd = new OracleCommand(query, connection))
                        {
                            cmd.BindByName = true;

                            cmd.Parameters.Add(":TRANSACTION_NUMBER", OracleDbType.Varchar2)
                                .Value = row["TRANSACTION_NUMBER"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["TRANSACTION_NUMBER"].ToString();

                            cmd.Parameters.Add(":LANE_NUMBER", OracleDbType.Varchar2)
                                .Value = row["LANE_NUMBER"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["LANE_NUMBER"].ToString();

                            cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp)
                                .Value = row["CREATED_AT"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["CREATED_AT"]);

                            cmd.Parameters.Add(":PLATE_ZONE", OracleDbType.Varchar2)
                                .Value = row["PLATE_ZONE"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["PLATE_ZONE"].ToString();

                            cmd.Parameters.Add(":PLATE_SERIES", OracleDbType.Varchar2)
                                .Value = row["PLATE_SERIES"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["PLATE_SERIES"].ToString();

                            cmd.Parameters.Add(":PLATE_NUMBER", OracleDbType.Varchar2)
                                .Value = row["PLATE_NUMBER"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["PLATE_NUMBER"].ToString();

                            cmd.Parameters.Add(":VEHICLE_ID", OracleDbType.Varchar2)
                                .Value = row["VEHICLE_ID"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["VEHICLE_ID"].ToString();

                            cmd.Parameters.Add(":NUMBER_OF_AXLE", OracleDbType.NVarchar2)
                                .Value = row["NUMBER_OF_AXLE"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["NUMBER_OF_AXLE"].ToString();

                            cmd.Parameters.Add(":VEHICLE_SPEED", OracleDbType.NVarchar2)
                                .Value = row["VEHICLE_SPEED"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["VEHICLE_SPEED"].ToString();

                            cmd.Parameters.Add(":AXLE_ONE", OracleDbType.NVarchar2)
                                .Value = row["AXLE_ONE"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_ONE"].ToString();

                            cmd.Parameters.Add(":AXLE_TWO", OracleDbType.NVarchar2)
                                .Value = row["AXLE_TWO"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_TWO"].ToString();

                            cmd.Parameters.Add(":AXLE_THREE", OracleDbType.NVarchar2)
                                .Value = row["AXLE_THREE"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_THREE"].ToString();

                            cmd.Parameters.Add(":AXLE_FOUR", OracleDbType.NVarchar2)
                                .Value = row["AXLE_FOUR"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_FOUR"].ToString();

                            cmd.Parameters.Add(":AXLE_FIVE", OracleDbType.NVarchar2)
                                .Value = row["AXLE_FIVE"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_FIVE"].ToString();

                            cmd.Parameters.Add(":AXLE_SIX", OracleDbType.Varchar2)
                                .Value = row["AXLE_SIX"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_SIX"].ToString();

                            cmd.Parameters.Add(":AXLE_SEVEN", OracleDbType.NVarchar2)
                                .Value = row["AXLE_SEVEN"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["AXLE_SEVEN"].ToString();

                            cmd.Parameters.Add(":GROSS_VEHICLE_WEIGHT", OracleDbType.Varchar2)
                                .Value = row["GROSS_VEHICLE_WEIGHT"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["GROSS_VEHICLE_WEIGHT"].ToString();

                            cmd.Parameters.Add(":IS_LOADED", OracleDbType.Char)
                                .Value = row["IS_LOADED"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["IS_LOADED"].ToString();

                            cmd.Parameters.Add(":IS_OVERLOADED", OracleDbType.Char)
                                .Value = row["IS_OVERLOADED"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["IS_OVERLOADED"].ToString();

                            cmd.Parameters.Add(":OVER_SIZED_MODIFIED", OracleDbType.Char)
                                .Value = row["OVER_SIZED_MODIFIED"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["OVER_SIZED_MODIFIED"].ToString();

                            cmd.Parameters.Add(":WHEEL_BASE", OracleDbType.Decimal)
                                .Value = row["WHEEL_BASE"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDecimal(row["WHEEL_BASE"]);

                            cmd.Parameters.Add(":BILL_NUMBER", OracleDbType.Char)
                                .Value = row["BILL_NUMBER"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["BILL_NUMBER"].ToString();

                            cmd.Parameters.Add(":RECEIPT_NUMBER", OracleDbType.Char)
                                .Value = row["RECEIPT_NUMBER"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["RECEIPT_NUMBER"].ToString();

                            cmd.Parameters.Add(":PLAZA_ID", OracleDbType.NVarchar2)
                                .Value = row["PLAZA_ID"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["PLAZA_ID"].ToString();

                            cmd.Parameters.Add(":LANE_NO", OracleDbType.NVarchar2)
                                .Value = row["LANE_NO"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["LANE_NO"].ToString();

                            cmd.Parameters.Add(":BANK_ID", OracleDbType.NVarchar2)
                                .Value = row["BANK_ID"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["BANK_ID"].ToString();

                            cmd.Parameters.Add(":PAYMENT_TYPE_ID", OracleDbType.NVarchar2)
                                .Value = row["PAYMENT_TYPE_ID"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["PAYMENT_TYPE_ID"].ToString();

                            cmd.Parameters.Add(":CREATED_BY", OracleDbType.Varchar2)
                                .Value = row["CREATED_BY"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["CREATED_BY"].ToString();

                            cmd.Parameters.Add(":IS_DELETED", OracleDbType.Char)
                                .Value = row["IS_DELETED"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["IS_DELETED"].ToString();

                            cmd.Parameters.Add(":DELETED_BY", OracleDbType.Decimal)
                                .Value = row["DELETED_BY"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDecimal(row["DELETED_BY"]);

                            cmd.Parameters.Add(":DELETED_DATE", OracleDbType.TimeStamp)
                                .Value = row["DELETED_DATE"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["DELETED_DATE"]);

                            cmd.Parameters.Add(":WIM_SEQUENCE_NUMBER", OracleDbType.Varchar2)
                                .Value = row["WIM_SEQUENCE_NUMBER"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["WIM_SEQUENCE_NUMBER"].ToString();

                            cmd.Parameters.Add(":IMAGE_NAME", OracleDbType.Varchar2)
                                .Value = row["IMAGE_NAME"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["IMAGE_NAME"].ToString();

                            cmd.Parameters.Add(":STATIC_WIM_GROSS", OracleDbType.Varchar2)
                                .Value = row["STATIC_WIM_GROSS"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["STATIC_WIM_GROSS"].ToString();

                            cmd.Parameters.Add(":PARENT_TRAN_ID", OracleDbType.Varchar2)
                                .Value = row["PARENT_TRAN_ID"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["PARENT_TRAN_ID"].ToString();

                            cmd.ExecuteNonQuery();

                         
                        }
                    }


                }
                catch (Exception ex)
                {
                    LogWritter($"[ERROR] Error inserting data into ALL_TRANSACTION_TB table: {ex.Message}");
                    throw; // Rethrow the exception after logging it
                }
                finally
                {
                    connection.Close();
                }
                                                                                                                                        

                }
        }


        public void BulkInsert(DataTable dataTable, string tablname)
        {
            if (dataTable.Rows.Count > 0)
            {

                using (var connection = new OracleConnection(_Destination))
                {
                    connection.Open();
                    try
                    {
                        using (OracleCommand cmd = new OracleCommand($"DELETE FROM {tablname}", connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        using (var comm = new OracleCommand())
                        {
                            comm.Connection = connection;
                            comm.CommandText = "Delete from " + tablname;
                            comm.CommandType = CommandType.Text;
                            comm.ExecuteNonQuery();
                            comm.CommandTimeout = 1000 * 60 * 5;

                        }
                        using (var bulk = new OracleBulkCopy(connection))
                        {
                            bulk.DestinationTableName = tablname;
                            bulk.BulkCopyTimeout = 300000;
                            bulk.WriteToServer(dataTable);
                        }
                        LogWritter(string.Format($@"[INFO]Bulk insert complete in temp table"));


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
                string appPath = @"D:\WimMigrationLog";

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


        private bool IsValidDbConn(string connectionString)
        {
            using var connection = new OracleConnection(connectionString);
            try
            {
                connection.Open();
                connection.Close();
                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        private string GetLastSyncID()
        {
            using var connection = new OracleConnection(_Destination);
            connection.Open();
            try
            {
                using var command = new OracleCommand(
              $"SELECT MAX(COALESCE(WIM_TRAN_ID, 0)) FROM {_tableName} WHERE PLAZA_ID = :plazaId",
              connection);

                command.Parameters.Add(new OracleParameter("plazaId", _plaza_Id));

                var result = command.ExecuteScalar();
                return (result ?? "0").ToString();
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
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

    }

}


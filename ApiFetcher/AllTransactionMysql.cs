using ApiFetcher.Notification;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
////////////////////////////////////////////////////////////////////////////////////
//                               only apply connection restore sms
//////////////////////////////////////////////////////////////////////////////////////
namespace ApiFetcher
{
    public class AllTransactionMysql: IDisposable
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
              
                if (!MysqlIsValidDbConn(_Source)) 
                {
                    LogWritter($"[ERROR] Invalid source database connection . Synchronization aborted for {_bridgeName}.");
                    return;
                }
                if (!IsValidDbConn(_Destination))
                {
                    LogWritter($"[ERROR] Invalid Destination database connection . Synchronization aborted for {_bridgeName}.");
                    return;
                }

                int lastSyncId = Convert.ToInt32(GetFirstIDMysql());
                if (lastSyncId <= 0)
                {
                    LogWritter($"[WARNING] No valid last synchronization ID found for {_bridgeName}. Skipping synchronization.");
                    return;
                }


               // GetDataLocalMysqlAll();


                //using (DataTable localData = GetDataLocalMysqlOld())
                //{
                //    if (localData.Rows.Count > 0)
                //    {
                //        LogWritter($"[INFO] Successfully retrieved records from source database of {_bridgeName} : {localData.Rows.Count}.");

                //        using (var connection = new OracleConnection(_Destination))
                //        {
                //            await connection.OpenAsync();

                //            try
                //            {
                //                InsertDataTovehicle_registrationTB(localData);
                //                BulkInsert(localData, _tableName);

                //                // Extract the month from the first row's CREATED_AT column
                //                var firstRowCreatedAt = localData.Rows[0]["CREATED_AT"];
                //                if (firstRowCreatedAt != DBNull.Value &&
                //                     DateTime.TryParse(firstRowCreatedAt.ToString(), out DateTime createdAt))
                //                {
                //                    int month = createdAt.Month;   // 1–12
                //                    int year = createdAt.Year;    // 2025 / 2026

                //                  //  InsertDataToMonthlyTable(localData, month, year);

                //                    //    // Conditional checks for the month
                //                    //    if (month == 1) // January
                //                    //    {
                //                    //        InsertDataToJanuary(localData);
                //                    //        LogWritter($"[SUCCESS] Successfully inserted into January table for {_bridgeName}.");
                //                    //    }
                //                    //    else if (month == 2) // February
                //                    //    {
                //                    //        InsertDataToFebruary(localData);
                //                    //        LogWritter($"[SUCCESS] Successfully inserted into February table for {_bridgeName}.");
                //                    //    }
                //                    //// Add additional conditions for other months if needed

                //                }
                //                else
                //                {
                //                    LogWritter("[ERROR] CREATED_AT column contains invalid or null data.");
                //                    throw new Exception("Invalid CREATED_AT value in the data.");
                //                }

                //                // Commit transaction only if both operations succeed
                //                // transaction.Commit();
                //                LogWritter($"[SUCCESS][WITH LAST INSERTED ID] Synchronized new records for {_bridgeName} : {localData.Rows.Count}. Commit successful.");
                //                if (wasConnectionDown)
                //                {
                //                    var message = $"CONNECTION RESTORED FOR {_bridgeName} SERVER AT {DateTime.Now}";
                //                    LogWritter($"[INFO] Connection restored. Sending notification: {message}");

                //                    try
                //                    {
                //                        await Task.WhenAll(
                //                            smsService.SendBulkSmsAsync(message),
                //                            smsService.SendSmsAsync(message)
                //                        );
                //                        LogWritter($"[SUCCESS] Connection up SMS sent successfully for {_bridgeName}.");
                //                    }
                //                    catch (Exception smsEx)
                //                    {
                //                        LogWritter($"[ERROR] Failed to send connection up SMS. Error: {smsEx.Message}");
                //                    }

                //                    wasConnectionDown = false; // Reset flag
                //                }


                //            }
                //            catch (Exception ex)
                //            {
                //                //  transaction.Rollback();
                //                LogWritter($"[ERROR] Transaction rolled back due to an error: {ex.Message}");
                //                throw;
                //            }
                //        }

                //    }
                //    else
                //    {
                //        LogWritter($"[WARNING] No data found locally for {_bridgeName}");
                //    }
                //}



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
        private DataTable GetDataLocalMysql()
        {
            MySqlConnection connection = null;

            try
            {
                var dataTable = new DataTable();

                connection = new MySqlConnection(_Source);
                connection.Open();

               // string lastSyncID = GetLastSyncIDMysql();
              //  string firstSyncID = GetFirstIDMysql();
                var firstSyncID = Convert.ToInt32(GetLastSyncID());

                string query = @"
                 SELECT
                     id,
                     father_husbend_name,
                     joint_owner,
                     laden_weight,
                     mobile_number,
                     nationality,
                     no_of_axle,
                     owner_address,
                     owner_name,
                     registration_date,
                     registration_number,
                     registration_office_name,
                     seating_capacity,
                     series_oid,
                     tax_token_exp_date,
                     tax_token_issue_date,
                     unladen_weight,
                     vehiclecc,
                     vehicle_class,
                     vehicle_colour,
                     vehicle_number,
                     vehicle_registration_number,
                     vehicle_series,
                     vehicle_type,
                     date_of_birth,
                     fitness_exp_date,
                     fitness_issue_date,
                     joint_owner_name,
                     request_id,
                     rfid,
                     route_permit_issue_date,
                     route_permit_number,
                     series,
                     zone,
                     chassis_no,
                     nid_number,
                     notification,
                     notification_start,
                     notification_end,
                     notification_status,
                     created_at,
                     updated_at,
                     deleted_at
                 FROM vehicle_registration
                ";

                //using (var cmd = new MySqlCommand(query, connection))
                //{
                //    cmd.Parameters.AddWithValue("@firstSyncID", Convert.ToInt64(firstSyncID));

                //    using (var reader = cmd.ExecuteReader())
                //    {
                //        dataTable.Load(reader);
                //    }
                //}

                if (firstSyncID > 0)
                {
                    query += $" WHERE TRANSACTION_ID > :firstSyncID";
                    LogWritter("[INFO]Get Local data from the Transaction ID :" + firstSyncID);
                }

                using (var cmd = new MySqlCommand(query, connection))
                {
                    if (firstSyncID > 0)
                    {
                        cmd.Parameters.AddWithValue("@firstSyncID", Convert.ToInt64(firstSyncID));
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
                LogWritter($"[ERROR] Error retrieving MySQL data: {ex.Message}");
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        private void GetDataLocalMysqlAll()
        {
            const int batchSize = 5000;
            long startId = 1;

            while (true)
            {
                DataTable dataTable = new DataTable();

                // ============================================================
                // 1. GET DATA FROM MYSQL
                // ============================================================
                using (MySqlConnection mysqlConnection = new MySqlConnection(_Source))
                {
                    try
                    {
                        mysqlConnection.Open();

                        string query = @"
                    SELECT
                        id,
                        father_husbend_name,
                        joint_owner,
                        laden_weight,
                        mobile_number,
                        nationality,
                        no_of_axle,
                        owner_address,
                        owner_name,
                        registration_date,
                        registration_number,
                        registration_office_name,
                        seating_capacity,
                        series_oid,
                        tax_token_exp_date,
                        tax_token_issue_date,
                        unladen_weight,
                        vehiclecc,
                        vehicle_class,
                        vehicle_colour,
                        vehicle_number,
                        vehicle_registration_number,
                        vehicle_series,
                        vehicle_type,
                        date_of_birth,
                        fitness_exp_date,
                        fitness_issue_date,
                        joint_owner_name,
                        request_id,
                        rfid,
                        route_permit_issue_date,
                        route_permit_number,
                        series,
                        zone,
                        chassis_no,
                        nid_number,
                        notification,
                        notification_start,
                        notification_end,
                        notification_status,
                        created_at,
                        updated_at,
                        deleted_at
                    FROM vehicle_registration
                    WHERE id >= @startId
                    ORDER BY id
                    LIMIT @batchSize";

                        using (MySqlCommand cmd = new MySqlCommand(query, mysqlConnection))
                        {
                            cmd.Parameters.Add("@startId", MySqlDbType.Int64).Value = startId;
                            cmd.Parameters.Add("@batchSize", MySqlDbType.Int32).Value = batchSize;

                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                dataTable.Load(reader);
                            }
                        }

                        LogWritter(
                            $"[INFO] MySQL retrieved {dataTable.Rows.Count} records. Start ID: {startId}");
                    }
                    catch (Exception ex)
                    {
                        LogWritter(
                            $"[ERROR] Error retrieving MySQL data: {ex.Message}");

                        throw;
                    }
                }


                // ============================================================
                // 2. NO MORE DATA
                // ============================================================
                if (dataTable.Rows.Count == 0)
                {
                    LogWritter(
                        "[INFO] No more MySQL records found. Migration completed.");

                    break;
                }


                // ============================================================
                // 3. INSERT BATCH INTO ORACLE
                // ============================================================
                using (OracleConnection oracleConnection =
                       new OracleConnection(_Destination))
                {
                    oracleConnection.Open();

                    using (OracleTransaction transaction =
                           oracleConnection.BeginTransaction())
                    {
                        try
                        {
                            string insertQuery = @"
                        INSERT INTO vehicle_registration
                        (
                            id,
                            father_husbend_name,
                            joint_owner,
                            laden_weight,
                            mobile_number,
                            nationality,
                            no_of_axle,
                            owner_address,
                            owner_name,
                            registration_date,
                            registration_number,
                            registration_office_name,
                            seating_capacity,
                            series_oid,
                            tax_token_exp_date,
                            tax_token_issue_date,
                            unladen_weight,
                            vehiclecc,
                            vehicle_class,
                            vehicle_colour,
                            vehicle_number,
                            vehicle_registration_number,
                            vehicle_series,
                            vehicle_type,
                            date_of_birth,
                            fitness_exp_date,
                            fitness_issue_date,
                            joint_owner_name,
                            request_id,
                            rfid,
                            route_permit_issue_date,
                            route_permit_number,
                            series,
                            zone,
                            chassis_no,
                            nid_number,
                            notification,
                            notification_start,
                            notification_end,
                            notification_status,
                            created_at,
                            updated_at,
                            deleted_at
                        )
                        VALUES
                        (
                            :id,
                            :father_husbend_name,
                            :joint_owner,
                            :laden_weight,
                            :mobile_number,
                            :nationality,
                            :no_of_axle,
                            :owner_address,
                            :owner_name,
                            :registration_date,
                            :registration_number,
                            :registration_office_name,
                            :seating_capacity,
                            :series_oid,
                            :tax_token_exp_date,
                            :tax_token_issue_date,
                            :unladen_weight,
                            :vehiclecc,
                            :vehicle_class,
                            :vehicle_colour,
                            :vehicle_number,
                            :vehicle_registration_number,
                            :vehicle_series,
                            :vehicle_type,
                            :date_of_birth,
                            :fitness_exp_date,
                            :fitness_issue_date,
                            :joint_owner_name,
                            :request_id,
                            :rfid,
                            :route_permit_issue_date,
                            :route_permit_number,
                            :series,
                            :zone,
                            :chassis_no,
                            :nid_number,
                            :notification,
                            :notification_start,
                            :notification_end,
                            :notification_status,
                            :created_at,
                            :updated_at,
                            :deleted_at
                        )";

                            using (OracleCommand cmd =
                                   new OracleCommand(insertQuery, oracleConnection))
                            {
                                cmd.Transaction = transaction;
                                cmd.BindByName = true;

                                // ------------------------------------------------
                                // Create parameters once
                                // ------------------------------------------------

                                cmd.Parameters.Add(":id", OracleDbType.Int64);
                                cmd.Parameters.Add(":father_husbend_name", OracleDbType.Varchar2, 255);
                                cmd.Parameters.Add(":joint_owner", OracleDbType.Varchar2, 20);
                                cmd.Parameters.Add(":laden_weight", OracleDbType.Varchar2, 20);
                                cmd.Parameters.Add(":mobile_number", OracleDbType.Varchar2, 20);
                                cmd.Parameters.Add(":nationality", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":no_of_axle", OracleDbType.Varchar2, 20);
                                cmd.Parameters.Add(":owner_address", OracleDbType.Clob);
                                cmd.Parameters.Add(":owner_name", OracleDbType.Varchar2, 255);
                                cmd.Parameters.Add(":registration_date", OracleDbType.Date);
                                cmd.Parameters.Add(":registration_number", OracleDbType.Varchar2, 50);
                                cmd.Parameters.Add(":registration_office_name", OracleDbType.Varchar2, 255);
                                cmd.Parameters.Add(":seating_capacity", OracleDbType.Int32);
                                cmd.Parameters.Add(":series_oid", OracleDbType.Varchar2, 36);
                                cmd.Parameters.Add(":tax_token_exp_date", OracleDbType.Date);
                                cmd.Parameters.Add(":tax_token_issue_date", OracleDbType.Date);
                                cmd.Parameters.Add(":unladen_weight", OracleDbType.Varchar2, 20);
                                cmd.Parameters.Add(":vehiclecc", OracleDbType.Varchar2, 20);
                                cmd.Parameters.Add(":vehicle_class", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":vehicle_colour", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":vehicle_number", OracleDbType.Varchar2, 50);
                                cmd.Parameters.Add(":vehicle_registration_number", OracleDbType.Varchar2, 50);
                                cmd.Parameters.Add(":vehicle_series", OracleDbType.Varchar2, 50);
                                cmd.Parameters.Add(":vehicle_type", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":date_of_birth", OracleDbType.Date);
                                cmd.Parameters.Add(":fitness_exp_date", OracleDbType.Date);
                                cmd.Parameters.Add(":fitness_issue_date", OracleDbType.Date);
                                cmd.Parameters.Add(":joint_owner_name", OracleDbType.Varchar2, 255);
                                cmd.Parameters.Add(":request_id", OracleDbType.Varchar2, 150);
                                cmd.Parameters.Add(":rfid", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":route_permit_issue_date", OracleDbType.Date);
                                cmd.Parameters.Add(":route_permit_number", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":series", OracleDbType.Varchar2, 50);
                                cmd.Parameters.Add(":zone", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":chassis_no", OracleDbType.Varchar2, 100);
                                cmd.Parameters.Add(":nid_number", OracleDbType.Varchar2, 30);
                                cmd.Parameters.Add(":notification", OracleDbType.Clob);
                                cmd.Parameters.Add(":notification_start", OracleDbType.Date);
                                cmd.Parameters.Add(":notification_end", OracleDbType.Date);
                                cmd.Parameters.Add(":notification_status", OracleDbType.Int32);
                                cmd.Parameters.Add(":created_at", OracleDbType.TimeStamp);
                                cmd.Parameters.Add(":updated_at", OracleDbType.TimeStamp);
                                cmd.Parameters.Add(":deleted_at", OracleDbType.Varchar2, 20);


                                // ====================================================
                                // 4. INSERT EACH ROW
                                // ====================================================

                                foreach (DataRow row in dataTable.Rows)
                                {
                                    cmd.Parameters[":id"].Value =
                                        row["id"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["id"];

                                    cmd.Parameters[":father_husbend_name"].Value =
                                        row["father_husbend_name"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["father_husbend_name"];

                                    cmd.Parameters[":joint_owner"].Value =
                                        row["joint_owner"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["joint_owner"];

                                    cmd.Parameters[":laden_weight"].Value =
                                        row["laden_weight"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["laden_weight"];

                                    cmd.Parameters[":mobile_number"].Value =
                                        row["mobile_number"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["mobile_number"];

                                    cmd.Parameters[":nationality"].Value =
                                        row["nationality"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["nationality"];

                                    cmd.Parameters[":no_of_axle"].Value =
                                        row["no_of_axle"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["no_of_axle"];

                                    cmd.Parameters[":owner_address"].Value =
                                        row["owner_address"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["owner_address"];

                                    cmd.Parameters[":owner_name"].Value =
                                        row["owner_name"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["owner_name"];

                                    cmd.Parameters[":registration_date"].Value =
                                        row["registration_date"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["registration_date"];

                                    cmd.Parameters[":registration_number"].Value =
                                        row["registration_number"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["registration_number"];

                                    cmd.Parameters[":registration_office_name"].Value =
                                        row["registration_office_name"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["registration_office_name"];

                                    cmd.Parameters[":seating_capacity"].Value =
                                        row["seating_capacity"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["seating_capacity"];

                                    cmd.Parameters[":series_oid"].Value =
                                        row["series_oid"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["series_oid"];

                                    cmd.Parameters[":tax_token_exp_date"].Value =
                                        row["tax_token_exp_date"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["tax_token_exp_date"];

                                    cmd.Parameters[":tax_token_issue_date"].Value =
                                        row["tax_token_issue_date"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["tax_token_issue_date"];

                                    cmd.Parameters[":unladen_weight"].Value =
                                        row["unladen_weight"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["unladen_weight"];

                                    cmd.Parameters[":vehiclecc"].Value =
                                        row["vehiclecc"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehiclecc"];

                                    cmd.Parameters[":vehicle_class"].Value =
                                        row["vehicle_class"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehicle_class"];

                                    cmd.Parameters[":vehicle_colour"].Value =
                                        row["vehicle_colour"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehicle_colour"];

                                    cmd.Parameters[":vehicle_number"].Value =
                                        row["vehicle_number"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehicle_number"];

                                    cmd.Parameters[":vehicle_registration_number"].Value =
                                        row["vehicle_registration_number"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehicle_registration_number"];

                                    cmd.Parameters[":vehicle_series"].Value =
                                        row["vehicle_series"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehicle_series"];

                                    cmd.Parameters[":vehicle_type"].Value =
                                        row["vehicle_type"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["vehicle_type"];

                                    cmd.Parameters[":date_of_birth"].Value =
                                        row["date_of_birth"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["date_of_birth"];

                                    cmd.Parameters[":fitness_exp_date"].Value =
                                        row["fitness_exp_date"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["fitness_exp_date"];

                                    cmd.Parameters[":fitness_issue_date"].Value =
                                        row["fitness_issue_date"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["fitness_issue_date"];

                                    cmd.Parameters[":joint_owner_name"].Value =
                                        row["joint_owner_name"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["joint_owner_name"];

                                    cmd.Parameters[":request_id"].Value =
                                        row["request_id"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["request_id"];

                                    cmd.Parameters[":rfid"].Value =
                                        row["rfid"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["rfid"];

                                    cmd.Parameters[":route_permit_issue_date"].Value =
                                        row["route_permit_issue_date"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["route_permit_issue_date"];

                                    cmd.Parameters[":route_permit_number"].Value =
                                        row["route_permit_number"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["route_permit_number"];

                                    cmd.Parameters[":series"].Value =
                                        row["series"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["series"];

                                    cmd.Parameters[":zone"].Value =
                                        row["zone"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["zone"];

                                    cmd.Parameters[":chassis_no"].Value =
                                        row["chassis_no"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["chassis_no"];

                                    cmd.Parameters[":nid_number"].Value =
                                        row["nid_number"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["nid_number"];

                                    cmd.Parameters[":notification"].Value =
                                        row["notification"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["notification"];

                                    cmd.Parameters[":notification_start"].Value =
                                        row["notification_start"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["notification_start"];

                                    cmd.Parameters[":notification_end"].Value =
                                        row["notification_end"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["notification_end"];

                                    cmd.Parameters[":notification_status"].Value =
                                        row["notification_status"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["notification_status"];

                                    cmd.Parameters[":created_at"].Value =
                                        row["created_at"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["created_at"];

                                    cmd.Parameters[":updated_at"].Value =
                                        row["updated_at"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["updated_at"];

                                    cmd.Parameters[":deleted_at"].Value =
                                        row["deleted_at"] == DBNull.Value
                                        ? DBNull.Value
                                        : row["deleted_at"];


                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // ====================================================
                            // 5. COMMIT BATCH
                            // ====================================================

                            transaction.Commit();

                            long lastId = Convert.ToInt64(
                                dataTable.Rows[dataTable.Rows.Count - 1]["id"]);

                            startId = lastId + 1;

                            LogWritter(
                                $"[INFO] Successfully inserted {dataTable.Rows.Count} records. " +
                                $"Last ID: {lastId}. Next ID: {startId}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();

                            LogWritter(
                                $"[ERROR] Oracle insert failed. " +
                                $"Batch starting ID: {startId}. " +
                                $"Transaction rolled back. Error: {ex.Message}");

                            throw;
                        }
                    }
                }
            }
        }
        private DataTable GetDataLocalMysqlOld()
        {
            MySqlConnection connection = null;

            try
            {
                var dataTable = new DataTable();

                connection = new MySqlConnection(_Source);
                connection.Open();

                long firstSyncID = 1;
                int batchSize = 5000;

                string query = @"
            SELECT
                id,
                father_husbend_name,
                joint_owner,
                laden_weight,
                mobile_number,
                nationality,
                no_of_axle,
                owner_address,
                owner_name,
                registration_date,
                registration_number,
                registration_office_name,
                seating_capacity,
                series_oid,
                tax_token_exp_date,
                tax_token_issue_date,
                unladen_weight,
                vehiclecc,
                vehicle_class,
                vehicle_colour,
                vehicle_number,
                vehicle_registration_number,
                vehicle_series,
                vehicle_type,
                date_of_birth,
                fitness_exp_date,
                fitness_issue_date,
                joint_owner_name,
                request_id,
                rfid,
                route_permit_issue_date,
                route_permit_number,
                series,
                zone,
                chassis_no,
                nid_number,
                notification,
                notification_start,
                notification_end,
                notification_status,
                created_at,
                updated_at,
                deleted_at
            FROM vehicle_registration
            WHERE id >= @firstSyncID
            ORDER BY id
            LIMIT @batchSize";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.Add("@firstSyncID", MySqlDbType.Int64).Value = firstSyncID;
                    cmd.Parameters.Add("@batchSize", MySqlDbType.Int32).Value = batchSize;

                    using (var reader = cmd.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }

                LogWritter($"[INFO] MySQL records retrieved: {dataTable.Rows.Count}");

                return dataTable;
            }
            catch (Exception ex)
            {
                LogWritter($"[ERROR] Error retrieving MySQL data: {ex.Message}");
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }




        public void InsertDataTovehicle_registrationTB(DataTable dataTable)
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                try
                {
                    connection.Open();

                    string query = @"
                INSERT INTO vehicle_registration
                (
                    id,
                    father_husbend_name,
                    joint_owner,
                    laden_weight,
                    mobile_number,
                    nationality,
                    no_of_axle,
                    owner_address,
                    owner_name,
                    registration_date,
                    registration_number,
                    registration_office_name,
                    seating_capacity,
                    series_oid,
                    tax_token_exp_date,
                    tax_token_issue_date,
                    unladen_weight,
                    vehiclecc,
                    vehicle_class,
                    vehicle_colour,
                    vehicle_number,
                    vehicle_registration_number,
                    vehicle_series,
                    vehicle_type,
                    date_of_birth,
                    fitness_exp_date,
                    fitness_issue_date,
                    joint_owner_name,
                    request_id,
                    rfid,
                    route_permit_issue_date,
                    route_permit_number,
                    series,
                    zone,
                    chassis_no,
                    nid_number,
                    notification,
                    notification_start,
                    notification_end,
                    notification_status,
                    created_at,
                    updated_at,
                    deleted_at
                )
                VALUES
                (
                    :ID,
                    :FATHER_HUSBEND_NAME,
                    :JOINT_OWNER,
                    :LADEN_WEIGHT,
                    :MOBILE_NUMBER,
                    :NATIONALITY,
                    :NO_OF_AXLE,
                    :OWNER_ADDRESS,
                    :OWNER_NAME,
                    :REGISTRATION_DATE,
                    :REGISTRATION_NUMBER,
                    :REGISTRATION_OFFICE_NAME,
                    :SEATING_CAPACITY,
                    :SERIES_OID,
                    :TAX_TOKEN_EXP_DATE,
                    :TAX_TOKEN_ISSUE_DATE,
                    :UNLADEN_WEIGHT,
                    :VEHICLECC,
                    :VEHICLE_CLASS,
                    :VEHICLE_COLOUR,
                    :VEHICLE_NUMBER,
                    :VEHICLE_REGISTRATION_NUMBER,
                    :VEHICLE_SERIES,
                    :VEHICLE_TYPE,
                    :DATE_OF_BIRTH,
                    :FITNESS_EXP_DATE,
                    :FITNESS_ISSUE_DATE,
                    :JOINT_OWNER_NAME,
                    :REQUEST_ID,
                    :RFID,
                    :ROUTE_PERMIT_ISSUE_DATE,
                    :ROUTE_PERMIT_NUMBER,
                    :SERIES,
                    :ZONE,
                    :CHASSIS_NO,
                    :NID_NUMBER,
                    :NOTIFICATION,
                    :NOTIFICATION_START,
                    :NOTIFICATION_END,
                    :NOTIFICATION_STATUS,
                    :CREATED_AT,
                    :UPDATED_AT,
                    :DELETED_AT
                )";

                    foreach (DataRow row in dataTable.Rows)
                    {
                        using (OracleCommand cmd = new OracleCommand(query, connection))
                        {
                            cmd.BindByName = true;

                            // ID
                            cmd.Parameters.Add(":ID", OracleDbType.Int64).Value =
                                row["id"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToInt64(row["id"]);

                            // VARCHAR2
                            cmd.Parameters.Add(":FATHER_HUSBEND_NAME", OracleDbType.Varchar2).Value =
                                row["father_husbend_name"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["father_husbend_name"].ToString();

                            cmd.Parameters.Add(":JOINT_OWNER", OracleDbType.Varchar2).Value =
                                row["joint_owner"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["joint_owner"].ToString();

                            cmd.Parameters.Add(":LADEN_WEIGHT", OracleDbType.Varchar2).Value =
                                row["laden_weight"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["laden_weight"].ToString();

                            cmd.Parameters.Add(":MOBILE_NUMBER", OracleDbType.Varchar2).Value =
                                row["mobile_number"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["mobile_number"].ToString();

                            cmd.Parameters.Add(":NATIONALITY", OracleDbType.Varchar2).Value =
                                row["nationality"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["nationality"].ToString();

                            cmd.Parameters.Add(":NO_OF_AXLE", OracleDbType.Varchar2).Value =
                                row["no_of_axle"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["no_of_axle"].ToString();

                            // CLOB
                            cmd.Parameters.Add(":OWNER_ADDRESS", OracleDbType.Clob).Value =
                                row["owner_address"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["owner_address"].ToString();

                            cmd.Parameters.Add(":OWNER_NAME", OracleDbType.Varchar2).Value =
                                row["owner_name"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["owner_name"].ToString();

                            // DATE
                            cmd.Parameters.Add(":REGISTRATION_DATE", OracleDbType.Date).Value =
                                row["registration_date"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["registration_date"]);

                            cmd.Parameters.Add(":REGISTRATION_NUMBER", OracleDbType.Varchar2).Value =
                                row["registration_number"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["registration_number"].ToString();

                            cmd.Parameters.Add(":REGISTRATION_OFFICE_NAME", OracleDbType.Varchar2).Value =
                                row["registration_office_name"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["registration_office_name"].ToString();

                            // NUMBER
                            cmd.Parameters.Add(":SEATING_CAPACITY", OracleDbType.Int32).Value =
                                row["seating_capacity"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToInt32(row["seating_capacity"]);

                            cmd.Parameters.Add(":SERIES_OID", OracleDbType.Varchar2).Value =
                                row["series_oid"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["series_oid"].ToString();

                            // DATE
                            cmd.Parameters.Add(":TAX_TOKEN_EXP_DATE", OracleDbType.Date).Value =
                                row["tax_token_exp_date"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["tax_token_exp_date"]);

                            cmd.Parameters.Add(":TAX_TOKEN_ISSUE_DATE", OracleDbType.Date).Value =
                                row["tax_token_issue_date"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["tax_token_issue_date"]);

                            cmd.Parameters.Add(":UNLADEN_WEIGHT", OracleDbType.Varchar2).Value =
                                row["unladen_weight"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["unladen_weight"].ToString();

                            cmd.Parameters.Add(":VEHICLECC", OracleDbType.Varchar2).Value =
                                row["vehiclecc"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehiclecc"].ToString();

                            cmd.Parameters.Add(":VEHICLE_CLASS", OracleDbType.Varchar2).Value =
                                row["vehicle_class"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehicle_class"].ToString();

                            cmd.Parameters.Add(":VEHICLE_COLOUR", OracleDbType.Varchar2).Value =
                                row["vehicle_colour"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehicle_colour"].ToString();

                            cmd.Parameters.Add(":VEHICLE_NUMBER", OracleDbType.Varchar2).Value =
                                row["vehicle_number"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehicle_number"].ToString();

                            cmd.Parameters.Add(":VEHICLE_REGISTRATION_NUMBER", OracleDbType.Varchar2).Value =
                                row["vehicle_registration_number"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehicle_registration_number"].ToString();

                            cmd.Parameters.Add(":VEHICLE_SERIES", OracleDbType.Varchar2).Value =
                                row["vehicle_series"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehicle_series"].ToString();

                            cmd.Parameters.Add(":VEHICLE_TYPE", OracleDbType.Varchar2).Value =
                                row["vehicle_type"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["vehicle_type"].ToString();

                            cmd.Parameters.Add(":DATE_OF_BIRTH", OracleDbType.Date).Value =
                                row["date_of_birth"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["date_of_birth"]);

                            cmd.Parameters.Add(":FITNESS_EXP_DATE", OracleDbType.Date).Value =
                                row["fitness_exp_date"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["fitness_exp_date"]);

                            cmd.Parameters.Add(":FITNESS_ISSUE_DATE", OracleDbType.Date).Value =
                                row["fitness_issue_date"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["fitness_issue_date"]);

                            cmd.Parameters.Add(":JOINT_OWNER_NAME", OracleDbType.Varchar2).Value =
                                row["joint_owner_name"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["joint_owner_name"].ToString();

                            cmd.Parameters.Add(":REQUEST_ID", OracleDbType.Varchar2).Value =
                                row["request_id"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["request_id"].ToString();

                            cmd.Parameters.Add(":RFID", OracleDbType.Varchar2).Value =
                                row["rfid"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["rfid"].ToString();

                            cmd.Parameters.Add(":ROUTE_PERMIT_ISSUE_DATE", OracleDbType.Date).Value =
                                row["route_permit_issue_date"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["route_permit_issue_date"]);

                            cmd.Parameters.Add(":ROUTE_PERMIT_NUMBER", OracleDbType.Varchar2).Value =
                                row["route_permit_number"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["route_permit_number"].ToString();

                            cmd.Parameters.Add(":SERIES", OracleDbType.Varchar2).Value =
                                row["series"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["series"].ToString();

                            cmd.Parameters.Add(":ZONE", OracleDbType.Varchar2).Value =
                                row["zone"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["zone"].ToString();

                            cmd.Parameters.Add(":CHASSIS_NO", OracleDbType.Varchar2).Value =
                                row["chassis_no"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["chassis_no"].ToString();

                            cmd.Parameters.Add(":NID_NUMBER", OracleDbType.Varchar2).Value =
                                row["nid_number"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["nid_number"].ToString();

                            // CLOB
                            cmd.Parameters.Add(":NOTIFICATION", OracleDbType.Clob).Value =
                                row["notification"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["notification"].ToString();

                            // DATE
                            cmd.Parameters.Add(":NOTIFICATION_START", OracleDbType.Date).Value =
                                row["notification_start"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["notification_start"]);

                            cmd.Parameters.Add(":NOTIFICATION_END", OracleDbType.Date).Value =
                                row["notification_end"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["notification_end"]);

                            cmd.Parameters.Add(":NOTIFICATION_STATUS", OracleDbType.Int32).Value =
                                row["notification_status"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToInt32(row["notification_status"]);

                            // TIMESTAMP
                            cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value =
                                row["created_at"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["created_at"]);

                            cmd.Parameters.Add(":UPDATED_AT", OracleDbType.TimeStamp).Value =
                                row["updated_at"] == DBNull.Value
                                    ? DBNull.Value
                                    : Convert.ToDateTime(row["updated_at"]);

                            // MySQL TIME -> Oracle VARCHAR2(20)
                            cmd.Parameters.Add(":DELETED_AT", OracleDbType.Varchar2).Value =
                                row["deleted_at"] == DBNull.Value
                                    ? DBNull.Value
                                    : row["deleted_at"].ToString();

                            cmd.ExecuteNonQuery();
                        }
                    }

                    LogWritter(
                        $"[INFO] Successfully inserted {dataTable.Rows.Count} records into vehicle_registration.");
                }
                catch (Exception ex)
                {
                    LogWritter(
                        $"[ERROR] Error inserting data into vehicle_registration table: {ex.Message}");

                    throw;
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

        private bool MysqlIsValidDbConn(string connectionString)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                LogWritter($"[ERROR] General error occurred while validating connection : {ex.Message}");
                return false;
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
              $"SELECT MAX(id) AS last_id\r\nFROM vehicle_registration;",
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
        private string GetLastSyncIDMysql()
        {
            using var connection = new MySqlConnection(_Source);
            connection.Open();

            try
            {
                using var command = new MySqlCommand(
                    "SELECT MAX(id) AS last_id FROM vehicle_registration;",
                    connection);

                var result = command.ExecuteScalar();

                return result == null || result == DBNull.Value
                    ? "0"
                    : result.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GetFirstIDMysql()
        {
            using var connection = new MySqlConnection(_Source);
            connection.Open();

            try
            {
                using var command = new MySqlCommand(
                    "SELECT MIN(id) AS first_id FROM vehicle_registration;",
                    connection);

                var result = command.ExecuteScalar();

                return result == null || result == DBNull.Value
                    ? "0"
                    : result.ToString();
            }
            catch (Exception ex)
            {
                throw;
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


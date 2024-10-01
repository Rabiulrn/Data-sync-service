using Oracle.ManagedDataAccess.Client;
using System.Data;


namespace ApiFetcher
{
    public class VehicleRegistrationMigration
    {
        static string _Source = "";
        static string _Destination = "";

        public async Task Sync(string Source, string Destination)
        {
            try
            {
                LogWritter("Vehicle Registraion Data Pulling");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;

                    if (Convert.ToInt32(GetLastSyncTime()) > 0)
                    {
                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter("Total Vehicle Registraion Data Count: " + data.Rows.Count);
                        }

                        BulkInsert(data, "TEMP_VEHICLES_REGISTRATION_TB_MEGHNA");
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("Vehicle Registraion New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }
                        InsertDataTableToDatabase(insertData);
                        LogWritter("Data Synced Vehicle Registraion");
                    }
                    else
                    {
                        LogWritter("No Data Found in TEMP_VEHICLES_REGISTRATION_TB_MEGHNA");
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
                string query = $@" SELECT VEHICLE_REG_ID, REGISTRATION_NUMBER, WALLET_NUMBER, MOBILE_NUMBER, BANK_ID, COMPANY_NAME, 
                                   BRTA_SERIES, BRTA_SERIES_BN_NAME, BRTA_CLASS, BRTA_TYPE, RHD_SERIES, RHD_SERIES_BN_NAME, 
                                   RHD_CLASS, RFID_NO, UNLADEN_WEIGHT, LADEN_WEIGHT, TOTAL_AXLE, OWNER_NAME, VEHICLE_CC, 
                                   PAYMENT_TYPE_ID, CREATED_AT, UPDATED_AT,STATUS
                                   FROM VEHICLES_REGISTRATION_TB
                                   ";

                if (lastSyncTime > 0)
                {
                    query += $" WHERE TRANSACTION_ID > :lastSyncTime";
                    LogWritter("Get Local and inside if  + " + lastSyncTime);
                }



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
            OracleConnection connection = new OracleConnection(_Destination);
            try
            {
                var dataTable = new DataTable();


                string query = $@"SELECT VEHICLE_REG_ID, REGISTRATION_NUMBER, WALLET_NUMBER, MOBILE_NUMBER, BANK_ID, COMPANY_NAME, 
                                   BRTA_SERIES, BRTA_SERIES_BN_NAME, BRTA_CLASS, BRTA_TYPE, RHD_SERIES, RHD_SERIES_BN_NAME, 
                                   RHD_CLASS, RFID_NO, UNLADEN_WEIGHT, LADEN_WEIGHT, TOTAL_AXLE, OWNER_NAME, VEHICLE_CC, 
                                   PAYMENT_TYPE_ID, CREATED_AT, UPDATED_AT, STATUS, IS_INSERTED
                                   FROM TEMP_VEHICLES_REGISTRATION_TB_MEGHNA WHERE IS_INSERTED IS NULL OR IS_INSERTED = 0";

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
                LogWritter("Inside ProcessTempDataBehHicleRegGet: " + dataTable.Rows.Count);
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

        public void InsertDataTableToDatabase(DataTable dataTable)
        {
            OracleConnection connection = new OracleConnection(_Destination);
            int insertedRowCount = 0; // Counter for inserted rows
            int duplicateRowCount = 0; // Counter for rows not inserted due to duplicates
            try
            {
                connection.Open();

                foreach (DataRow row in dataTable.Rows)
                {
                    string query = @"
    INSERT INTO VEHICLES_REGISTRATION_TB (
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
    )
    SELECT
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
    FROM DUAL
    WHERE NOT EXISTS (
        SELECT 1 FROM VEHICLES_REGISTRATION_TB WHERE REGISTRATION_NUMBER = :REGISTRATION_NUMBER
    )";

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        // Set the parameters
                        // cmd.Parameters.Add(":VEHICLE_REG_ID", OracleDbType.Int64).Value = row["VEHICLE_REG_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt64(row["VEHICLE_REG_ID"]);
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
                        cmd.Parameters.Add(":UNLADEN_WEIGHT", OracleDbType.Varchar2).Value = row["UNLADEN_WEIGHT"] == DBNull.Value ? DBNull.Value : row["UNLADEN_WEIGHT"].ToString();
                        cmd.Parameters.Add(":LADEN_WEIGHT", OracleDbType.Varchar2).Value = row["LADEN_WEIGHT"] == DBNull.Value ? DBNull.Value : row["LADEN_WEIGHT"].ToString();
                        cmd.Parameters.Add(":TOTAL_AXLE", OracleDbType.Varchar2).Value = row["TOTAL_AXLE"] == DBNull.Value ? DBNull.Value : row["TOTAL_AXLE"].ToString();
                        cmd.Parameters.Add(":OWNER_NAME", OracleDbType.Varchar2).Value = row["OWNER_NAME"] == DBNull.Value ? DBNull.Value : row["OWNER_NAME"].ToString();
                        cmd.Parameters.Add(":VEHICLE_CC", OracleDbType.Varchar2).Value = row["VEHICLE_CC"] == DBNull.Value ? DBNull.Value : row["VEHICLE_CC"].ToString();
                        cmd.Parameters.Add(":PAYMENT_TYPE_ID", OracleDbType.Int32).Value = row["PAYMENT_TYPE_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["PAYMENT_TYPE_ID"]);
                        cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value = row["CREATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CREATED_AT"]);
                        cmd.Parameters.Add(":UPDATED_AT", OracleDbType.TimeStamp).Value = row["UPDATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["UPDATED_AT"]);
                        cmd.Parameters.Add(":STATUS", OracleDbType.Int64).Value = row["STATUS"] == DBNull.Value ? DBNull.Value : Convert.ToInt64(row["STATUS"]);
                        // Execute the query
                        //cmd.ExecuteNonQuery();

                        // Execute the query and track rows inserted
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            insertedRowCount++; // Increment count for successful insertions
                        }
                        else
                        {
                            duplicateRowCount++; // Increment count for duplicates
                        }
                        //int rowsAffected = cmd.ExecuteNonQuery();
                        //if (rowsAffected == 0)
                        //{
                        //    Console.WriteLine("Registration number already exists: " + row["REGISTRATION_NUMBER"].ToString());
                        //}
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
                // Log only the number of successfully inserted rows
                LogWritter("Successfully inserted rows: " + insertedRowCount);
                LogWritter("Rows not inserted due to duplicates: " + duplicateRowCount);
                if (dataTable.Rows.Count > 0)
                {
                    //  LogWritter("Data Inserted For All Transaction Table is: " + dataTable.Rows.Count);
                    UpdateTempTable();
                }

            }
        }

        private void UpdateTempTable()
        {
            var connection = new OracleConnection(_Destination);
            try
            {
                LogWritter(string.Format("UPDATING TABLE AFTER INSERTION TEMP_VEHICLES_REGISTRATION_TB_MEGHNA"));

                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = "UPDATE TEMP_VEHICLES_REGISTRATION_TB_MEGHNA SET IS_INSERTED=1";
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
                    LogWritter(string.Format("Total Records : {0} for {1} All Transaction", dataTable.Rows.Count, tablname));

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
                LogWritter(string.Format("Data Not found for {0} All Transaction", tablname));
            }
        }

        public void LogWritter(string message)
        {

            string filename = "All_Transactions_" + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
            string appPath = $@"E:\\demolog";
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
                LogWritter("Connection is Valid");
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
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                //string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT MAX(VEHICLE_REG_ID) AS VEHICLE_REG_ID FROM TEMP_VEHICLES_REGISTRATION_TB_MEGHNA ";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
                {
                    try
                    {
                        connection.Open();
                        OracleDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            // DateTime actualtime  = reader.GetDateTime(0); // Assuming CREATED_AT is a DateTime column

                            string nextTransId = reader.GetString(0).ToString(); // Assuming CREATED_AT is a DateTime column
                            if (nextTransId == null || String.IsNullOrEmpty(nextTransId))
                            {
                                nextTransId = "0";
                            }

                            // DateTime createdAt = actualtime.AddMinutes(-5);
                            //DateTime createdAt = actualtime.AddMinutes(-5);

                            LogWritter("Last inserted VEHICLES_REGISTRATION_TB lAST Created_at IS : " + nextTransId.ToString());
                            //string formattedDateTime = createdAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                            //return formattedDateTime;
                            return nextTransId;
                        }
                        else
                        {
                            LogWritter("No data found in the VEHICLES_REGISTRATION_TB Table.");
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

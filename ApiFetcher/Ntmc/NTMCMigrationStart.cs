using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFetcher.Ntmc
{
    public class NTMCMigrationStart
    {

        static string _Source = "";
        static string _Destination = "";
        static string _tableName = "";
        static int _plaza_Id = 0;

        public async Task Sync(string Source, string Destination,string tableName,int plaza_Id)
        {
            try
            {
                LogWritter($@"All NTMC Data Pulling from {_tableName}");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;
                    _tableName = tableName;
                    _plaza_Id = plaza_Id;

                    if (!string.IsNullOrEmpty(GetLastSyncTime()))
                    {
                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter($@"Total {_tableName} NTMC Data Count: " + data.Rows.Count);
                        }

                        BulkInsert(data, _tableName);
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("NTMC Data New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }
                        InsertDataTableToDatabase(insertData);
                        LogWritter($@"Data Synced NTMC Data {_tableName}");
                    }
                    else
                    {
                        LogWritter($@"No Data Found in {_tableName}");
                    }
                }

            }
            catch (Exception ex)
            {

                LogWritter("Data Pulling error NTMC:" + ex.Message);
                throw;
            }

        }

        private DataTable GetDataLocal()
        {
            OracleConnection connection = new OracleConnection(_Source);
            try
            {
                var lastSyncTime = GetLastSyncTime();
                var dataTable = new DataTable();
                string query = $@"SELECT 
                          VEHICLE_REG_ID,
                          REGISTRATION_NUMBER,
                          BRTA_SERIES,
                          BRTA_SERIES_BN_NAME,
                          BRTA_CLASS,
                          BRTA_TYPE,
                          RHD_SERIES,
                          RHD_SERIES_BN_NAME,
                          RHD_CLASS,
                          BRIDGE_NAME,
                          FARE,
                          RFID_NO,
                          UNLADEN_WEIGHT,
                          LADEN_WEIGHT,
                          TOTAL_AXLE,
                          OWNER_NAME,
                          VEHICLE_CC,
                          REQUEST_BY,
                          NTMC_STATUS,
                          CREATED_AT,
                          IS_INSERTED,
                          PLAZA_ID
                        FROM 
                          NTMC_REQUEST_RESPOSE_TD ";

                if (!string.IsNullOrEmpty(lastSyncTime))
                {
                    query += " WHERE CREATED_AT > TO_TIMESTAMP(:lastSyncTime, 'DD-MON-YY HH.MI.SS.FF9 AM')";
                    LogWritter("Get from Local and inside if  + " + lastSyncTime);
                }

                //if (!string.IsNullOrEmpty(lastSyncTime))
                //{
                //    string format;
                //    // Determine if lastSyncTime is in 12-hour or 24-hour format
                //    if (lastSyncTime.Contains("AM") || lastSyncTime.Contains("PM"))
                //    {
                //        // 12-hour format
                //        format = "YYYY-MM-DD HH:MI:SS AM";
                //    }
                //    else
                //    {
                //        // Check if fractional seconds are included
                //        if (lastSyncTime.Contains("."))
                //        {
                //            // 24-hour format with fractional seconds
                //            format = "YYYY-MM-DD HH24:MI:SS.FF";
                //        }
                //        else
                //        {
                //            // 24-hour format without fractional seconds
                //            format = "YYYY-MM-DD HH24:MI:SS";
                //        }
                //    }

                //    // Correct the format for TO_TIMESTAMP function in Oracle
                //    query += $" WHERE CREATED_AT > TO_TIMESTAMP(:lastSyncTime, '{format}')";
                //    LogWritter("Get Local and inside if + " + lastSyncTime);
                //}

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
                return dataTable;
            }
            catch (Exception E)
            {
                var T = E.ToString();
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

                var dataTable = new DataTable();

                string query = $@"SELECT 
                                    VEHICLE_REG_ID,
  REGISTRATION_NUMBER,
  BRTA_SERIES,
  BRTA_SERIES_BN_NAME,
  BRTA_CLASS,
  BRTA_TYPE,
  RHD_SERIES,
  RHD_SERIES_BN_NAME,
  RHD_CLASS,
  BRIDGE_NAME,
  FARE,
  RFID_NO,
  UNLADEN_WEIGHT,
  LADEN_WEIGHT,
  TOTAL_AXLE,
  OWNER_NAME,
  VEHICLE_CC,
  REQUEST_BY,
  NTMC_STATUS,
  CREATED_AT,
  IS_INSERTED,
  PLAZA_ID
                                FROM 
                                  {_tableName} WHERE IS_INSERTED IS NULL OR IS_INSERTED = 0";

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
                LogWritter("Inside Process Data: " + dataTable.Rows.Count);
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
                        string query = @" INSERT INTO NTMC_REQUEST_RESPOSE_TD 
            (
                
                REGISTRATION_NUMBER,
                BRTA_SERIES,
                BRTA_SERIES_BN_NAME,
                BRTA_CLASS,
                BRTA_TYPE,
                RHD_SERIES,
                RHD_SERIES_BN_NAME,
                RHD_CLASS,
                BRIDGE_NAME,
                FARE,
                RFID_NO,
                UNLADEN_WEIGHT,
                LADEN_WEIGHT,
                TOTAL_AXLE,
                OWNER_NAME,
                VEHICLE_CC,
                REQUEST_BY,
                NTMC_STATUS,
                CREATED_AT,
                IS_INSERTED,
                PLAZA_ID
            ) 
            VALUES 
            (
                
                :REGISTRATION_NUMBER,
                :BRTA_SERIES,
                :BRTA_SERIES_BN_NAME,
                :BRTA_CLASS,
                :BRTA_TYPE,
                :RHD_SERIES,
                :RHD_SERIES_BN_NAME,
                :RHD_CLASS,
                :BRIDGE_NAME,
                :FARE,
                :RFID_NO,
                :UNLADEN_WEIGHT,
                :LADEN_WEIGHT,
                :TOTAL_AXLE,
                :OWNER_NAME,
                :VEHICLE_CC,
                :REQUEST_BY,
                :NTMC_STATUS,
                :CREATED_AT,
                :IS_INSERTED,
                :PLAZA_ID
            )";

                        using (OracleCommand cmd = new OracleCommand(query, connection))
                        {
                           // cmd.Parameters.Add(":VEHICLE_REG_ID", OracleDbType.Int64).Value = row["VEHICLE_REG_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt64(row["VEHICLE_REG_ID"]);
                            cmd.Parameters.Add(":REGISTRATION_NUMBER", OracleDbType.Varchar2).Value = row["REGISTRATION_NUMBER"] == DBNull.Value ? DBNull.Value : row["REGISTRATION_NUMBER"].ToString();
                            cmd.Parameters.Add(":BRTA_SERIES", OracleDbType.Varchar2).Value = row["BRTA_SERIES"] == DBNull.Value ? DBNull.Value : row["BRTA_SERIES"].ToString();
                            cmd.Parameters.Add(":BRTA_SERIES_BN_NAME", OracleDbType.Varchar2).Value = row["BRTA_SERIES_BN_NAME"] == DBNull.Value ? DBNull.Value : row["BRTA_SERIES_BN_NAME"].ToString();
                            cmd.Parameters.Add(":BRTA_CLASS", OracleDbType.Varchar2).Value = row["BRTA_CLASS"] == DBNull.Value ? DBNull.Value : row["BRTA_CLASS"].ToString();
                            cmd.Parameters.Add(":BRTA_TYPE", OracleDbType.Varchar2).Value = row["BRTA_TYPE"] == DBNull.Value ? DBNull.Value : row["BRTA_TYPE"].ToString();
                            cmd.Parameters.Add(":RHD_SERIES", OracleDbType.Varchar2).Value = row["RHD_SERIES"] == DBNull.Value ? DBNull.Value : row["RHD_SERIES"].ToString();
                            cmd.Parameters.Add(":RHD_SERIES_BN_NAME", OracleDbType.Varchar2).Value = row["RHD_SERIES_BN_NAME"] == DBNull.Value ? DBNull.Value : row["RHD_SERIES_BN_NAME"].ToString();
                            cmd.Parameters.Add(":RHD_CLASS", OracleDbType.Varchar2).Value = row["RHD_CLASS"] == DBNull.Value ? DBNull.Value : row["RHD_CLASS"].ToString();
                            cmd.Parameters.Add(":BRIDGE_NAME", OracleDbType.Varchar2).Value = row["BRIDGE_NAME"] == DBNull.Value ? DBNull.Value : row["BRIDGE_NAME"].ToString();
                            cmd.Parameters.Add(":FARE", OracleDbType.Varchar2).Value = row["FARE"] == DBNull.Value ? DBNull.Value : row["FARE"].ToString();
                            cmd.Parameters.Add(":RFID_NO", OracleDbType.Varchar2).Value = row["RFID_NO"] == DBNull.Value ? DBNull.Value : row["RFID_NO"].ToString();
                            cmd.Parameters.Add(":UNLADEN_WEIGHT", OracleDbType.Varchar2).Value = row["UNLADEN_WEIGHT"] == DBNull.Value ? DBNull.Value : row["UNLADEN_WEIGHT"].ToString();
                            cmd.Parameters.Add(":LADEN_WEIGHT", OracleDbType.Varchar2).Value = row["LADEN_WEIGHT"] == DBNull.Value ? DBNull.Value : row["LADEN_WEIGHT"].ToString();
                            cmd.Parameters.Add(":TOTAL_AXLE", OracleDbType.Varchar2).Value = row["TOTAL_AXLE"] == DBNull.Value ? DBNull.Value : row["TOTAL_AXLE"].ToString();
                            cmd.Parameters.Add(":OWNER_NAME", OracleDbType.Varchar2).Value = row["OWNER_NAME"] == DBNull.Value ? DBNull.Value : row["OWNER_NAME"].ToString();
                            cmd.Parameters.Add(":VEHICLE_CC", OracleDbType.Varchar2).Value = row["VEHICLE_CC"] == DBNull.Value ? DBNull.Value : row["VEHICLE_CC"].ToString();
                            cmd.Parameters.Add(":REQUEST_BY", OracleDbType.Varchar2).Value = row["REQUEST_BY"] == DBNull.Value ? DBNull.Value : row["REQUEST_BY"].ToString();
                            cmd.Parameters.Add(":NTMC_STATUS", OracleDbType.Int32).Value = row["NTMC_STATUS"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["NTMC_STATUS"]);
                            cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value = row["CREATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CREATED_AT"]);
                            cmd.Parameters.Add(":IS_INSERTED", OracleDbType.Int32).Value = row["IS_INSERTED"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["IS_INSERTED"]);
                            cmd.Parameters.Add(":PLAZA_ID", OracleDbType.Int32).Value = row["PLAZA_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["PLAZA_ID"]);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    if (dataTable.Rows.Count > 0)
                    {
                        LogWritter("Data Inserted For NTMC Table is: " + dataTable.Rows.Count);
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
                LogWritter(string.Format($@"UPDATING TABLE AFTER INSERTION {_tableName}"));

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
                    LogWritter(string.Format($@"Total Records : {0} for {1} NTMC {_tableName}", dataTable.Rows.Count, tablname));

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
                LogWritter(string.Format($@"Data Not found for {0} NTMC Data {_tableName}", tablname));
            }
        }

        public void LogWritter(string message)
        {

            string filename = $@"NTMC_{_tableName}" + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
            string appPath = $@"D:\\NTMC";
            int index = appPath.LastIndexOf("\\");
            appPath = appPath.Remove(index);
            string path = Path.Combine(appPath, $@"NTMC_{_tableName}" + DateTime.Now.Year.ToString());
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
                LogWritter($@"Connection is Valid for {_tableName}");
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
                string sqlQuery = $@"SELECT MAX(CREATED_AT) FROM NTMC_REQUEST_RESPOSE_TD WHERE PLAZA_ID = {_plaza_Id}";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
                {
                    try
                    {
                        connection.Open();
                        OracleDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {

                            string nextTransId = reader.GetString(0).ToString(); // Assuming CREATED_AT is a DateTime column
                            if (nextTransId == null || string.IsNullOrEmpty(nextTransId))
                            {
                                nextTransId = "0";
                            }


                            LogWritter($@"UPDATING TABLE AFTER INSERTION  {_tableName} " + nextTransId.ToString());
                            return nextTransId;
                        }
                        else
                        {
                            LogWritter( $@"UPDATING TABLE AFTER INSERTION  {_tableName}.");
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

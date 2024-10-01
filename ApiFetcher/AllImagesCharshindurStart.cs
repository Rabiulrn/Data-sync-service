using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFetcher
{
    public class AllImagesCharshindurStart
    {
        static string _Source = "";
        static string _Destination = "";

        public async Task Sync(string Source, string Destination)
        {
            try
            {
                LogWritter("Image Data Pulling from CHARSINDUR");
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;


                    if (!string.IsNullOrEmpty(GetLastSyncTime()))
                    {
                        LogWritter("Image Data Image Pulling from CHARSINDUR");
                        var data = GetDataLocal();
                        if (data.Rows.Count > 0)
                        {
                            LogWritter("Total All Images Data Count CHARSINDUR: " + data.Rows.Count);
                        }

                        BulkInsert(data, "TEMP_IMAGES_CHARSINDUR");
                        var insertData = ProcessTempDataBehHicleRegGet();
                        if (insertData.Rows.Count > 0)
                        {
                            LogWritter("Shitalokhya Images New Data Ready for Insert Count is: " + insertData.Rows.Count);
                        }

                        InsertDataTableToDatabase(insertData);
                        LogWritter("Data Synced Shitalokhya Images");
                    }
                    else
                    {
                        LogWritter("No Data Found in TEMP_IMAGES_MEGHNA");
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
                                    ID,
                                    IMAGE_NAME,
                                    IMAGE_DATA,
                                    CREATED_AT,
                                    UPDATED_AT,
                                    PLAZA_ID,
                                    IS_INSERTED
                                FROM
                                    IMAGES";
                if (!string.IsNullOrEmpty(lastSyncTime))
                {
                    query += $" WHERE CREATED_AT > TO_TIMESTAMP(:lastSyncTime, 'DD-MON-YY HH.MI.SS.FF9 AM')";
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
                string query = $@"SELECT
                                    IT.ID,
                                    IT.IMAGE_NAME,
                                    IT.IMAGE_DATA,
                                    IT.CREATED_AT,
                                    IT.UPDATED_AT,
                                    IT.PLAZA_ID
                                FROM
                                    TEMP_IMAGES_CHARSINDUR IT
                               WHERE IT.IS_INSERTED IS NULL OR IT.IS_INSERTED = 0";

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
            int isSuccess = 0;
            try
            {
                connection.Open();

                foreach (DataRow row in dataTable.Rows)
                {
                    string query = @"INSERT INTO IMAGES (IMAGE_NAME, IMAGE_DATA, CREATED_AT, UPDATED_AT,PLAZA_ID) 
                 VALUES (:IMAGE_NAME, :IMAGE_DATA, :CREATED_AT, :UPDATED_AT, :PLAZA_ID)";

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        cmd.Parameters.Add(":IMAGE_NAME", OracleDbType.Varchar2).Value = row["IMAGE_NAME"] == DBNull.Value ? DBNull.Value : row["IMAGE_NAME"].ToString();
                        byte[] imageData = new byte[0];
                        if (row["IMAGE_DATA"] != DBNull.Value)
                        {
                            imageData = (byte[])row["IMAGE_DATA"];
                        }
                        cmd.Parameters.Add(":IMAGE_DATA", OracleDbType.Blob).Value = imageData;
                        cmd.Parameters.Add(":CREATED_AT", OracleDbType.TimeStamp).Value = row["CREATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["CREATED_AT"]);
                        cmd.Parameters.Add(":UPDATED_AT", OracleDbType.TimeStamp).Value = row["UPDATED_AT"] == DBNull.Value ? DBNull.Value : Convert.ToDateTime(row["UPDATED_AT"]);
                        cmd.Parameters.Add(":PLAZA_ID", OracleDbType.Int32).Value = row["PLAZA_ID"] == DBNull.Value ? DBNull.Value : Convert.ToInt32(row["PLAZA_ID"]);


                        // Ensure all parameters are bound before executing the command
                        foreach (OracleParameter parameter in cmd.Parameters)
                        {
                            if (parameter.Value == null)
                            {
                                parameter.Value = DBNull.Value;
                            }
                        }

                        isSuccess = cmd.ExecuteNonQuery();
                    }




                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting data: " + ex.Message);
                // Handle exceptions as per your application's requirements
                throw;
            }
            finally
            {
                connection.Close();
                if (dataTable.Rows.Count > 0 && isSuccess > 0)
                {
                    LogWritter("Data Inserted For Images Table is: " + dataTable.Rows.Count);
                    UpdateTempTable();
                }
                else
                {
                    LogWritter("Data Not Inserted " + dataTable.Rows.Count);

                }

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

            string filename = "CHARSINDUR_Images_" + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
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
            OracleConnection connection = new OracleConnection(_Destination);
            try
            {
                //string sqlQuery = "SELECT CREATED_AT FROM TEMP_ALL_TRANSACTIONS_TB_SHITALOKHYA ORDER BY CREATED_AT DESC FETCH FIRST 1 ROW ONLY";
                string sqlQuery = $@"SELECT  MAX(CREATED_AT) AS CREATED_AT FROM IMAGES WHERE PLAZA_ID=2";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
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

                        LogWritter("Last inserted TEMP_IMAGES_CHARSINDUR lAST TRANS ID IS : " + nextTransId.ToString());
                        //string formattedDateTime = createdAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                        //return formattedDateTime;
                        return nextTransId;
                    }
                    else
                    {
                        LogWritter("No data found in the TEMP_IMAGES_CHARSINDUR Table.");
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
        private void UpdateTempTable()
        {
            var connection = new OracleConnection(_Destination);
            try
            {
                LogWritter(string.Format("UPDATING TABLE AFTER INSERTION TEMP_IMAGES_CHARSINDUR"));

                connection.Open();
                var comm = new OracleCommand();
                comm.Connection = connection;
                comm.CommandText = "UPDATE TEMP_IMAGES_CHARSINDUR SET IS_INSERTED=1";
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
        private bool IsValidDbConn(string Source)
        {
            OracleConnection conn = new OracleConnection(Source);
            try
            {
                conn.Open();
                conn.Close();
                LogWritter("Connection is Valid for CHARSINDUR Images");
                return true;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                conn.Close();
            }

        }
    }
}

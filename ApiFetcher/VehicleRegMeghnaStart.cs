using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFetcher
{
    public class VehicleRegMeghnaStart
    {
        private static string _Source = "";
        private static string _Destination = "";
        public async Task Sync(string Source, string Destination)
        {
            try
            {
                if (IsValidDbConn(Source))
                {
                    _Source = Source;
                    _Destination = Destination;
                    LogWritter("All Transaction Data Pulling from Meghna Started");
                    MeghnaToTempAllTrans();
                    TempToAllTransacTable();
                    LogWritter("All Transaction Data Pulling from Meghna Ended");
                }
            }
            catch (Exception ex)
            {

                LogWritter("Data Pulling error:" + ex.Message);
                throw;
            }
        }

        private void TempToAllTransacTable()
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                connection.Open();

               
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
                    UPDATED_AT
                            )
SELECT
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
    TEMP_VEHICLES_REGISTRATION_TB_MEGHNA TVR
LEFT JOIN
    VEHICLES_REGISTRATION_TB VR
ON
    TVR.VEHICLE_REG_ID = VR.VEHICLE_REG_ID
WHERE
     TVR.IS_INSERTED IS NULL OR TVR.IS_INSERTED = 0
    AND VR.REGISTRATION_NUMBER IS NULL";

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

            

            }
        }     
        private void MeghnaToTempAllTrans()
        {
            using (OracleConnection connection = new OracleConnection(_Destination))
            {
                connection.Open();

               
                    string query = @"INSERT INTO TEMP_VEHICLES_REGISTRATION_TB_MEGHNA
                (   VEHICLE_REG_ID,
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
SELECT
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
    VEHICLES_REGISTRATION_TB@MeghnaRemoteConns
ORDER BY
    VEHICLE_REG_ID ASC";

                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

            }
        }

        private void LogWritter(string message)
        {

            string filename = "MEGHNA_Vehicle_Reg_" + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
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

        }
    }
}

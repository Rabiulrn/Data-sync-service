

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
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;


namespace ApiFetcher
{
    public class UpayMigration
    {

        public async Task Sync(List<string> apiLst)
        {
            try
            {

                    LogWritter("Upay API Transaction Begins");
                    var res = await CallAPI(apiLst);
                if (res!=null)
                {
                    LogWritter($"StatusCode: {res.StatusCode}, Message: {res.Message}, IsSuccessStatus: {res.IsSuccessStatus}, EffectedRow: {res.EffectedRow}, Id: {res.Id}, Data: {res.Data}");
                }
                    LogWritter("Data Synced Upay API Transaction");
               
            }
            catch (Exception ex)
            {

                LogWritter("Upay error:" + ex.Message);
				throw;
            }

        }

       
        private async Task<ResponseResult> CallAPI(List<string> apiLst)
        {
            // string baseUrl = "http://192.168.21.100:8091/toll-fee/service/v1/registration-list/";
            ResponseResult responseLogg =new ResponseResult();
            foreach (var item in apiLst)
            {
                try
                {

                    string json = JsonConvert.SerializeObject(null);

                    using (var client = new HttpClient())
                    {
                        // Set the Accept header
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                        client.Timeout=Timeout.InfiniteTimeSpan;
                        // Set the content type to application/json
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        // Make the POST request
                        var response = await client.PostAsync(item, content);

                        // Read the response
                        string responseString = await response.Content.ReadAsStringAsync();
                        responseLogg.Data = JsonConvert.DeserializeObject<dynamic>(responseString);
                       
                    }

                }
                catch (Exception ex)
                {
                    throw;

                }
            }
          
            return responseLogg;
        }

		private void LogWritter(string message)
		{

			string filename = "Upay_APICALLER_"+DateTime.Now.ToString("yyyy_MM_dd") + ".log";
			string appPath = $@"D:\\Upay_APICALLER_";
			int index = appPath.LastIndexOf("\\");
			appPath = appPath.Remove(index);
			string path = Path.Combine(appPath, "Upay_API_CALLER" + DateTime.Now.Year.ToString());
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			File.AppendAllText(path + "\\" + filename, "\r\n" + DateTime.Now.ToString("dd HH:mm:ss.f") + "\t" + message);

		}

    }
    public class ResponseResult
    {
        public int StatusCode { get; set; }

        public string Message { get; set; }

        public bool IsSuccessStatus { get; set; }

        public int EffectedRow { get; set; }

        public string Id { get; set; }

        public dynamic Data { get; set; }

        public ResponseResult()
        {
        }


        public ResponseResult(string message, bool isSuccessStatus)
        {
            IsSuccessStatus = isSuccessStatus;
            Message = message;
            if (IsSuccessStatus)
            {
                StatusCode = 200;
            }
            else
            {
                StatusCode = 500;
            }
        }
    }

}

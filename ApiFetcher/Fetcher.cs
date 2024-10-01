using ApiFetcher.Models;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.ComponentModel;
using System.Data;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiFetcher
{
	public class Fetcher
	{
		readonly static string fetchConn = "";
		private string insertConn ;

		public async Task Sync(string urlFetch, string insertConString)
		{
			try
			{
                insertConn = insertConString;
				var datas = await Fetch(urlFetch);
				if (datas != null)
				{
					Insert(datas, "temp_registration_vehicle");
				}
				//asdas();
			}
			catch (Exception E)
			{
				throw;

			}
		
			
		}

		private async Task<DataTable> Fetch(string urlFetch)
		{
            var dataTable = new DataTable();
			string apiUrl = urlFetch;

			using (HttpClient client = new HttpClient())
			{
				try
				{
					HttpResponseMessage response = await client.GetAsync(apiUrl);

					if (response.IsSuccessStatusCode)
					{
						var data = await response.Content.ReadAsStringAsync();
						if (data.Count()>0)
						{
                            var t=  JsonConvert.DeserializeObject<IList<RegistrationFetch>>(data);
                            dataTable =ToDataTable(t);

                        }
                        Console.WriteLine(data);
					}
					else
					{
						Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
					}
				}
				catch (HttpRequestException ex)
				{
					throw;
				}
			}
	
			if (dataTable.Rows.Count > 0)
			{
				LogWritter("Total  Count: " + dataTable.Rows.Count);
			}
          
            return dataTable;
		}
	
        private void Insert(DataTable dataTable, string tablname) 
		{
			if (dataTable == null)
			{
				LogWritter(string.Format("Data Not found for {0} Table", tablname));

				return;
			}
			var connection = new OracleConnection(insertConn);
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

				LogWritter(string.Format("{1} Table: {0}", ex.Message, tablname));
                throw;
            }
			finally
			{
				connection.Close();
			}
		}
		private void LogWritter(string message)
		{
			try
			{
                string filename = DateTime.Now.ToString("yyyy_MM_dd") + "_serviceEquipment.log";
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
            catch (Exception EX)
			{

				throw;
			}
			
		}
        public DataTable ToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection props =
            TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;
        }
        private void asdas()
        {
			try
			{
                string connectionString = insertConn;

                using (OracleConnection connection = new OracleConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    // Create a command with an INSERT statement that returns the generated primary key
                    string insertQuery = "insert into Bridges (Bridge_name) values ('asdas') RETURNING ID INTO :primaryKey";
                    OracleCommand command = new OracleCommand(insertQuery, connection);

                    // Define the output parameter for the generated primary key
                    OracleParameter primaryKeyParameter = new OracleParameter("primaryKey", OracleDbType.Int32);
                    primaryKeyParameter.Direction = System.Data.ParameterDirection.Output;
                    command.Parameters.Add(primaryKeyParameter);

                    // Execute the command
                    command.ExecuteNonQuery();

                    // Retrieve the generated primary key value
                    OracleDecimal generatedPrimaryKey = (OracleDecimal)command.Parameters["primaryKey"].Value;
                    int primaryKeyValue = generatedPrimaryKey.ToInt32();
                    Console.WriteLine("Generated Primary Key: " + generatedPrimaryKey);
                }
            }
			catch (Exception E)
			{

				throw;
			}
          
        }

		
    }
}

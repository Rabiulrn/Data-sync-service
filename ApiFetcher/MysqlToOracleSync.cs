//using Microsoft.Extensions.Configuration;
//using Oracle.ManagedDataAccess.Client;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Data;
//using MySql.Data.MySqlClient;
//using Oracle.ManagedDataAccess.Client;

//namespace ApiFetcher;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly string _mySqlConnStr;
//    private readonly string _oracleConnStr;
//    private readonly int _intervalSeconds;
//    private readonly int _batchSize;

//    // Track last synced timestamp in memory (or persist to a config file/DB)
//    private DateTime _lastSyncTime = DateTime.MinValue;

//    public Worker(ILogger<Worker> logger, IConfiguration configuration)
//    {
//        _logger = logger;
//        _mySqlConnStr = configuration.GetConnectionString("MySqlDb")
//            ?? throw new ArgumentNullException("MySqlDb connection string missing.");
//        _oracleConnStr = configuration.GetConnectionString("OracleDb")
//            ?? throw new ArgumentNullException("OracleDb connection string missing.");

//        _intervalSeconds = configuration.GetValue<int>("SyncSettings:IntervalInSeconds", 60);
//        _batchSize = configuration.GetValue<int>("SyncSettings:BatchSize", 5000);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("MySQL to Oracle Sync Worker Started.");

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                await PerformSyncAsync(stoppingToken);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred during synchronization.");
//            }

//            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
//        }
//    }

//    private async Task PerformSyncAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("Checking for updated records since: {LastSyncTime}", _lastSyncTime);

//        DataTable dt = new DataTable();

//        // 1. Fetch modified data from MySQL
//        using (var mySqlConn = new MySqlConnection(_mySqlConnStr))
//        {
//            await mySqlConn.OpenAsync(ct);

//            string query = @"
//                SELECT 
//                    id, father_husbend_name, joint_owner, laden_weight, mobile_number, 
//                    nationality, no_of_axle, owner_address, owner_name, registration_date, 
//                    registration_number, registration_office_name, seating_capacity, series_oid, 
//                    tax_token_exp_date, tax_token_issue_date, unladen_weight, vehiclecc, 
//                    vehicle_class, vehicle_colour, vehicle_number, vehicle_registration_number, 
//                    vehicle_series, vehicle_type, date_of_birth, fitness_exp_date, 
//                    fitness_issue_date, joint_owner_name, request_id, rfid, 
//                    route_permit_issue_date, route_permit_number, series, zone, chassis_no, 
//                    nid_number, notification, notification_start, notification_end, 
//                    notification_status, created_at, updated_at, deleted_at
//                FROM vehicle_registration
//                WHERE updated_at > @LastSyncTime
//                ORDER BY updated_at ASC";

//            using var cmd = new MySqlCommand(query, mySqlConn);
//            cmd.Parameters.AddWithValue("@LastSyncTime", _lastSyncTime);

//            using var adapter = new MySqlDataAdapter(cmd);
//            adapter.Fill(dt);
//        }

//        if (dt.Rows.Count == 0)
//        {
//            _logger.LogInformation("No new or updated records found.");
//            return;
//        }

//        _logger.LogInformation("Fetched {Count} records from MySQL. Bulk copying to Oracle...", dt.Rows.Count);

//        // 2. Perform Bulk Write/Sync to Oracle
//        using (var oracleConn = new OracleConnection(_oracleConnStr))
//        {
//            await oracleConn.OpenAsync(ct);

//            using var bulkCopy = new OracleBulkCopy(oracleConn)
//            {
//                DestinationTableName = "VEHICLE_REGISTRATION",
//                BatchSize = _batchSize,
//                BulkCopyTimeout = 300
//            };

//            // Map MySQL columns to Oracle table columns
//            foreach (DataColumn column in dt.Columns)
//            {
//                // Oracle column names are uppercase by default
//                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName.ToUpper());
//            }

//            await bulkCopy.WriteToServerAsync(dt, ct);
//        }

//        // 3. Update internal timestamp mark to latest record's timestamp
//        var maxUpdated = dt.AsEnumerable()
//            .Max(r => r.Field<DateTime?>("updated_at"));

//        if (maxUpdated.HasValue)
//        {
//            _lastSyncTime = maxUpdated.Value;
//        }

//        _logger.LogInformation("Synchronization completed successfully.");
//    }
//}

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace StockService.API.Logging;

/// <summary>
/// Serilog configuration extensions for StockService.
/// Configures structured logging to console and SQL Server.
/// </summary>
public static class SerilogExtensions
{
    public static void ConfigureSerilog(IConfiguration config)
    {
        // Enable Serilog internal diagnostics to help detect sink issues
        SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine($"[Serilog] {msg}"));

        // Use dedicated logging database connection
        var connectionString = config.GetConnectionString("LogConnection") 
                            ?? config.GetConnectionString("DbConnection");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Validate connection string
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("[Serilog] WARNING: No connection string found for logging. SQL sink will be disabled.");
        }

        // Configure columns to match existing Logs schema
        var columnOptions = new ColumnOptions
        {
            AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn("TraceId", SqlDbType.NVarChar, dataLength: 64),
                new SqlColumn("CorrelationId", SqlDbType.NVarChar, dataLength: 64),
                new SqlColumn("UserId", SqlDbType.NVarChar, dataLength: 128),
                new SqlColumn("StockItemId", SqlDbType.UniqueIdentifier),
                new SqlColumn("WarehouseId", SqlDbType.UniqueIdentifier),
                new SqlColumn("ReservationId", SqlDbType.UniqueIdentifier),
                new SqlColumn("ProductId", SqlDbType.UniqueIdentifier),
                new SqlColumn("SourceContext", SqlDbType.NVarChar, dataLength: 256),
                new SqlColumn("RequestPath", SqlDbType.NVarChar, dataLength: 512),
                new SqlColumn("RequestMethod", SqlDbType.NVarChar, dataLength: 16),
                new SqlColumn("StatusCode", SqlDbType.Int),
                new SqlColumn("EnvironmentName", SqlDbType.NVarChar, dataLength: 64),
                new SqlColumn("Application", SqlDbType.NVarChar, dataLength: 128)
            }
        };
        
        // Configure standard columns
        columnOptions.Store.Remove(StandardColumn.Properties); // We use custom columns
        columnOptions.Store.Add(StandardColumn.LogEvent); // Store the full log event JSON
        columnOptions.TimeStamp.NonClusteredIndex = true;
        columnOptions.LogEvent.DataLength = 2048;

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            // Set minimum level to Information to capture LogInformation calls
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "StockService")
            .Enrich.WithProperty("EnvironmentName", environment)
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(new RenderedCompactJsonFormatter());

        // Add SQL Server sink only if connection string is available
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            loggerConfig.WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName = "Logs",
                    SchemaName = "dbo",
                    AutoCreateSqlTable = true,
                    BatchPostingLimit = 50,
                    BatchPeriod = TimeSpan.FromSeconds(5),
                    EagerlyEmitFirstEvent = true
                },
                // Log Information level and above to SQL
                restrictedToMinimumLevel: LogEventLevel.Information,
                columnOptions: columnOptions);
        }

        Log.Logger = loggerConfig.CreateLogger();

        // Log startup message to verify logging is working
        Log.Information("StockService Serilog configured. Environment: {Environment}, SQL Logging: {SqlEnabled}",
            environment, !string.IsNullOrWhiteSpace(connectionString));
    }
}

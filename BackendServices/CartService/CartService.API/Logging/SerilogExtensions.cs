using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace CartService.Api.Logging
{
    public static class SerilogExtensions
    {
        public static void ConfigureSerilog(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DbConnection");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

            // Configure column mappings (in code, not in appsettings)
            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn("TraceId", SqlDbType.NVarChar, dataLength: 64),
                    new SqlColumn("CorrelationId", SqlDbType.NVarChar, dataLength: 64),
                    new SqlColumn("UserId", SqlDbType.NVarChar, dataLength: 128),
                    new SqlColumn("CartId", SqlDbType.BigInt),
                    new SqlColumn("RequestPath", SqlDbType.NVarChar, dataLength: 512),
                    new SqlColumn("RequestMethod", SqlDbType.NVarChar, dataLength: 16),
                    new SqlColumn("StatusCode", SqlDbType.Int),
                    new SqlColumn("EnvironmentName", SqlDbType.NVarChar, dataLength: 64),
                    new SqlColumn("Application", SqlDbType.NVarChar, dataLength: 128)
                }
            };

            // Remove XML/Properties clutter — store structured JSON
            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.Store.Add(StandardColumn.LogEvent);
            columnOptions.TimeStamp.NonClusteredIndex = true;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "CartService")
                .Enrich.WithProperty("EnvironmentName", environment)
                .WriteTo.Console(new RenderedCompactJsonFormatter())

                // 👇 Add MSSQL Sink via code
                .WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "Logs",
                        SchemaName = "dbo",
                        BatchPostingLimit = 50,
                        BatchPeriod = TimeSpan.FromSeconds(5),
                        EagerlyEmitFirstEvent = true
                    },
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    formatProvider: null,
                    columnOptions: columnOptions)
                .CreateLogger();
        }
    }
}

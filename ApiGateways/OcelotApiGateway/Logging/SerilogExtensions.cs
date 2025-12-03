using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using Serilog.Debugging;

namespace OcelotApiGateway.Logging
{
    public static class SerilogExtensions
    {
        public static void ConfigureSerilog(IConfiguration config)
        {
            SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));

            // Use dedicated logging database
            var connectionString = config.GetConnectionString("LogConnection");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn("TraceId", SqlDbType.NVarChar, dataLength: 64),
                    new SqlColumn("CorrelationId", SqlDbType.NVarChar, dataLength: 64),
                    new SqlColumn("UserId", SqlDbType.NVarChar, dataLength: 128),
                    new SqlColumn("CartId", SqlDbType.BigInt),
                    new SqlColumn("SourceContext", SqlDbType.NVarChar, dataLength: 256),
                    new SqlColumn("RequestPath", SqlDbType.NVarChar, dataLength: 512),
                    new SqlColumn("RequestMethod", SqlDbType.NVarChar, dataLength: 16),
                    new SqlColumn("StatusCode", SqlDbType.Int),
                    new SqlColumn("EnvironmentName", SqlDbType.NVarChar, dataLength: 64),
                    new SqlColumn("Application", SqlDbType.NVarChar, dataLength: 128)
                }
            };
            columnOptions.TimeStamp.NonClusteredIndex = true;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "OcelotGateway")
                .Enrich.WithProperty("EnvironmentName", environment)
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .WriteTo.MSSqlServer(
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
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    columnOptions: columnOptions)
                .CreateLogger();
        }
    }
}

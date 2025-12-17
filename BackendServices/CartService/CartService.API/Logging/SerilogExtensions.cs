using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using Serilog.Debugging;

namespace CartService.Api.Logging
{
    public static class SerilogExtensions
    {
        public static void ConfigureSerilog(IConfiguration config)
        {
            // Enable Serilog internal diagnostics to help detect sink issues (schema/permissions)
            SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));

            // Use dedicated logging database
            var connectionString = config.GetConnectionString("LogConnection");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

            // Match existing dbo.Logs schema: keep default columns including Properties
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
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Warning()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "CartService")
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

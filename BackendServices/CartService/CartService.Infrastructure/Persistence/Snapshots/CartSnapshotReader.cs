using CartService.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Infrastructure.Persistence.Snapshots
{
    public static class CartSnapshotReader
    {
        /// <summary>
        /// Executes dbo.SP_Cart_Get and materializes all 9 result sets.
        /// </summary>
        public static async Task<CartSnapshot> ReadAsync(
            CartServiceDbContext db, long cartId, int commandTimeoutSeconds = 60, CancellationToken ct = default)
        {
            await using var conn = (SqlConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand("dbo.SP_Cart_Get", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = commandTimeoutSeconds
            };
            cmd.Parameters.Add(new SqlParameter("@CartId", SqlDbType.BigInt) { Value = cartId });

            var snap = new CartSnapshot();

            await using var rdr = await cmd.ExecuteReaderAsync(ct);

            snap.Cart = await rdr.MapListAsync<Cart>(); await rdr.NextResultAsync(ct);
            snap.Items = await rdr.MapListAsync<CartItem>(); await rdr.NextResultAsync(ct);
            snap.ItemOptions = await rdr.MapListAsync<CartItemOption>(); await rdr.NextResultAsync(ct);
            snap.Coupons = await rdr.MapListAsync<CartCoupon>(); await rdr.NextResultAsync(ct);
            snap.Adjustments = await rdr.MapListAsync<CartAdjustment>(); await rdr.NextResultAsync(ct);
            snap.Shipments = await rdr.MapListAsync<CartShipment>(); await rdr.NextResultAsync(ct);
            snap.Taxes = await rdr.MapListAsync<CartTaxis>(); await rdr.NextResultAsync(ct);
            snap.Payments = await rdr.MapListAsync<CartPayment>(); await rdr.NextResultAsync(ct);
            snap.Totals = await rdr.MapListAsync<CartTotal>();

            return snap;
        }
    }
}

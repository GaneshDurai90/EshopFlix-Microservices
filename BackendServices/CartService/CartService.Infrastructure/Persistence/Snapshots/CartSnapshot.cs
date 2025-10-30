using CartService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Infrastructure.Persistence.Snapshots
{
    /// <summary>
    /// Full cart view composed from SP_Cart_Get multi-result.
    /// </summary>
    public sealed class CartSnapshot
    {
        public List<Cart> Cart { get; set; } = new();
        public List<CartItem> Items { get; set; } = new();
        public List<CartItemOption> ItemOptions { get; set; } = new();
        public List<CartCoupon> Coupons { get; set; } = new();
        public List<CartAdjustment> Adjustments { get; set; } = new();
        public List<CartShipment> Shipments { get; set; } = new();
        public List<CartTaxis> Taxes { get; set; } = new();
        public List<CartPayment> Payments { get; set; } = new();
        public List<CartTotal> Totals { get; set; } = new();
    }
}

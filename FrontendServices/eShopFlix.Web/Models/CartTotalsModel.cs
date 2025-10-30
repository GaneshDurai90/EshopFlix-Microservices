namespace eShopFlix.Web.Models
{
    public class CartTotalsModel
    {
        public long CartId { get; set; }
        public int ItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime LastCalculated { get; set; }
    }
}

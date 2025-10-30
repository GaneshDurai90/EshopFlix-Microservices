namespace eShopFlix.Web.Models
{
    public class CouponModel
    {
        public int Id { get; set; }
        public long CartId { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsApplied { get; set; }
        public DateTime AppliedDate { get; set; }
        public string? Source { get; set; }
    }
}

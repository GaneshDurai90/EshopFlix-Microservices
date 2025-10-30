namespace eShopFlix.Web.Models
{
    public class TaxLineModel
    {
        public int Id { get; set; }
        public long CartId { get; set; }
        public string? Jurisdiction { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

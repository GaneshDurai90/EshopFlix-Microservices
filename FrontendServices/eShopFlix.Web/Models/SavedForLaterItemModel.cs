namespace eShopFlix.Web.Models
{
    public class SavedForLaterItemModel
    {
        public int Id { get; set; }
        public long CartId { get; set; }
        public int ItemId { get; set; }
        public string? Sku { get; set; }
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ProductSnapshotJson { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

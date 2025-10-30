namespace eShopFlix.Web.Models
{
    public class ShipmentModel
    {
        public int Id { get; set; }
        public long CartId { get; set; }
        public string Carrier { get; set; }
        public string MethodCode { get; set; }
        public string MethodName { get; set; }
        public decimal Cost { get; set; }
        public int? EstimatedDays { get; set; }
        public string? AddressSnapshotJson { get; set; }
        public bool IsSelected { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

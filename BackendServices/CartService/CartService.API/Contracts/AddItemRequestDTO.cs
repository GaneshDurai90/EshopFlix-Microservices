namespace CartService.API.Contracts
{
    public class AddItemRequestDTO
    {
        public long UserId { get; set; }
        public long? CartId { get; set; }

        public int ItemId { get; set; }
        public string Sku { get; set; } = string.Empty;            // optional for now if Web doesn’t send it
        public string ProductName { get; set; } = string.Empty;    // optional; Catalog can enrich later

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public string? TaxCategory { get; set; }
        public string? ProductSnapshotJson { get; set; }
        public string? VariantJson { get; set; }
        public bool IsGift { get; set; }
        public int? ParentItemId { get; set; }

    }
}

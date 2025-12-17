namespace CatalogService.API.Contracts.ProductRelationships
{
    public sealed class ProductRelationshipRequest
    {
        public int RelatedProductId { get; set; }
        public byte RelationshipType { get; set; }
        public int SortOrder { get; set; }
    }
}

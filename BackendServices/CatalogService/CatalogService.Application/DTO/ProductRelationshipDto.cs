namespace CatalogService.Application.DTO
{
    public sealed class ProductRelationshipDto
    {
        public int ParentProductId { get; set; }
        public int RelatedProductId { get; set; }
        public byte RelationshipType { get; set; }
        public int SortOrder { get; set; }
    }
}

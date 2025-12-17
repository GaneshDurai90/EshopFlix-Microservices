using System;
using System.ComponentModel.DataAnnotations.Schema;
using CatalogService.Domain.Enums;

namespace CatalogService.Domain.Entities
{
    public partial class Product
    {
        [NotMapped]
        public string ImageUrl
        {
            get => PrimaryImageUrl;
            set => PrimaryImageUrl = value;
        }

        public ProductStatus GetStatus() => (ProductStatus)Status;

        public void SetStatus(ProductStatus status) => Status = (byte)status;

        public void Activate() => SetStatus(ProductStatus.Active);

        public void Deactivate() => SetStatus(ProductStatus.Inactive);

        public void MarkDraft() => SetStatus(ProductStatus.Draft);

        public void MarkDiscontinued() => SetStatus(ProductStatus.Discontinued);

        public void UpdateBasics(
            string name,
            string slug,
            string? shortDescription,
            string? longDescription,
            int? brandId,
            int? manufacturerId,
            int? categoryId,
            bool isSearchable,
            decimal? weight,
            string? dimensions,
            string? primaryImageUrl,
            string? seoTitle,
            string? seoDescription,
            string? seoKeywords)
        {
            Name = name;
            Slug = slug;
            ShortDescription = shortDescription;
            LongDescription = longDescription;
            BrandId = brandId;
            ManufacturerId = manufacturerId;
            CategoryId = categoryId;
            IsSearchable = isSearchable;
            Weight = weight;
            Dimensions = dimensions;
            PrimaryImageUrl = primaryImageUrl;
            SeoTitle = seoTitle;
            SeoDescription = seoDescription;
            SeoKeywords = seoKeywords;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

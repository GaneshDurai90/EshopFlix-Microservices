using System;

namespace CatalogService.Domain.Entities
{
    public partial class Category
    {
        public void UpdateDetails(string name, string slug, string? description, int? parentCategoryId, int sortOrder, bool isActive)
        {
            Name = name;
            Slug = slug;
            Description = description;
            ParentCategoryId = parentCategoryId;
            SortOrder = sortOrder;
            IsActive = isActive;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

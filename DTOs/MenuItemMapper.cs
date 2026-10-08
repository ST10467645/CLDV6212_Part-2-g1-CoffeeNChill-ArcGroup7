// Attribution:
// entity handling per the Azure Tables client library documentation (Microsoft Learn, 2026c).
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.DTOs
{
    // Defines the mapper that converts between the menu DTOs and the MenuItem table entity.
    // Keeping the conversions in one place means the functions stay short and the storage concerns stay separate.
    public static class MenuItemMapper
    {
        // Converts a MenuItemRequest DTO into a MenuItem entity, it is used when a menu item is created.
        public static MenuItem ToEntity(MenuItemRequest dto) => new MenuItem
        {
            PartitionKey = dto.Category.Trim(),
            RowKey = dto.Sku.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description ?? string.Empty,
            Price = dto.Price,
            IsAvailable = dto.IsAvailable
        };

        // Converts a MenuItem entity into a MenuItemResponse DTO, it is used for every reply that returns a menu item.
        public static MenuItemResponse ToResponse(MenuItem entity) => new MenuItemResponse
        {
            Category = entity.PartitionKey,
            Sku = entity.RowKey,
            Name = entity.Name ?? string.Empty,
            Description = entity.Description ?? string.Empty,
            Price = entity.Price,
            IsAvailable = entity.IsAvailable
        };

        // Copies only the values that were sent in an update onto the existing entity, so the other values stay as they were.
        public static void ApplyUpdate(MenuItem entity, MenuItemUpdateRequest dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Name)) entity.Name = dto.Name.Trim();
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.Price.HasValue) entity.Price = dto.Price.Value;
            if (dto.IsAvailable.HasValue) entity.IsAvailable = dto.IsAvailable.Value;
        }
    }
}
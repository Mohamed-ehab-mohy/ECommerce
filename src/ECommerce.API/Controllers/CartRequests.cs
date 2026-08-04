namespace ECommerce.API.Controllers;

public sealed record AddCartItemRequest(Guid ProductId, int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);

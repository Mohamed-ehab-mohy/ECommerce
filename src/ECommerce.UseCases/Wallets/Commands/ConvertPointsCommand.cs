using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Wallets.Commands;

public sealed record ConvertPointsCommand(int Points) : IRequest<Result>;

namespace ECommerce.UseCases.Flags.Responses;

public sealed record FeatureFlagResponse(string Key, string Description, bool Enabled);

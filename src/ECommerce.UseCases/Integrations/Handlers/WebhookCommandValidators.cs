using System.Net;
using System.Text.RegularExpressions;
using ECommerce.Domain.Integrations;
using ECommerce.UseCases.Integrations.Commands;
using ECommerce.UseCases.Integrations.Queries;
using FluentValidation;

namespace ECommerce.UseCases.Integrations.Handlers;

public sealed partial class CreateWebhookEndpointCommandValidator : AbstractValidator<CreateWebhookEndpointCommand>
{
    public CreateWebhookEndpointCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("A name is required.")
            .MaximumLength(100)
            .WithMessage("Name must be at most 100 characters.");

        RuleFor(command => command.Url)
            .NotEmpty()
            .WithMessage("A URL is required.")
            .MaximumLength(2000)
            .WithMessage("URL must be at most 2000 characters.")
            .Must(url => url is not null && HttpUrlRegex().IsMatch(url))
            .WithMessage("URL must be a valid http(s) URL.")
            .Must(url => url is not null && !IsPrivateOrReservedUrl(url))
            .WithMessage("URL must not target private, reserved, or loopback addresses.");

        RuleFor(command => command.EventTypes)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one event type must be subscribed.")
            .Must(types => types is not null && types.All(WebhookEventTypes.IsSupported))
            .WithMessage("Event types must be from the supported catalog.");
    }

    [GeneratedRegex(@"^https?://[^\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex HttpUrlRegex();

    private static bool IsPrivateOrReservedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return true;

        var host = uri.Host;

        return IPAddress.TryParse(host, out var ip)
            ? IsPrivateOrReservedIp(ip)
            : host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
              host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
              host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivateOrReservedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6Multicast)
            return true;

        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                   (ip.Equals(IPAddress.IPv6Loopback) || ip.Equals(IPAddress.IPv6Any));

        var bytes = ip.GetAddressBytes();
        return bytes[0] == 0 ||
               bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168) ||
               bytes[0] == 127 ||
               (bytes[0] == 169 && bytes[1] == 254);
    }
}

public sealed class RotateWebhookSecretCommandValidator : AbstractValidator<RotateWebhookSecretCommand>
{
    public RotateWebhookSecretCommandValidator()
    {
        RuleFor(command => command.EndpointId)
            .NotEmpty()
            .WithMessage("An endpoint id is required.");
    }
}

public sealed class ReplayWebhookCommandValidator : AbstractValidator<ReplayWebhookCommand>
{
    public ReplayWebhookCommandValidator()
    {
        RuleFor(command => command.EndpointId)
            .NotEmpty()
            .WithMessage("An endpoint id is required.");
    }
}

public sealed class ListWebhookDeliveriesQueryValidator : AbstractValidator<ListWebhookDeliveriesQuery>
{
    public ListWebhookDeliveriesQueryValidator()
    {
        RuleFor(query => query.EndpointId)
            .NotEmpty()
            .WithMessage("An endpoint id is required.");

        RuleFor(query => query.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(200)
            .WithMessage("Limit must be between 1 and 200.")
            .When(query => query.Limit is not null);
    }
}

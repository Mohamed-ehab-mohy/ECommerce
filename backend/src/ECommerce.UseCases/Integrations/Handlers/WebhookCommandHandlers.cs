using ECommerce.Domain.Integrations;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Commands;
using ECommerce.UseCases.Integrations.Ports;
using ECommerce.UseCases.Integrations.Queries;
using ECommerce.UseCases.Integrations.Responses;
using ECommerce.UseCases.Integrations.Services;

namespace ECommerce.UseCases.Integrations.Handlers;

/// <summary>Registers a webhook endpoint and returns the signing secret exactly once.</summary>
public sealed class CreateWebhookEndpointCommandHandler(
    IWebhookEndpointRepository endpoints,
    IUnitOfWork unitOfWork,
    IValidator<CreateWebhookEndpointCommand> validator,
    TimeProvider timeProvider) : IRequestHandler<CreateWebhookEndpointCommand, Result<WebhookEndpointCreatedResponse>>
{
    public async Task<Result<WebhookEndpointCreatedResponse>> Handle(
        CreateWebhookEndpointCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WebhookEndpointCreatedResponse>();
        }

        var secret = WebhookSecretGenerator.Generate();
        var endpoint = WebhookEndpoint.Create(
            request.Name.Trim(),
            request.Url.Trim(),
            secret,
            request.EventTypes.Select(type => type.Trim().ToLowerInvariant()).ToList(),
            timeProvider.GetUtcNow().UtcDateTime);

        endpoints.Add(endpoint);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<WebhookEndpointCreatedResponse>.Success(new WebhookEndpointCreatedResponse(
            endpoint.Id,
            endpoint.Name,
            endpoint.Url,
            endpoint.Secret,
            endpoint.EventTypes.ToList()));
    }
}

/// <summary>Rotates the endpoint secret; the new secret is returned exactly once (docs/08 §6.10).</summary>
public sealed class RotateWebhookSecretCommandHandler(
    IWebhookEndpointRepository endpoints,
    IUnitOfWork unitOfWork,
    IValidator<RotateWebhookSecretCommand> validator,
    TimeProvider timeProvider) : IRequestHandler<RotateWebhookSecretCommand, Result<WebhookSecretRotatedResponse>>
{
    public async Task<Result<WebhookSecretRotatedResponse>> Handle(
        RotateWebhookSecretCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WebhookSecretRotatedResponse>();
        }

        var endpoint = await endpoints.GetByIdAsync(request.EndpointId, cancellationToken);

        return endpoint is null
            ? WebhookErrors.EndpointNotFound
            : await RotateAsync(endpoint, cancellationToken);
    }

    private async Task<Result<WebhookSecretRotatedResponse>> RotateAsync(
        WebhookEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        var secret = WebhookSecretGenerator.Generate();
        endpoint.RotateSecret(secret, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<WebhookSecretRotatedResponse>.Success(new WebhookSecretRotatedResponse(endpoint.Id, secret));
    }
}

/// <summary>Replays one or all failed/suspended deliveries for an endpoint (docs/08 §8.1).</summary>
public sealed class ReplayWebhookCommandHandler(
    IWebhookEndpointRepository endpoints,
    IWebhookDeliveryRepository deliveries,
    IWebhookDeliveryJobScheduler scheduler,
    IUnitOfWork unitOfWork,
    IValidator<ReplayWebhookCommand> validator,
    TimeProvider timeProvider) : IRequestHandler<ReplayWebhookCommand, Result<WebhookReplayResponse>>
{
    public async Task<Result<WebhookReplayResponse>> Handle(
        ReplayWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WebhookReplayResponse>();
        }

        var endpoint = await endpoints.GetByIdAsync(request.EndpointId, cancellationToken);
        if (endpoint is null)
        {
            return WebhookErrors.EndpointNotFound;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (request.DeliveryId is { } deliveryId)
        {
            var delivery = await deliveries.GetByIdAsync(deliveryId, cancellationToken);
            if (delivery is null || delivery.EndpointId != endpoint.Id)
            {
                return WebhookErrors.DeliveryNotFound;
            }

            if (delivery.Status != WebhookDeliveryStatus.Delivered)
            {
                delivery.ResetForReplay(utcNow);
                scheduler.Enqueue(delivery.Id);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<WebhookReplayResponse>.Success(new WebhookReplayResponse(1));
        }

        var replayable = (await deliveries.ListByEndpointAsync(endpoint.Id, cancellationToken))
            .Where(delivery => delivery.Status is WebhookDeliveryStatus.Failed or WebhookDeliveryStatus.Suspended)
            .ToList();

        foreach (var delivery in replayable)
        {
            delivery.ResetForReplay(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var delivery in replayable)
        {
            scheduler.Enqueue(delivery.Id);
        }

        return Result<WebhookReplayResponse>.Success(new WebhookReplayResponse(replayable.Count));
    }
}

/// <summary>Lists registered webhook endpoints (docs/08 §6.10).</summary>
public sealed class ListWebhookEndpointsQueryHandler(
    IWebhookEndpointRepository endpoints) : IRequestHandler<ListWebhookEndpointsQuery, Result<IReadOnlyList<WebhookEndpointResponse>>>
{
    public async Task<Result<IReadOnlyList<WebhookEndpointResponse>>> Handle(
        ListWebhookEndpointsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await endpoints.ListAsync(cancellationToken);

        return Result<IReadOnlyList<WebhookEndpointResponse>>.Success(
            all.OrderBy(endpoint => endpoint.CreatedAt)
                .Select(endpoint => new WebhookEndpointResponse(
                    endpoint.Id,
                    endpoint.Name,
                    endpoint.Url,
                    endpoint.IsActive,
                    endpoint.SuspendedUntilUtc,
                    endpoint.EventTypes.ToList()))
                .ToList());
    }
}

/// <summary>Returns the delivery log for an endpoint.</summary>
public sealed class ListWebhookDeliveriesQueryHandler(
    IWebhookEndpointRepository endpoints,
    IWebhookDeliveryRepository deliveries,
    IValidator<ListWebhookDeliveriesQuery> validator) : IRequestHandler<ListWebhookDeliveriesQuery, Result<IReadOnlyList<WebhookDeliveryResponse>>>
{
    public async Task<Result<IReadOnlyList<WebhookDeliveryResponse>>> Handle(
        ListWebhookDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<IReadOnlyList<WebhookDeliveryResponse>>();
        }

        var endpoint = await endpoints.GetByIdAsync(request.EndpointId, cancellationToken);
        if (endpoint is null)
        {
            return WebhookErrors.EndpointNotFound;
        }

        var limit = Math.Clamp(request.Limit ?? 100, 1, 200);
        var log = await deliveries.ListByEndpointAsync(endpoint.Id, cancellationToken);

        return Result<IReadOnlyList<WebhookDeliveryResponse>>.Success(
            log.Take(limit)
                .Select(delivery => new WebhookDeliveryResponse(
                    delivery.Id,
                    delivery.EndpointId,
                    delivery.EventId,
                    delivery.EventType,
                    delivery.Status.ToString(),
                    delivery.Attempts,
                    delivery.LastStatusCode,
                    delivery.LastError,
                    delivery.NextRetryAtUtc,
                    delivery.DeliveredAtUtc))
                .ToList());
    }
}

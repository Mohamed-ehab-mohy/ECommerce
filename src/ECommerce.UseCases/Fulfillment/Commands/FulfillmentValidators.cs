using ECommerce.Domain.Fulfillment;

namespace ECommerce.UseCases.Fulfillment.Commands;

public sealed class CreateFulfillmentTaskCommandValidator : AbstractValidator<CreateFulfillmentTaskCommand>
{
    public CreateFulfillmentTaskCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Zone).MaximumLength(64);
    }
}

public sealed class AssignFulfillmentTaskCommandValidator : AbstractValidator<AssignFulfillmentTaskCommand>
{
    public AssignFulfillmentTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.AssigneeId).NotEmpty();
    }
}

public sealed class StartPickingFulfillmentTaskCommandValidator : AbstractValidator<StartPickingFulfillmentTaskCommand>
{
    public StartPickingFulfillmentTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}

public sealed class MarkFulfillmentTaskPackedCommandValidator : AbstractValidator<MarkFulfillmentTaskPackedCommand>
{
    public MarkFulfillmentTaskPackedCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}

public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.CarrierKey).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DestinationCountry).NotEmpty().Length(2);
        RuleFor(x => x.DestinationPostalCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.WeightGrams).GreaterThan(0).LessThan(2_000_000);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class ApplyShipmentTrackingCommandValidator : AbstractValidator<ApplyShipmentTrackingCommand>
{
    public ApplyShipmentTrackingCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<ShipmentStatus>(status, ignoreCase: true, out _))
            .WithMessage("The shipment status is not valid.");
    }
}

public sealed class SplitFulfillmentTaskCommandValidator : AbstractValidator<SplitFulfillmentTaskCommand>
{
    public SplitFulfillmentTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ItemIds).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Zone).MaximumLength(64);
    }
}

public sealed class CorrectShippingAddressCommandValidator : AbstractValidator<CorrectShippingAddressCommand>
{
    public CorrectShippingAddressCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Phone).MaximumLength(24);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(256);
        RuleFor(x => x.City).NotEmpty().MaximumLength(96);
        RuleFor(x => x.Region).MaximumLength(96);
        RuleFor(x => x.Country).NotEmpty().Length(2);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(16);
    }
}

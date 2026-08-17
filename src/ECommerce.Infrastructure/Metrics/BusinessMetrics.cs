using System.Diagnostics.Metrics;

namespace ECommerce.Infrastructure.Metrics;

public sealed class BusinessMetrics
{
    public const string MeterName = "ECommerce.Business";

    private readonly Counter<long> _ordersPlaced;
    private readonly Counter<long> _paymentsCaptured;
    private readonly Counter<long> _paymentsFailed;
    private readonly Counter<long> _cartsAbandoned;
    private readonly Histogram<double> _checkoutDuration;

    public BusinessMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _ordersPlaced = meter.CreateCounter<long>("ecommerce.orders.placed");
        _paymentsCaptured = meter.CreateCounter<long>("ecommerce.payments.captured");
        _paymentsFailed = meter.CreateCounter<long>("ecommerce.payments.failed");
        _cartsAbandoned = meter.CreateCounter<long>("ecommerce.carts.abandoned");
        _checkoutDuration = meter.CreateHistogram<double>("ecommerce.checkout.duration_seconds", "s");
    }

    public void RecordOrderPlaced() => _ordersPlaced.Add(1);

    public void RecordPaymentCaptured() => _paymentsCaptured.Add(1);

    public void RecordPaymentFailed() => _paymentsFailed.Add(1);

    public void RecordCartAbandoned() => _cartsAbandoned.Add(1);

    public void RecordCheckoutDuration(TimeSpan duration) => _checkoutDuration.Record(duration.TotalSeconds);
}

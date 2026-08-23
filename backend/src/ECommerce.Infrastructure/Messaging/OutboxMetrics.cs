using System.Diagnostics.Metrics;

namespace ECommerce.Infrastructure.Messaging;

public sealed class OutboxMetrics
{
    public const string MeterName = "ECommerce.Outbox";

    private readonly Counter<long> _publishedCounter;
    private readonly Counter<long> _deadLetterCounter;
    private readonly Gauge<double> _lagGauge;

    public OutboxMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _publishedCounter = meter.CreateCounter<long>("outbox.messages.published");
        _deadLetterCounter = meter.CreateCounter<long>("outbox.messages.dead_lettered");
        _lagGauge = meter.CreateGauge<double>("outbox.lag_seconds");
    }

    public void RecordPublished() => _publishedCounter.Add(1);

    public void RecordDeadLetter() => _deadLetterCounter.Add(1);

    public void RecordLag(TimeSpan lag) => _lagGauge.Record(lag.TotalSeconds);
}

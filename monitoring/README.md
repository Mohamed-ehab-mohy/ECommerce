# ECommerce API Monitoring

## Health Checks

The API includes the following health check endpoints:

### Endpoints

- **`/health`** - Complete health check (all checks)
- **`/health/ready`** - Readiness probe (database + memory checks)
- **`/health/live`** - Liveness probe (always returns healthy)

### Health Check Types

1. **Database Health Check** (`/health/database`)
   - Tests SQL Server connectivity
   - Verifies database responsiveness
   - Returns detailed SQL error information

2. **Memory Health Check** (`/health/memory`)
   - Monitors memory usage
   - Configurable threshold (default: 1GB)
   - Returns GC statistics

### Response Format

```json
{
  "status": "Healthy",
  "totalDuration": 12.5,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is accessible and responding normally.",
      "duration": 5.2,
      "exception": null,
      "exceptionType": null,
      "data": null
    },
    {
      "name": "memory",
      "status": "Healthy",
      "description": "Memory usage (256.5 MB) is within normal range.",
      "duration": 0.8,
      "exception": null,
      "exceptionType": null,
      "data": {
        "allocatedBytes": 268435456,
        "allocatedMB": 256.0,
        "gen0Collections": 12,
        "gen1Collections": 3,
        "gen2Collections": 1,
        "totalAvailableMemoryMB": 16384.0,
        "heapSizeBytes": 301989888,
        "thresholdBytes": 1073741824
      }
    }
  ]
}
```

## Prometheus Metrics

The API exposes Prometheus metrics at `/metrics`.

### Available Metrics

- `http_requests_total` - Total HTTP requests
- `http_request_duration_seconds` - Request duration histogram
- `http_request_size_bytes` - Request size histogram
- `http_response_size_bytes` - Response size histogram
- `process_resident_memory_bytes` - Resident memory usage
- `process_virtual_memory_bytes` - Virtual memory usage
- `process_cpu_seconds_total` - CPU usage
- `dotnet_gc_memory_total_allocated_bytes` - GC allocated memory
- `dotnet_threadpool_thread_count` - Thread pool thread count
- `dotnet_threadpool_queue_length` - Thread pool queue length

## Grafana Dashboard

### Access

- **URL**: http://localhost:3001
- **Username**: admin
- **Password**: admin123

### Dashboard Features

1. **Health Status Panel**
   - Overall health status (Healthy/Degraded/Unhealthy)
   - Health check duration
   - Total requests count

2. **HTTP Metrics**
   - Request rate by method
   - Request duration percentiles (p50, p95, p99)
   - Status code distribution
   - Response size

3. **System Metrics**
   - Memory usage (Resident/Virtual)
   - CPU usage
   - Garbage collection
   - Thread count

## Setup

### Prerequisites

- Docker Desktop
- .NET 10 SDK
- SQL Server (local or Docker)

### Running the Stack

1. Start the API:
```bash
dotnet run --project ECommerce.API
```

2. Start monitoring stack:
```bash
docker-compose up -d
```

3. Access services:
   - **API**: http://localhost:5000
   - **Swagger**: http://localhost:5000/swagger
   - **Prometheus**: http://localhost:9090
   - **Grafana**: http://localhost:3001

### Stopping the Stack

```bash
docker-compose down
```

### Viewing Logs

```bash
docker-compose logs -f prometheus
docker-compose logs -f grafana
```

## Configuration

### Health Check Thresholds

Edit `HealthChecks/MemoryHealthCheck.cs` to adjust memory threshold:

```csharp
// Default: 1GB threshold
private readonly long _threshold = 1024L * 1024 * 1024;
```

### Prometheus Scrape Interval

Edit `monitoring/prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s  # Default: 15 seconds
```

### Grafana Admin Password

Edit `docker-compose.yml`:

```yaml
environment:
  - GF_SECURITY_ADMIN_PASSWORD=your_password
```

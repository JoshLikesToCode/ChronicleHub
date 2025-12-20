# API Usage Examples

Practical examples for common ChronicleHub API operations.

## Setup

**Set your API key:**
```bash
export API_KEY="dev-chronicle-hub-key-12345"
export BASE_URL="http://localhost:5000"
```

## Event Ingestion Examples

### User Login Event

```bash
curl -X POST $BASE_URL/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d '{
    "Type": "user_login",
    "Source": "WebApp",
    "Payload": {
      "userId": "user-12345",
      "loginMethod": "email",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "success": true
    }
  }'
```

### E-commerce Purchase

```bash
curl -X POST $BASE_URL/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d '{
    "Type": "purchase",
    "Source": "MobileApp",
    "Payload": {
      "orderId": "order-789",
      "customerId": "customer-456",
      "total": 129.99,
      "currency": "USD",
      "items": [
        {"productId": "prod-1", "quantity": 2, "price": 49.99},
        {"productId": "prod-2", "quantity": 1, "price": 30.01}
      ],
      "paymentMethod": "credit_card"
    }
  }'
```

### Page View Tracking

```bash
curl -X POST $BASE_URL/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d '{
    "Type": "page_view",
    "Source": "WebApp",
    "Payload": {
      "userId": "user-12345",
      "page": "/products/widgets",
      "referrer": "https://google.com",
      "sessionId": "session-abc",
      "duration": 45000,
      "scrollDepth": 0.75
    }
  }'
```

### Error Tracking

```bash
curl -X POST $BASE_URL/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d '{
    "Type": "error",
    "Source": "WebApp",
    "Payload": {
      "errorType": "JavaScriptError",
      "message": "Cannot read property of undefined",
      "stackTrace": "...",
      "url": "/checkout",
      "userId": "user-12345",
      "browser": "Chrome 120",
      "severity": "error"
    }
  }'
```

### Feature Usage Event

```bash
curl -X POST $BASE_URL/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d '{
    "Type": "feature_usage",
    "Source": "MobileApp",
    "Payload": {
      "featureName": "dark_mode",
      "action": "enabled",
      "userId": "user-12345",
      "platform": "iOS",
      "appVersion": "2.1.0"
    }
  }'
```

## Querying Events

### Get All Events (Paginated)

```bash
# First page (default: 20 items)
curl $BASE_URL/api/events | jq .

# Specific page and size
curl "$BASE_URL/api/events?page=2&pageSize=50" | jq .
```

### Filter by Event Type

```bash
# Get all login events
curl "$BASE_URL/api/events?type=user_login" | jq .

# Get all purchases
curl "$BASE_URL/api/events?type=purchase" | jq .
```

### Filter by Source

```bash
# Events from mobile app
curl "$BASE_URL/api/events?source=MobileApp" | jq .

# Events from web app
curl "$BASE_URL/api/events?source=WebApp" | jq .
```

### Filter by Date Range

```bash
# Events in December 2025
curl "$BASE_URL/api/events?startDate=2025-12-01&endDate=2025-12-31" | jq .

# Events in last 7 days
START_DATE=$(date -d '7 days ago' +%Y-%m-%d)
curl "$BASE_URL/api/events?startDate=$START_DATE" | jq .
```

### Complex Filtering

```bash
# Mobile app purchases in December
curl "$BASE_URL/api/events?type=purchase&source=MobileApp&startDate=2025-12-01&endDate=2025-12-31" | jq .
```

### Get Event by ID

```bash
# Get specific event
EVENT_ID="3fa85f64-5717-4562-b3fc-2c963f66afa6"
curl "$BASE_URL/api/events/$EVENT_ID" | jq .
```

## Statistics Queries

### Get Daily Statistics

```bash
# Today's stats
TODAY=$(date +%Y-%m-%d)
curl "$BASE_URL/api/stats/daily/$TODAY" | jq .

# Specific date
curl "$BASE_URL/api/stats/daily/2025-12-13" | jq .
```

### Extract Specific Categories

```bash
# Get login count for today
curl "$BASE_URL/api/stats/daily/$TODAY" | jq '.Data.CategoryBreakdown[] | select(.Category == "user_login")'

# Get total events
curl "$BASE_URL/api/stats/daily/$TODAY" | jq '.Data.TotalEvents'
```

## Bulk Operations

### Import Events from File

```bash
# events.json contains array of events
cat events.json | jq -c '.[]' | while read event; do
  curl -X POST $BASE_URL/api/events \
    -H "Content-Type: application/json" \
    -H "X-Api-Key: $API_KEY" \
    -d "$event"
  echo "Imported: $(echo $event | jq -r '.Type')"
  sleep 0.1  # Rate limiting
done
```

### Export Events to File

```bash
# Export all events (handling pagination)
page=1
total_pages=1

while [ $page -le $total_pages ]; do
  response=$(curl -s "$BASE_URL/api/events?page=$page&pageSize=100")
  echo "$response" | jq -r '.Items[]' >> events_export.jsonl
  total_pages=$(echo "$response" | jq -r '((.TotalCount + .PageSize - 1) / .PageSize | floor)')
  page=$((page + 1))
  echo "Exported page $page of $total_pages"
done
```

## Health Checks

### Check Application Health

```bash
# Liveness check
curl $BASE_URL/health/live | jq .

# Readiness check (includes DB)
curl $BASE_URL/health/ready | jq .
```

### Monitoring Script

```bash
#!/bin/bash
# monitor.sh - Simple health check monitor

while true; do
  status=$(curl -s -o /dev/null -w "%{http_code}" $BASE_URL/health/ready)
  if [ $status -eq 200 ]; then
    echo "$(date): Healthy"
  else
    echo "$(date): Unhealthy (HTTP $status)"
    # Send alert
  fi
  sleep 30
done
```

## Language-Specific Examples

### JavaScript / Node.js

```javascript
const axios = require('axios');

const BASE_URL = 'http://localhost:5000';
const API_KEY = 'dev-chronicle-hub-key-12345';

// Create event
async function createEvent(type, source, payload) {
  try {
    const response = await axios.post(`${BASE_URL}/api/events`, {
      Type: type,
      Source: source,
      Payload: payload
    }, {
      headers: {
        'X-Api-Key': API_KEY,
        'Content-Type': 'application/json'
      }
    });
    return response.data;
  } catch (error) {
    console.error('Error creating event:', error.response?.data || error.message);
    throw error;
  }
}

// Usage
createEvent('page_view', 'WebApp', {
  page: '/home',
  userId: 'user-123'
}).then(event => {
  console.log('Created event:', event.Id);
});
```

### Python

```python
import requests
from datetime import datetime

BASE_URL = 'http://localhost:5000'
API_KEY = 'dev-chronicle-hub-key-12345'

def create_event(event_type, source, payload):
    """Create a new event"""
    response = requests.post(
        f'{BASE_URL}/api/events',
        json={
            'Type': event_type,
            'Source': source,
            'Payload': payload
        },
        headers={'X-Api-Key': API_KEY}
    )
    response.raise_for_status()
    return response.json()

def get_daily_stats(date):
    """Get statistics for a date"""
    response = requests.get(f'{BASE_URL}/api/stats/daily/{date}')
    response.raise_for_status()
    return response.json()

# Usage
event = create_event('user_login', 'WebApp', {
    'userId': 'user-123',
    'loginMethod': 'email'
})
print(f"Created event: {event['Id']}")

stats = get_daily_stats('2025-12-13')
print(f"Total events: {stats['Data']['TotalEvents']}")
```

### C# / .NET

```csharp
using System.Net.Http;
using System.Net.Http.Json;

public class ChronicleHubClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ChronicleHubClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _apiKey = apiKey;
    }

    public async Task<EventResponse> CreateEventAsync(string type, string source, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Headers = { { "X-Api-Key", _apiKey } },
            Content = JsonContent.Create(new
            {
                Type = type,
                Source = source,
                Payload = payload
            })
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventResponse>();
    }
}

// Usage
var client = new ChronicleHubClient("http://localhost:5000", "dev-chronicle-hub-key-12345");
var event = await client.CreateEventAsync("page_view", "WebApp", new
{
    page = "/home",
    userId = "user-123"
});
Console.WriteLine($"Created event: {event.Id}");
```

## Testing with Sample Data

ChronicleHub includes sample event files in the `samples/` directory:

```bash
# Send sample login event
curl -X POST $BASE_URL/api/events \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d @samples/user-login-event.json

# Send sample purchase
curl -X POST $BASE_URL/api/events \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d @samples/purchase-event.json

# Send sample page view
curl -X POST $BASE_URL/api/events \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d @samples/page-view-event.json
```

## Next Steps

- [API Endpoints Reference](endpoints.md) - Complete endpoint documentation
- [Authentication Guide](authentication.md) - API key setup and security
- [Error Handling](../architecture/error-handling.md) - Understanding error responses

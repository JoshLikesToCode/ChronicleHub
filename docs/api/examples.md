# API Usage Examples

Practical examples for common ChronicleHub API operations.

## Authentication Overview

ChronicleHub uses a **dual authentication system**:

- **API Keys** (`X-Api-Key` header) - For service-to-service event ingestion (`POST /api/events`)
- **JWT Bearer Tokens** (`Authorization: Bearer` header) - For user interactions (querying events, stats, admin)

See the [Authentication Guide](authentication.md) for complete details.

## Setup

### For Event Ingestion (API Key)

```bash
# You'll need a tenant-scoped API key (created via admin API or database)
export API_KEY="ch_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0"
export BASE_URL="http://localhost:5000"
```

### For Queries and Admin (JWT Token)

```bash
export BASE_URL="http://localhost:5000"

# First, register a user and tenant
curl -X POST $BASE_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123",
    "firstName": "John",
    "lastName": "Doe",
    "tenantName": "Acme Corporation"
  }' | jq -r '.accessToken' > access_token.txt

# Save token for later use
export JWT_TOKEN=$(cat access_token.txt)
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

**Note:** All query operations require JWT authentication.

### Get All Events (Paginated)

```bash
# First page (default: 20 items)
curl -H "Authorization: Bearer $JWT_TOKEN" \
  $BASE_URL/api/events | jq .

# Specific page and size
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?page=2&pageSize=50" | jq .
```

### Filter by Event Type

```bash
# Get all login events
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?type=user_login" | jq .

# Get all purchases
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?type=purchase" | jq .
```

### Filter by Source

```bash
# Events from mobile app
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?source=MobileApp" | jq .

# Events from web app
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?source=WebApp" | jq .
```

### Filter by Date Range

```bash
# Events in December 2025
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?startDate=2025-12-01&endDate=2025-12-31" | jq .

# Events in last 7 days
START_DATE=$(date -d '7 days ago' +%Y-%m-%d)
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?startDate=$START_DATE" | jq .
```

### Complex Filtering

```bash
# Mobile app purchases in December
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events?type=purchase&source=MobileApp&startDate=2025-12-01&endDate=2025-12-31" | jq .
```

### Get Event by ID

```bash
# Get specific event
EVENT_ID="3fa85f64-5717-4562-b3fc-2c963f66afa6"
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/events/$EVENT_ID" | jq .
```

## Statistics Queries

**Note:** All stats operations require JWT authentication.

### Get Daily Statistics

```bash
# Today's stats
TODAY=$(date +%Y-%m-%d)
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/stats/daily/$TODAY" | jq .

# Specific date
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/stats/daily/2025-12-13" | jq .
```

### Extract Specific Categories

```bash
# Get login count for today
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/stats/daily/$TODAY" | jq '.Data.CategoryBreakdown[] | select(.Category == "user_login")'

# Get total events
curl -H "Authorization: Bearer $JWT_TOKEN" \
  "$BASE_URL/api/stats/daily/$TODAY" | jq '.Data.TotalEvents'
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
  response=$(curl -s -H "Authorization: Bearer $JWT_TOKEN" \
    "$BASE_URL/api/events?page=$page&pageSize=100")
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
const API_KEY = 'ch_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0';
let jwtToken = null;

// Register and login
async function registerAndLogin(email, password, firstName, lastName, tenantName) {
  try {
    const response = await axios.post(`${BASE_URL}/api/auth/register`, {
      email,
      password,
      firstName,
      lastName,
      tenantName
    });
    jwtToken = response.data.accessToken;
    return response.data;
  } catch (error) {
    console.error('Error registering:', error.response?.data || error.message);
    throw error;
  }
}

// Create event using API key
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

// Query events using JWT
async function getEvents(page = 1, pageSize = 20, type = null) {
  try {
    const params = { page, pageSize };
    if (type) params.type = type;

    const response = await axios.get(`${BASE_URL}/api/events`, {
      params,
      headers: { 'Authorization': `Bearer ${jwtToken}` }
    });
    return response.data;
  } catch (error) {
    console.error('Error querying events:', error.response?.data || error.message);
    throw error;
  }
}

// Get daily stats using JWT
async function getDailyStats(date) {
  try {
    const response = await axios.get(`${BASE_URL}/api/stats/daily/${date}`, {
      headers: { 'Authorization': `Bearer ${jwtToken}` }
    });
    return response.data;
  } catch (error) {
    console.error('Error getting stats:', error.response?.data || error.message);
    throw error;
  }
}

// Usage
(async () => {
  // First, register and login
  await registerAndLogin('user@example.com', 'SecurePass123', 'John', 'Doe', 'Acme Corp');

  // Create event using API key
  const event = await createEvent('page_view', 'WebApp', {
    page: '/home',
    userId: 'user-123'
  });
  console.log('Created event:', event.Id);

  // Query events using JWT
  const events = await getEvents(1, 20, 'page_view');
  console.log('Found events:', events.TotalCount);

  // Get stats using JWT
  const stats = await getDailyStats('2025-12-27');
  console.log('Total events:', stats.Data.TotalEvents);
})();
```

### Python

```python
import requests
from datetime import datetime

BASE_URL = 'http://localhost:5000'
API_KEY = 'ch_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0'
JWT_TOKEN = None  # Will be set after login

def register_and_login(email, password, first_name, last_name, tenant_name):
    """Register a new user and get JWT token"""
    global JWT_TOKEN
    response = requests.post(
        f'{BASE_URL}/api/auth/register',
        json={
            'email': email,
            'password': password,
            'firstName': first_name,
            'lastName': last_name,
            'tenantName': tenant_name
        }
    )
    response.raise_for_status()
    result = response.json()
    JWT_TOKEN = result['accessToken']
    return result

def create_event(event_type, source, payload):
    """Create a new event using API key"""
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

def get_events(page=1, page_size=20, event_type=None):
    """Query events using JWT token"""
    params = {'page': page, 'pageSize': page_size}
    if event_type:
        params['type'] = event_type

    response = requests.get(
        f'{BASE_URL}/api/events',
        params=params,
        headers={'Authorization': f'Bearer {JWT_TOKEN}'}
    )
    response.raise_for_status()
    return response.json()

def get_daily_stats(date):
    """Get statistics for a date using JWT token"""
    response = requests.get(
        f'{BASE_URL}/api/stats/daily/{date}',
        headers={'Authorization': f'Bearer {JWT_TOKEN}'}
    )
    response.raise_for_status()
    return response.json()

# Usage
# First, register and login
register_and_login('user@example.com', 'SecurePass123', 'John', 'Doe', 'Acme Corp')

# Create event using API key
event = create_event('user_login', 'WebApp', {
    'userId': 'user-123',
    'loginMethod': 'email'
})
print(f"Created event: {event['Id']}")

# Query events using JWT
events = get_events(event_type='user_login')
print(f"Found {events['TotalCount']} login events")

# Get stats using JWT
stats = get_daily_stats('2025-12-27')
print(f"Total events today: {stats['Data']['TotalEvents']}")
```

### C# / .NET

```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

public class ChronicleHubClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private string _jwtToken;

    public ChronicleHubClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _apiKey = apiKey;
    }

    public async Task RegisterAndLoginAsync(string email, string password,
        string firstName, string lastName, string tenantName)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            TenantName = tenantName
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        _jwtToken = result.AccessToken;
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

    public async Task<PagedEventsResponse> GetEventsAsync(int page = 1, int pageSize = 20, string type = null)
    {
        var query = $"/api/events?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(type))
            query += $"&type={type}";

        var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedEventsResponse>();
    }

    public async Task<DailyStatsResponse> GetDailyStatsAsync(string date)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/stats/daily/{date}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DailyStatsResponse>();
    }
}

// Usage
var client = new ChronicleHubClient("http://localhost:5000",
    "ch_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0");

// Register and login
await client.RegisterAndLoginAsync("user@example.com", "SecurePass123",
    "John", "Doe", "Acme Corp");

// Create event using API key
var event = await client.CreateEventAsync("page_view", "WebApp", new
{
    page = "/home",
    userId = "user-123"
});
Console.WriteLine($"Created event: {event.Id}");

// Query events using JWT
var events = await client.GetEventsAsync(page: 1, pageSize: 20, type: "page_view");
Console.WriteLine($"Found {events.TotalCount} events");

// Get stats using JWT
var stats = await client.GetDailyStatsAsync("2025-12-27");
Console.WriteLine($"Total events: {stats.Data.TotalEvents}");
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

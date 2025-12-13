# Sample Events

This directory contains sample event JSON files that demonstrate different types of events ChronicleHub can ingest.

## Sample Files

- **user-login-event.json** - User authentication event with device and location data
- **purchase-event.json** - E-commerce transaction event with order details
- **page-view-event.json** - Web analytics event with user interaction data

## How to Use

### Using cURL

```bash
# Replace with your API key (default: dev-chronicle-hub-key-12345)
API_KEY="dev-chronicle-hub-key-12345"

# Send a user login event
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d @samples/user-login-event.json

# Send a purchase event
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d @samples/purchase-event.json

# Send a page view event
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $API_KEY" \
  -d @samples/page-view-event.json
```

### Using PowerShell

```powershell
$headers = @{
    "Content-Type" = "application/json"
    "X-Api-Key" = "dev-chronicle-hub-key-12345"
}

# Send a user login event
Invoke-RestMethod -Uri "http://localhost:5000/api/events" `
  -Method Post `
  -Headers $headers `
  -Body (Get-Content samples/user-login-event.json -Raw)
```

### Using Swagger UI

1. Navigate to http://localhost:5000/swagger
2. Click the "Authorize" button
3. Enter your API key: `dev-chronicle-hub-key-12345`
4. Expand the `POST /api/events` endpoint
5. Click "Try it out"
6. Copy the contents of any sample JSON file into the request body
7. Click "Execute"

## Customizing Events

All sample events use the same structure:

- **Type**: Event category (e.g., "user_login", "purchase", "page_view")
- **Source**: Origin system (e.g., "WebApp", "MobileApp")
- **TimestampUtc**: When the event occurred (ISO 8601 format)
- **Payload**: Flexible JSON object containing event-specific data

Feel free to modify these samples or create your own event types!

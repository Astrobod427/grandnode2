# Testing Guide - Widgets.ExtendedWebApi

## Quick Start

### 1. Automated Test Script

The easiest way to test all endpoints:

```bash
cd src/Plugins/Widgets.ExtendedWebApi

# Edit the script and set your admin password
nano test-api.sh
# Set: ADMIN_PASSWORD="your_password"

# Run the test script
./test-api.sh
```

## Manual Testing

### Step 1: Get JWT Token

```bash
# Get admin JWT token
curl -X POST http://localhost:5011/api/token \
  -H "Content-Type: application/json" \
  -d '{
    "email": "francois.gendre@zohomail.com",
    "password": "YOUR_PASSWORD"
  }' | jq

# Save the token to a variable
export TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

### Step 2: Test Backend API (Admin)

#### Orders Endpoints

```bash
# List all orders
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order | jq

# Get specific order
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order/{ORDER_ID} | jq

# Mark order as paid
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order/{ORDER_ID}/MarkAsPaid

# Mark order as authorized
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order/{ORDER_ID}/MarkAsAuthorized

# Cancel order
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order/{ORDER_ID}/Cancel

# Delete order
curl -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order/{ORDER_ID}
```

#### Shipments Endpoints

```bash
# List all shipments
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/shipment | jq

# Get specific shipment
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/shipment/{SHIPMENT_ID} | jq

# Mark as shipped
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/shipment/{SHIPMENT_ID}/SetAsShipped

# Mark as delivered
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/shipment/{SHIPMENT_ID}/SetAsDelivered

# Update tracking number
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '"TRACK123456"' \
  http://localhost:5011/api/admin/shipment/{SHIPMENT_ID}/SetTrackingNumber

# Delete shipment
curl -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/shipment/{SHIPMENT_ID}
```

#### Merchandise Returns Endpoints

```bash
# List all returns
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/merchandisereturn | jq

# Get specific return
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/merchandisereturn/{RETURN_ID} | jq

# Update return status and notes
curl -X PATCH \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "merchandiseReturnStatus": 2,
    "staffNotes": "Approved for refund"
  }' \
  http://localhost:5011/api/admin/merchandisereturn/{RETURN_ID}

# Delete return
curl -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/merchandisereturn/{RETURN_ID}
```

### Step 3: Test Frontend API (Customer)

Frontend endpoints use session-based authentication (cookies).

#### Login and get session cookie

```bash
# Login to get session cookie
curl -X POST http://localhost:5011/login \
  -c cookies.txt \
  -d "Email=francois.gendre@zohomail.com&Password=YOUR_PASSWORD&RememberMe=false"
```

#### Test customer endpoints

```bash
# Get my orders
curl -b cookies.txt \
  http://localhost:5011/api/my/myorders | jq

# Get specific order
curl -b cookies.txt \
  http://localhost:5011/api/my/myorders/{ORDER_ID} | jq

# Get my shipments
curl -b cookies.txt \
  http://localhost:5011/api/my/myshipments | jq

# Get specific shipment
curl -b cookies.txt \
  http://localhost:5011/api/my/myshipments/{SHIPMENT_ID} | jq

# Get my returns
curl -b cookies.txt \
  http://localhost:5011/api/my/myreturns | jq

# Get specific return
curl -b cookies.txt \
  http://localhost:5011/api/my/myreturns/{RETURN_ID} | jq
```

## Testing with Real Data

### 1. Create Test Data via GrandNode Admin

1. Access admin panel: http://localhost:5011/admin
2. Login with: francois.gendre@zohomail.com
3. Create test orders:
   - Sales → Orders → Add new order
4. Create test shipments:
   - Sales → Orders → View → Add shipment
5. Create test returns:
   - Sales → Merchandise returns → Add new return

### 2. Get IDs from MongoDB

```bash
# Get order IDs
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.Order.find({}, {_id: 1, OrderNumber: 1}).limit(5)"

# Get shipment IDs
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.Shipment.find({}, {_id: 1, ShipmentNumber: 1}).limit(5)"

# Get return IDs
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.MerchandiseReturn.find({}, {_id: 1, ReturnNumber: 1}).limit(5)"
```

### 3. Test with Real IDs

Replace `{ORDER_ID}`, `{SHIPMENT_ID}`, `{RETURN_ID}` in the commands above with real IDs from MongoDB.

## Expected Responses

### Order DTO Example

```json
{
  "id": "507f1f77bcf86cd799439011",
  "orderNumber": 100001,
  "customerId": "507f191e810c19729de860ea",
  "customerEmail": "customer@example.com",
  "orderTotal": 99.99,
  "orderStatus": "20",
  "paymentStatus": "Paid",
  "shippingStatus": "Shipped",
  "createdOnUtc": "2024-01-15T10:30:00Z",
  "paidDateUtc": "2024-01-15T11:00:00Z",
  "currencyCode": "USD"
}
```

### Shipment DTO Example

```json
{
  "id": "507f1f77bcf86cd799439011",
  "shipmentNumber": 10001,
  "orderId": "507f191e810c19729de860ea",
  "trackingNumber": "TRACK123456",
  "totalWeight": 2.5,
  "shippedDateUtc": "2024-01-16T09:00:00Z",
  "deliveryDateUtc": null,
  "adminComment": "",
  "createdOnUtc": "2024-01-15T10:30:00Z"
}
```

### Merchandise Return DTO Example

```json
{
  "id": "507f1f77bcf86cd799439011",
  "returnNumber": 1001,
  "orderId": "507f191e810c19729de860ea",
  "customerId": "507f191e810c19729de860eb",
  "customerComments": "Product arrived damaged",
  "staffNotes": "Approved for refund",
  "merchandiseReturnStatus": "Approved",
  "pickupDate": "2024-01-20T14:00:00Z",
  "createdOnUtc": "2024-01-18T10:30:00Z"
}
```

## Common HTTP Status Codes

- `200 OK` - Successful GET request
- `201 Created` - Successful POST request creating resource
- `204 No Content` - Successful DELETE request
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Authenticated but lacks permissions
- `404 Not Found` - Resource doesn't exist

## Troubleshooting

### Issue: "401 Unauthorized" on backend endpoints

**Solution:** Make sure you have a valid JWT token and include it in the Authorization header:
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" ...
```

### Issue: "401 Unauthorized" on frontend endpoints

**Solution:** Login first to get a session cookie:
```bash
curl -X POST http://localhost:5011/login -c cookies.txt -d "Email=...&Password=..."
curl -b cookies.txt http://localhost:5011/api/my/myorders
```

### Issue: "403 Forbidden" on backend endpoints

**Solution:** Your user needs to be in the "Administrators" group. Check in MongoDB:
```bash
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.Customer.findOne({Email: 'your@email.com'}, {Groups: 1})"
```

### Issue: Token endpoint returns 400

**Solution:** Check your credentials and ensure the API is enabled in GrandNode settings.

## Advanced Testing

### Using Postman

1. Import the following environment variables:
   - `base_url`: http://localhost:5011
   - `admin_email`: francois.gendre@zohomail.com
   - `admin_password`: YOUR_PASSWORD

2. Create a request to get token:
   - POST `{{base_url}}/api/token`
   - Body: `{"email": "{{admin_email}}", "password": "{{admin_password}}"}`
   - Save token to environment variable

3. Use `{{token}}` in Authorization header for other requests

### Using HTTPie

```bash
# Install httpie
pip install httpie

# Get token
http POST localhost:5011/api/token email=francois.gendre@zohomail.com password=YOUR_PASSWORD

# Use token
http localhost:5011/api/admin/order "Authorization: Bearer TOKEN"
```

### Performance Testing with Apache Bench

```bash
# Test orders endpoint performance
ab -n 100 -c 10 -H "Authorization: Bearer YOUR_TOKEN" \
  http://localhost:5011/api/admin/order
```

## Integration Testing

For automated integration tests, see the main GrandNode test suite documentation.
The plugin follows the same testing patterns as other GrandNode modules.

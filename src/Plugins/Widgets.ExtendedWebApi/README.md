# Widgets.ExtendedWebApi

Extended REST API plugin for GrandNode 2 that provides additional endpoints for managing orders, shipments, and merchandise returns.

## Features

This plugin extends the standard GrandNode API with two sets of endpoints:

### Backend API (Admin)
RESTful endpoints for administrative operations with JWT Bearer authentication:
- **Orders Management** - View, mark as paid/authorized, cancel, and delete orders
- **Shipments Management** - View, mark as shipped/delivered, update tracking numbers, and delete shipments
- **Merchandise Returns Management** - View, update status, add staff notes, and delete returns

### Frontend API (Customer)
Customer-facing API endpoints with session-based authentication:
- **My Orders** - View own orders
- **My Shipments** - Track own shipments
- **My Returns** - View own merchandise returns

## Installation

### 1. Build the Plugin

```bash
cd src/Plugins/Widgets.ExtendedWebApi
dotnet build
```

The plugin will automatically be copied to `/src/Web/Grand.Web/Plugins/Widgets.ExtendedWebApi/`.

### 2. Install via Admin Panel

1. Navigate to **Configuration → Plugins → Local plugins**
2. Find **Widgets.ExtendedWebApi**
3. Click **Install**
4. Restart the application

## API Endpoints

### Backend API (Admin)

Base URL: `/api/admin/`

Authentication: JWT Bearer token (same as standard GrandNode API)

#### Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/order` | List all orders |
| GET | `/api/admin/order/{id}` | Get order details |
| POST | `/api/admin/order/{id}/MarkAsPaid` | Mark order as paid |
| POST | `/api/admin/order/{id}/MarkAsAuthorized` | Mark payment as authorized |
| POST | `/api/admin/order/{id}/Cancel` | Cancel order |
| DELETE | `/api/admin/order/{id}` | Delete order |

#### Shipments

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/shipment` | List all shipments |
| GET | `/api/admin/shipment/{id}` | Get shipment details |
| POST | `/api/admin/shipment/{id}/SetAsShipped` | Mark shipment as shipped |
| POST | `/api/admin/shipment/{id}/SetAsDelivered` | Mark shipment as delivered |
| POST | `/api/admin/shipment/{id}/SetTrackingNumber` | Update tracking number |
| DELETE | `/api/admin/shipment/{id}` | Delete shipment |

#### Merchandise Returns

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/merchandisereturn` | List all returns |
| GET | `/api/admin/merchandisereturn/{id}` | Get return details |
| PATCH | `/api/admin/merchandisereturn/{id}` | Update return status/notes |
| DELETE | `/api/admin/merchandisereturn/{id}` | Delete return |

#### Products

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/product` | List all products with pagination |
| GET | `/api/admin/product/{id}` | Get product details |
| **GET** | **`/api/admin/product/search`** | **Advanced search with filters** |

**📖 Advanced Product Search**

The `/search` endpoint provides powerful filtering capabilities including:
- Full-text search across name, description, SKU, and tags
- Category, brand, and vendor filtering
- Price range filtering
- Featured/new/homepage product filtering
- Multiple sort options (position, name, price, date, best sellers)
- Pagination support

For detailed documentation with examples, see **[API-SEARCH.md](./API-SEARCH.md)**

**Quick Example:**
```bash
GET /api/admin/product/search?keywords=laptop&priceMin=500&priceMax=2000&orderBy=2
```

#### Categories

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/category` | List all categories with pagination |
| GET | `/api/admin/category/{id}` | Get category details |

#### Customers

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/customer` | List all customers with pagination |
| GET | `/api/admin/customer/{id}` | Get customer details |

### Frontend API (Customer)

Base URL: `/api/my/`

Authentication: Session-based (FrontAuthentication)

#### My Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/my/myorders` | List customer's orders |
| GET | `/api/my/myorders/{id}` | Get order details |

#### My Shipments

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/my/myshipments` | List customer's shipments |
| GET | `/api/my/myshipments/{id}` | Get shipment details |

#### My Returns

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/my/myreturns` | List customer's returns |
| GET | `/api/my/myreturns/{id}` | Get return details |

## Authentication

### Backend API (Admin)

Use JWT Bearer token authentication:

```bash
# 1. Get JWT token
curl -X POST https://yoursite.com/api/token \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@yourstore.com",
    "password": "yourpassword"
  }'

# 2. Use token in requests
curl https://yoursite.com/api/admin/order \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Frontend API (Customer)

Use session-based authentication (cookies):

```bash
# 1. Login (get session cookie)
curl -X POST https://yoursite.com/login \
  -c cookies.txt \
  -d "Email=customer@example.com&Password=password"

# 2. Use session cookie in requests
curl https://yoursite.com/api/my/myorders \
  -b cookies.txt
```

## Usage Examples

### Mark Order as Paid

```bash
curl -X POST https://yoursite.com/api/admin/order/507f1f77bcf86cd799439011/MarkAsPaid \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Update Tracking Number

```bash
curl -X POST https://yoursite.com/api/admin/shipment/507f1f77bcf86cd799439011/SetTrackingNumber \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '"TRACK123456"'
```

### Update Return Status

```bash
curl -X PATCH https://yoursite.com/api/admin/merchandisereturn/507f1f77bcf86cd799439011 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "merchandiseReturnStatus": 2,
    "staffNotes": "Approved for refund"
  }'
```

### Get Customer Orders (Frontend)

```bash
curl https://yoursite.com/api/my/myorders \
  -b cookies.txt
```

## Response DTOs

### OrderDto

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

### ShipmentDto

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

### MerchandiseReturnDto

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

## Permissions

Backend endpoints require the following GrandNode permissions:
- **ManageOrders** - Required for all backend endpoints

Frontend endpoints automatically filter results by the authenticated customer.

## Dependencies

- GrandNode 2.3.0+
- ASP.NET Core 9.0
- Microsoft.AspNetCore.OData 9.0.0
- AutoMapper

## Technical Details

- **OData Support**: The plugin includes OData dependencies for potential future query expansion
- **Model Validation**: Automatic model validation via `ModelValidationAttribute`
- **MediatR Integration**: Uses GrandNode's MediatR commands for business logic
- **Security**:
  - Backend: JWT Bearer authentication with permission checks
  - Frontend: Session-based authentication with customer filtering

## License

Same as GrandNode 2 (MIT License)

## Support

For issues or feature requests, please use the main GrandNode repository.

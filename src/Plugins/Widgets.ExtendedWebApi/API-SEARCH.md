# Advanced Product Search API

## Endpoint

```
GET /api/admin/Product/search
```

## Authentication

Requires Bearer token with `ManageProducts` permission.

```bash
Authorization: Bearer YOUR_JWT_TOKEN
```

## Overview

The advanced search endpoint provides powerful filtering and search capabilities for products using MongoDB's native search features. All parameters are optional and can be combined for complex queries.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `keywords` | string | "" | Search text (searches product name by default) |
| `searchDescriptions` | bool | false | Include short and full descriptions in search |
| `searchSku` | bool | true | Include SKU in search |
| `searchProductTags` | bool | false | Include product tags in search |
| `categoryIds` | string | "" | Filter by categories (comma-separated IDs) |
| `brandId` | string | "" | Filter by brand ID |
| `vendorId` | string | "" | Filter by vendor ID |
| `priceMin` | double? | null | Minimum price filter |
| `priceMax` | double? | null | Maximum price filter |
| `showOnHomePage` | bool? | null | Filter products shown on homepage (true/false) |
| `featuredProducts` | bool? | null | Filter featured products (true/false) |
| `markedAsNewOnly` | bool | false | Show only products marked as new |
| `publishedOnly` | bool | true | Show only published products |
| `orderBy` | int | 0 | Sort order (see below) |
| `pageIndex` | int | 0 | Page number (0-based) |
| `pageSize` | int | 50 | Items per page (max 100) |

### Sort Options (orderBy)

| Value | Sort By |
|-------|---------|
| 0 | Position (default) |
| 1 | Name (A-Z) |
| 2 | Price (low to high) |
| 3 | Created Date (newest first) |
| 4 | Best Sellers |
| 5 | On Sale |

## Response Format

```json
{
  "items": [
    {
      "id": "string",
      "name": "string",
      "shortDescription": "string",
      "fullDescription": "string",
      "sku": "string",
      "gtin": "string",
      "brandId": "string",
      "vendorId": "string",
      "price": 0.0,
      "oldPrice": 0.0,
      "catalogPrice": 0.0,
      "stockQuantity": 0,
      "published": true,
      "showOnHomePage": false,
      "bestSeller": false,
      "createdOnUtc": "2025-01-01T00:00:00Z",
      "updatedOnUtc": "2025-01-01T00:00:00Z"
    }
  ],
  "pageIndex": 0,
  "pageSize": 50,
  "totalCount": 100,
  "searchCriteria": {
    "keywords": "laptop",
    "searchDescriptions": true,
    "searchSku": true,
    "searchProductTags": false,
    "categoryIds": "cat123,cat456",
    "brandId": "",
    "vendorId": "",
    "priceMin": 500,
    "priceMax": 2000,
    "orderBy": 2
  }
}
```

## Examples

### 1. Simple Text Search

Search for products containing "laptop" in the name:

```bash
GET /api/admin/Product/search?keywords=laptop
```

### 2. Full-Text Search

Search in name, descriptions, and tags:

```bash
GET /api/admin/Product/search?keywords=gaming&searchDescriptions=true&searchProductTags=true
```

### 3. Category Filter

Find all products in specific categories:

```bash
GET /api/admin/Product/search?categoryIds=cat123,cat456,cat789
```

### 4. Price Range Search

Find products between 500 and 2000:

```bash
GET /api/admin/Product/search?priceMin=500&priceMax=2000
```

### 5. Combined Search

Gaming laptops in electronics category, price 800-1500, sorted by price:

```bash
GET /api/admin/Product/search
  ?keywords=gaming laptop
  &searchDescriptions=true
  &categoryIds=electronics123
  &priceMin=800
  &priceMax=1500
  &orderBy=2
  &pageSize=20
```

### 6. Brand-Specific Search

All products from a specific brand, sorted by best sellers:

```bash
GET /api/admin/Product/search?brandId=brand123&orderBy=4
```

### 7. New Products on Homepage

Find recently added products marked for homepage:

```bash
GET /api/admin/Product/search?markedAsNewOnly=true&showOnHomePage=true&orderBy=3
```

### 8. Vendor Products with Stock

Products from specific vendor with price filter:

```bash
GET /api/admin/Product/search?vendorId=vendor456&priceMin=50&orderBy=1
```

### 9. Featured Products

All featured products sorted by price:

```bash
GET /api/admin/Product/search?featuredProducts=true&orderBy=2
```

### 10. SKU Search

Find product by exact SKU:

```bash
GET /api/admin/Product/search?keywords=SKU-12345&searchSku=true&searchDescriptions=false
```

## Use Cases

### E-commerce Search Bar
```bash
# User searches for "red shoes"
GET /api/admin/Product/search
  ?keywords=red shoes
  &searchDescriptions=true
  &publishedOnly=true
  &orderBy=4
```

### Price Comparison
```bash
# Find all laptops under $1000, sorted by price
GET /api/admin/Product/search
  ?keywords=laptop
  &categoryIds=electronics123
  &priceMax=1000
  &orderBy=2
```

### Inventory Management
```bash
# Find all products from vendor with low stock
GET /api/admin/Product/search
  ?vendorId=vendor123
  &publishedOnly=false
```

### Marketing Campaign
```bash
# Get all featured bestsellers for promotion
GET /api/admin/Product/search
  ?featuredProducts=true
  &orderBy=4
  &pageSize=10
```

## MongoDB Behind the Scenes

The search uses MongoDB's powerful query capabilities:

1. **Text Search**: When `searchDescriptions=true`, MongoDB performs full-text search across indexed text fields
2. **Range Queries**: Price filters use efficient range queries with indexes
3. **Compound Filters**: Multiple filters are combined using MongoDB's `$and` operator
4. **Sorting**: Leverages MongoDB indexes for efficient sorting
5. **Pagination**: Uses `skip` and `limit` for efficient pagination

## Performance Tips

1. **Use Specific Filters**: More specific queries are faster
   - ✅ Good: `categoryIds=cat123&priceMin=100`
   - ❌ Slow: `keywords=*&searchDescriptions=true`

2. **Limit Page Size**: Smaller page sizes return faster
   - ✅ Recommended: `pageSize=20`
   - ❌ Avoid: `pageSize=1000`

3. **Enable Text Search Selectively**: Only when needed
   - ✅ Use `searchDescriptions=true` for broad searches
   - ❌ Don't enable for SKU/ID lookups

4. **Use Pagination**: Always paginate large result sets
   - ✅ Use `pageIndex` to navigate results
   - ❌ Don't fetch all results at once

## Error Responses

### 403 Forbidden
```json
{
  "error": "Insufficient permissions"
}
```

User lacks `ManageProducts` permission.

### 400 Bad Request
```json
{
  "error": "Invalid parameter",
  "details": "pageSize must be between 1 and 100"
}
```

Invalid parameter value provided.

## Rate Limiting

Consider implementing rate limiting for production:
- Recommended: 100 requests per minute per user
- Heavy searches (with `searchDescriptions=true`): 20 requests per minute

## Comparison with Graph Databases

While Graph DBs (Neo4j, ArangoDB) excel at relationship queries ("customers who bought X also bought Y"), MongoDB is excellent for:

✅ **Full-text search** - Built-in text indexing
✅ **Range queries** - Fast price/date filtering
✅ **Aggregation** - Complex data transformations
✅ **Flexible schema** - Easy to add new fields
✅ **Horizontal scaling** - Sharding support

For product recommendations and complex relationships, consider:
- MongoDB's **Aggregation Pipeline** with `$lookup` for joins
- **MongoDB Atlas Search** for advanced features (autocomplete, fuzzy matching)
- External recommendation engine (if needed) using MongoDB as data source

## Advanced: MongoDB Atlas Search

If using MongoDB Atlas, you can enable Atlas Search for:
- **Fuzzy matching**: "laptp" finds "laptop"
- **Synonyms**: "phone" also finds "mobile"
- **Autocomplete**: Real-time suggestions
- **Faceted search**: "Show me counts per category"
- **Highlighting**: Show which part matched

Contact your MongoDB administrator to enable Atlas Search on the `Product` collection.

## Next Steps

1. Test the search endpoint with various parameter combinations
2. Monitor query performance with MongoDB profiling
3. Add custom indexes for frequently filtered fields
4. Consider MongoDB Atlas Search for advanced features
5. Implement caching for popular searches

## Support

For issues or questions:
- Check MongoDB query logs
- Review GrandNode documentation
- Contact your system administrator

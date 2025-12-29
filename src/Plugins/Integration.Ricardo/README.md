# ricardo.ch Integration for GrandNode

Integrate your GrandNode e-commerce store with [ricardo.ch](https://www.ricardo.ch), Switzerland's leading online marketplace.

## Features (Phase 1 - MVP)

✅ **Product Publishing**
- Publish GrandNode products to ricardo.ch
- Automatic price calculation with configurable markup
- Product image synchronization (up to 10 images)
- Category mapping

✅ **Stock Management**
- Manual or automatic stock synchronization
- Real-time stock updates
- Low stock alerts

✅ **Configuration**
- Easy-to-use admin interface
- Sandbox/Production environment support
- Detailed logging for troubleshooting

## Installation

### 1. Build the Plugin

```bash
cd src/Plugins/Integration.Ricardo
dotnet build -c Release
```

The plugin DLL will be copied to `src/Web/Grand.Web/Plugins/Integration.Ricardo/`

### 2. Install in GrandNode

1. Restart your GrandNode application
2. Go to **Admin Panel → Configuration → Plugins**
3. Find "ricardo.ch Integration" and click **Install**
4. Click **Configure** to set up your credentials

## Configuration

### Step 1: Get ricardo.ch API Credentials

1. **Create Professional Seller Account**
   - Go to [ricardo.ch](https://www.ricardo.ch)
   - Sign up for a professional seller account

2. **Request API Access**
   - Contact ricardo.ch support
   - Request API access for your account
   - Fill out the [API application form](https://help.ricardo.ch/hc/de/articles/115002970529)

3. **Receive Credentials**
   You'll receive:
   - Partner ID
   - Partner Key
   - Sandbox credentials for testing

### Step 2: Configure Plugin

1. Go to **Admin → Configuration → Plugins → ricardo.ch Integration → Configure**

2. **API Credentials**
   - Enable "Use Sandbox" for testing
   - Enter your Partner ID
   - Enter your Partner Key
   - Enter your ricardo account username
   - Enter your ricardo account password
   - Click "Test Connection" to verify

3. **Publishing Settings**
   - Set default ricardo category ID
   - Set article duration (1-10 days)
   - Configure price markup percentage

4. **Stock Synchronization** (Optional)
   - Enable automatic stock sync
   - Set sync interval (15-1440 minutes)

5. Click **Save Configuration**

## Usage

### Publishing a Product to ricardo.ch

**Option 1: Via Admin Interface** (Coming in Phase 2)
1. Go to **Catalog → Products**
2. Edit a product
3. Scroll to "ricardo.ch" section
4. Click "Publish to ricardo.ch"

**Option 2: Via API** (Current MVP)

Use the `RicardoProductService` programmatically:

```csharp
// Inject the service
private readonly RicardoProductService _ricardoProductService;

// Publish a product
var result = await _ricardoProductService.PublishProductAsync(
    productId: "your-product-id",
    categoryId: 12345 // Optional, uses default if not provided
);

if (result.Success)
{
    // Product published successfully
    var ricardoArticleId = result.RicardoArticleId;
    var ricardoArticleNr = result.RicardoArticleNr;
}
else
{
    // Handle error
    var errorMessage = result.ErrorMessage;
}
```

### Updating Stock

```csharp
var success = await _ricardoProductService.UpdateStockAsync(
    ricardoArticleId: 123456789,
    newQuantity: 50
);
```

### Closing an Article

```csharp
var success = await _ricardoProductService.CloseArticleAsync(
    ricardoArticleId: 123456789
);
```

## ricardo.ch Category IDs

You need to know the category IDs from ricardo.ch. Common categories:

- **Electronics**: 100
- **Fashion**: 200
- **Home & Garden**: 300
- **Sports**: 400

For complete list, use ricardo.ch API `SystemService.GetCategories()` or check ricardo.ch website.

## Product Requirements

For a product to be published to ricardo.ch, it must have:

✅ Product name (max 40 characters for title)
✅ Price > 0
✅ Stock quantity > 0
✅ At least one image (recommended)
✅ Description (recommended)

## Troubleshooting

### "Authentication failed"
- Check your Partner ID and Partner Key
- Verify your account username and password
- Make sure you're using the correct environment (Sandbox vs Production)

### "Invalid category ID"
- Verify the category ID exists on ricardo.ch
- Check if category accepts your product type

### "Product validation failed"
- Ensure product has name, price > 0, and stock > 0
- Check product images are accessible

### Enable Detailed Logging
1. Go to plugin configuration
2. Enable "Detailed Logging"
3. Check GrandNode logs for detailed error messages

## Roadmap

### Phase 2 (Planned)
- Bulk product publishing
- Order import from ricardo.ch
- Bidirectional stock sync
- Product performance analytics

### Phase 3 (Future)
- Auction support
- Custom templates
- Multi-account management
- Advanced reporting

## API Documentation

- [ricardo.ch API Documentation](https://help.ricardo.ch/hc/de/articles/115002970529-Technical-documentation)
- [Quick Start Guide](https://help.ricardo.ch/hc/fr/articles/115002970729-How-to-start-quickly)

## Support

For plugin issues:
- Check the [Troubleshooting](#troubleshooting) section
- Enable detailed logging
- Contact GrandNode support

For ricardo.ch API issues:
- Contact ricardo.ch support
- Check [ricardo.ch Help Center](https://help.ricardo.ch)

## License

This plugin is part of GrandNode and follows the same license.

## Version History

### 1.0.0 (Initial Release)
- Product publishing to ricardo.ch
- Stock synchronization
- Sandbox and production support
- Admin configuration interface

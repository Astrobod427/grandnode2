# Development Documentation - ricardo.ch Integration Plugin

This document provides detailed technical information about the development process, architectural decisions, and troubleshooting for the ricardo.ch Integration plugin.

## Table of Contents
1. [Project Overview](#project-overview)
2. [Development Timeline](#development-timeline)
3. [Architecture & Design Decisions](#architecture--design-decisions)
4. [Technical Implementation](#technical-implementation)
5. [Problems Encountered & Solutions](#problems-encountered--solutions)
6. [File Structure](#file-structure)
7. [Testing Guide](#testing-guide)
8. [Future Development](#future-development)

---

## Project Overview

**Goal**: Create a GrandNode plugin to integrate with ricardo.ch marketplace, enabling merchants to publish products from their GrandNode store to Switzerland's leading online marketplace.

**Development Approach**: Incremental development with Phase 1 MVP first, then extend with additional features in Phase 2 and Phase 3.

**Target Platform**: GrandNode 2.3+ on .NET 9.0

---

## Development Timeline

### Initial Planning (2025-12-29)
- **Decision Point**: Start with basic functionality (Option 1) vs. build everything at once
- **Chosen Approach**: Phase 1 MVP with proper architecture for future extension
- **Rationale**: Faster time to market, ability to test with real API, easier to iterate

### Phase 1 Development (2025-12-29)
1. Plugin structure and project setup
2. API client implementation
3. Settings and configuration
4. Product publishing service
5. Admin UI
6. Documentation
7. Build and testing

**Status**: ✅ Phase 1 MVP Complete

---

## Architecture & Design Decisions

### 1. Plugin Architecture

**Decision**: Follow standard GrandNode plugin pattern
- Self-contained in `src/Plugins/Integration.Ricardo/`
- Uses `Microsoft.NET.Sdk.Razor` for view support
- References GrandNode core projects but marked as `<Private>false</Private>`
- Output to `src/Web/Grand.Web/Plugins/Integration.Ricardo/`

**Rationale**: Ensures compatibility with GrandNode's plugin system and hot-reload capabilities.

### 2. JSON Serialization

**Decision**: Use `System.Text.Json` instead of `Newtonsoft.Json`
- Initial attempt used Newtonsoft.Json but encountered package management issues
- Switched to System.Text.Json which is built into .NET

**Rationale**:
- System.Text.Json is the modern .NET standard
- No external dependencies
- Better performance
- Avoids central package management conflicts

### 3. API Client Design

**Decision**: Separate `RicardoApiClient` and `RicardoProductService`
- `RicardoApiClient`: Low-level HTTP communication, authentication, JSON-RPC protocol
- `RicardoProductService`: Business logic, product validation, data transformation

**Rationale**:
- Separation of concerns
- Easier to test
- Reusable API client for future features
- Business logic isolated from communication layer

### 4. Authentication Strategy

**Decision**: In-memory token management with automatic renewal
- Tokens stored in private field `_tokenCredential`
- Automatic re-authentication when token expires
- Token expiration checked before each request

**Rationale**:
- Security: Tokens not persisted to disk
- Simplicity: No need for token storage/retrieval logic
- Reliability: Automatic handling of expired tokens

### 5. Settings Storage

**Decision**: Use GrandNode's `ISettings` system
- Settings stored in MongoDB via GrandNode's settings service
- Passwords stored (not ideal but acceptable for Phase 1)

**Future Improvement**: Consider encryption for sensitive credentials

### 6. Admin UI

**Decision**: ASP.NET Core MVC Areas pattern
- Located in `Areas/Admin/`
- Uses GrandNode's admin layout and authorization
- AJAX for connection testing

**Rationale**: Consistent with GrandNode's admin panel architecture

---

## Technical Implementation

### Core Components

#### 1. RicardoApiClient.cs
```
Location: Services/RicardoApiClient.cs
Purpose: HTTP client for ricardo.ch API communication
```

**Key Features**:
- JWT authentication with `TokenCredentialLogin`
- JSON-RPC 2.0 request formatting
- Automatic token renewal via `IsTokenExpired()`
- Generic `PostAsync<TResponse>()` method for all API calls
- Error handling and logging

**API Methods**:
- `AuthenticateAsync()` - Get JWT token
- `InsertArticleAsync()` - Publish product
- `UpdateArticleQuantityAsync()` - Update stock
- `CloseArticleAsync()` - Remove listing

**JSON-RPC Protocol**:
```json
{
  "jsonrpc": "2.0",
  "method": "InsertArticle",
  "params": [{ ...payload... }],
  "id": "unique-guid"
}
```

#### 2. RicardoProductService.cs
```
Location: Services/RicardoProductService.cs
Purpose: Business logic for product publishing
```

**Key Features**:
- Product validation (name, price > 0, stock > 0)
- Price markup calculation with decimal precision
- HTML description cleanup using regex
- Image URL preparation (up to 10 images)
- Title truncation (40 chars max)
- Description truncation (8000 chars max)

**Critical Fix Applied**:
```csharp
// Original (type mismatch error):
var finalPrice = product.Price * (1 + (_settings.PriceMarkupPercentage / 100));

// Fixed (explicit decimal cast):
var finalPrice = (decimal)product.Price * (1 + (_settings.PriceMarkupPercentage / 100));
```

**Why**: `product.Price` is `double`, `_settings.PriceMarkupPercentage` is `decimal`. C# doesn't allow implicit conversion between these types.

#### 3. RicardoSettings.cs
```
Location: Models/RicardoSettings.cs
Purpose: Configuration model
```

**Properties**:
- `UseSandbox`: Environment selection
- `PartnerId`, `PartnerKey`: API credentials
- `AccountUsername`, `AccountPassword`: ricardo.ch account
- `EnableStockSync`: Future feature flag
- `StockSyncIntervalMinutes`: Future sync interval
- `PriceMarkupPercentage`: Price adjustment (decimal)
- `DefaultCategoryId`: ricardo.ch category
- `DefaultArticleDurationDays`: Listing duration

#### 4. RicardoApiModels.cs
```
Location: Models/RicardoApiModels.cs
Purpose: DTOs for API communication
```

**Request Models**:
- `InsertArticleRequest`: Complete article data
- `UpdateArticleQuantityRequest`: Stock update
- `CloseArticleRequest`: Remove listing

**Response Models**:
- `RicardoTokenCredential`: Authentication response
- `InsertArticleResponse`: New article details
- `UpdateArticleQuantityResponse`: Update confirmation
- `CloseArticleResponse`: Removal confirmation

**Supporting Models**:
- `PictureInformation`: Image data
- `PaymentConditionIds`: Payment methods
- `DeliveryConditionIds`: Shipping options
- `WarrantyConditionIds`: Warranty terms

#### 5. Admin Controller & View
```
Controller: Areas/Admin/Controllers/RicardoController.cs
View: Areas/Admin/Views/Ricardo/Configure.cshtml
```

**Features**:
- Configuration form with sections:
  - API Credentials
  - Publishing Settings
  - Stock Synchronization (future)
- Connection testing via AJAX
- Form validation
- Success/error messages

#### 6. Dependency Injection
```
Location: Infrastructure/StartupApplication.cs
```

**Registrations**:
```csharp
services.AddHttpClient<RicardoApiClient>();
services.AddScoped<RicardoApiClient>();
services.AddScoped<RicardoProductService>();
```

**Note**: `RicardoSettings` needs to be retrieved via `ISettingService`, not injected directly.

---

## Problems Encountered & Solutions

### Problem 1: NuGet Package Version Management

**Error**:
```
NU1008: Projects that use central package version management should not define
the version on the PackageReference items but on the PackageVersion items: Newtonsoft.Json
```

**Location**: Integration.Ricardo.csproj

**Root Cause**: GrandNode uses centralized package version management via `Directory.Packages.props`. Individual projects should not specify package versions.

**Solution**: Removed version specification from PackageReference
```xml
<!-- Before -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- After -->
<PackageReference Include="Newtonsoft.Json" />
<!-- Version managed centrally in Directory.Packages.props -->
```

### Problem 2: Newtonsoft.Json Not Available

**Error**:
```
CS0246: The type or namespace name 'JsonPropertyAttribute' could not be found
```

**Location**: Multiple files using `[JsonProperty]` attributes

**Root Cause**: Even after fixing package reference, Newtonsoft.Json namespace was not available in the plugin context.

**Solution**: Complete migration to System.Text.Json
```csharp
// Before
using Newtonsoft.Json;
[JsonProperty("result")]
JsonConvert.SerializeObject(obj)
JsonConvert.DeserializeObject<T>(json)

// After
using System.Text.Json;
using System.Text.Json.Serialization;
[JsonPropertyName("result")]
JsonSerializer.Serialize(obj)
JsonSerializer.Deserialize<T>(json)
```

**Files Modified**: RicardoApiClient.cs

### Problem 3: Type Mismatch in Price Calculation

**Error**:
```
CS0019: Operator '*' cannot be applied to operands of type 'double' and 'decimal'
```

**Location**: RicardoProductService.cs line 60

**Root Cause**:
- `product.Price` is type `double` (from GrandNode Product entity)
- `_settings.PriceMarkupPercentage` is type `decimal` (from RicardoSettings)
- C# doesn't allow implicit conversion between double and decimal

**Solution**: Explicit cast to decimal
```csharp
var finalPrice = (decimal)product.Price * (1 + (_settings.PriceMarkupPercentage / 100));
```

**Why Decimal**:
- More appropriate for monetary calculations
- `InsertArticleRequest.StartPrice` expects decimal
- Avoids floating-point precision issues

### Problem 4: Build Permission Errors

**Error**:
```
Access to the path '.../obj/475dbe9a-bd95-4e78-92dc-4f89215056f4.tmp' is denied
```

**Root Cause**: Build artifacts created by previous build (possibly in Docker) had different ownership/permissions.

**Solution**: Clean build directories with sudo
```bash
sudo rm -rf src/Plugins/Integration.Ricardo/obj
sudo rm -rf src/Plugins/Integration.Ricardo/bin
sudo rm -rf src/Web/Grand.Web/Plugins/Integration.Ricardo
dotnet build src/Plugins/Integration.Ricardo/Integration.Ricardo.csproj -c Debug
```

**Prevention**: Run builds consistently in same environment (either host or Docker, not mixed)

---

## File Structure

```
src/Plugins/Integration.Ricardo/
├── Areas/
│   └── Admin/
│       ├── Controllers/
│       │   └── RicardoController.cs          # Admin configuration controller
│       └── Views/
│           ├── _ViewImports.cshtml            # Razor imports
│           └── Ricardo/
│               └── Configure.cshtml            # Configuration UI
├── Infrastructure/
│   └── StartupApplication.cs                  # DI registration
├── Models/
│   ├── RicardoApiModels.cs                    # API DTOs
│   └── RicardoSettings.cs                     # Plugin settings
├── Services/
│   ├── RicardoApiClient.cs                    # HTTP client & auth
│   └── RicardoProductService.cs               # Business logic
├── CHANGELOG.md                                # Version history
├── DEVELOPMENT.md                              # This file
├── Integration.Ricardo.csproj                  # Project file
├── plugin.json                                 # Plugin metadata
├── README.md                                   # User documentation
└── RicardoDefaults.cs                         # Constants

Output (after build):
src/Web/Grand.Web/Plugins/Integration.Ricardo/
├── Integration.Ricardo.dll                     # Plugin assembly
├── Integration.Ricardo.deps.json              # Dependencies
├── Integration.Ricardo.pdb                    # Debug symbols
└── plugin.json                                # Metadata
```

---

## Testing Guide

### Prerequisites
1. GrandNode 2.3+ running
2. ricardo.ch professional seller account
3. ricardo.ch API credentials

### Installation Testing

1. **Build the plugin**:
   ```bash
   dotnet build src/Plugins/Integration.Ricardo/Integration.Ricardo.csproj -c Debug
   ```

2. **Restart GrandNode**

3. **Install via Admin Panel**:
   - Navigate to Admin → Configuration → Plugins
   - Find "ricardo.ch Integration"
   - Click "Install"
   - Verify no errors in logs

### Configuration Testing

1. **Access Configuration**:
   - Click "Configure" on the plugin
   - Verify all form fields are present

2. **Test Connection**:
   - Enter sandbox credentials
   - Enable "Use Sandbox"
   - Click "Test Connection"
   - Should see success message if credentials are valid

3. **Save Settings**:
   - Fill all required fields
   - Click "Save Configuration"
   - Verify success message

### API Testing

**Note**: Currently Phase 1 requires programmatic usage. Admin UI integration planned for Phase 2.

1. **Test Authentication**:
   ```csharp
   var apiClient = serviceProvider.GetService<RicardoApiClient>();
   var authenticated = await apiClient.AuthenticateAsync();
   // Should return true with valid credentials
   ```

2. **Test Product Publishing**:
   ```csharp
   var productService = serviceProvider.GetService<RicardoProductService>();
   var result = await productService.PublishProductAsync(
       productId: "existing-product-id",
       categoryId: 100 // Valid ricardo.ch category
   );

   // Verify:
   // - result.Success == true
   // - result.RicardoArticleId > 0
   // - Check ricardo.ch website for listing
   ```

3. **Test Stock Update**:
   ```csharp
   var success = await productService.UpdateStockAsync(
       ricardoArticleId: 123456789,
       newQuantity: 25
   );
   // Verify on ricardo.ch website
   ```

4. **Test Article Closure**:
   ```csharp
   var success = await productService.CloseArticleAsync(
       ricardoArticleId: 123456789
   );
   // Verify article removed from ricardo.ch
   ```

### Logging

Enable detailed logging in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Integration.Ricardo": "Debug"
    }
  }
}
```

Check logs for:
- Authentication success/failure
- API request/response details
- Error messages with stack traces

---

## Future Development

### Phase 2 Tasks

1. **Bulk Product Publishing**
   - UI for selecting multiple products
   - Background job processing
   - Progress tracking
   - Error handling per product

2. **Order Import**
   - Webhook listener for ricardo.ch orders
   - Order mapping to GrandNode
   - Customer creation/matching
   - Inventory deduction
   - Order status sync

3. **Bidirectional Stock Sync**
   - Scheduled background job
   - GrandNode → ricardo.ch sync
   - ricardo.ch → GrandNode sync
   - Conflict resolution
   - Sync logs and reports

4. **Product Edit Integration**
   - Add "ricardo.ch" tab to product edit page
   - Show publication status
   - Quick publish button
   - View ricardo.ch listing
   - Sync stock button

5. **Analytics**
   - Track views, favorites, sales
   - Revenue reporting
   - Performance metrics per product
   - Category performance

### Phase 3 Tasks

1. **Auction Support**
   - Auction-specific settings
   - Bid management
   - Reserve price configuration
   - Auction duration settings

2. **Custom Templates**
   - HTML template editor
   - Placeholder system
   - Template preview
   - Multiple templates per category

3. **Multi-Account Management**
   - Multiple ricardo.ch accounts
   - Account selection per product
   - Separate settings per account
   - Account performance comparison

4. **Advanced Reporting**
   - Sales trends
   - Category analysis
   - Competitor pricing
   - Export to Excel/PDF

### Technical Improvements

1. **Security**
   - Encrypt credentials in database
   - Use ASP.NET Data Protection API
   - Token refresh strategy
   - API rate limiting

2. **Performance**
   - Cache category lists
   - Batch API calls where possible
   - Async everywhere
   - Connection pooling

3. **Testing**
   - Unit tests for all services
   - Integration tests with mock API
   - End-to-end tests
   - Performance tests

4. **Monitoring**
   - Health checks
   - API call metrics
   - Error rate monitoring
   - Alert system

---

## Development Notes

### ricardo.ch API Specifics

**Environment URLs**:
- Production: `https://ws.ricardo.ch/ricardoapi/`
- Sandbox: `https://ws.test.ricardo.ch/ricardoapi/`

**Services**:
- SecurityService.json - Authentication
- ArticlesService.json - Article management
- SystemService.json - Categories, settings
- SearchService.json - Search functionality

**Authentication Flow**:
1. Call `TokenCredentialLogin` with Partner credentials + Account credentials
2. Receive JWT token in `TokenCredential` field
3. Include token in `Token-Credential` header for subsequent requests
4. Token expires after period specified in `TokenExpirationDate`
5. Re-authenticate when token expires

**JSON-RPC Format**:
- Always use `jsonrpc: "2.0"`
- Method name must match API method exactly
- Params must be array (even for single object)
- Include unique `id` for request tracking
- Check response for `error` object before accessing `result`

### GrandNode Plugin Development

**Hot Reload**: Changes to plugin code require:
1. Rebuild plugin project
2. Restart GrandNode application

**Settings System**:
- Use `ISettingService.GetSettingByKey<T>(key, defaultValue)`
- Settings stored in MongoDB `Setting` collection
- Key format: `SystemName` from plugin.json

**Admin Authorization**:
- Use `[PermissionAuthorize(PermissionSystemName.Plugins)]` on controllers
- Inherits from `BaseAdminPluginController`

**Dependency Injection**:
- Register services in `StartupApplication.ConfigureServices()`
- Use standard .NET DI lifetime scopes (Scoped, Singleton, Transient)

---

## Contact & Support

**Development Team**: GrandNode Team
**Plugin Version**: 1.0.0
**GrandNode Version**: 2.3+
**Last Updated**: 2025-12-29

For issues or questions:
1. Check README.md troubleshooting section
2. Review this document for technical details
3. Check GrandNode logs with detailed logging enabled
4. Contact ricardo.ch support for API-specific issues

---

*This documentation is maintained alongside the plugin development. All changes should be documented here for future reference.*

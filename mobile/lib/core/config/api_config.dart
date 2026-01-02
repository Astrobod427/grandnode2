class ApiConfig {
  static const String baseUrl = 'https://labaraque.shop';
  static const String apiPath = '/api';

  // Auth endpoints
  static const String tokenEndpoint = '/Api/Token/Create';
  static const String registerEndpoint = '/api/mobile/Account/register';

  // Mobile API endpoints (require JWT auth)
  static const String cartEndpoint = '/api/mobile/ShoppingCart';
  static const String wishlistEndpoint = '/api/mobile/Wishlist';
  static const String checkoutEndpoint = '/api/mobile/Checkout';

  // Public catalog endpoints (no auth required)
  static const String catalogProductsEndpoint = '/api/mobile/Catalog/products';
  static const String catalogCategoriesEndpoint = '/api/mobile/Catalog/categories';
  static const String catalogFeaturedEndpoint = '/api/mobile/Catalog/featured';
  static const String catalogSearchEndpoint = '/api/mobile/Catalog/search';

  // Store settings (theme, etc.)
  static const String storeSettingsEndpoint = '/api/mobile/Store/settings';

  // Timeouts
  static const Duration connectionTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
}

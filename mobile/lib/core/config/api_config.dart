class ApiConfig {
  static const String baseUrl = 'https://labaraque.shop';
  static const String apiPath = '/api';

  // Auth endpoints
  static const String tokenEndpoint = '/Api/Token/Create';

  // Mobile API endpoints
  static const String cartEndpoint = '/api/mobile/ShoppingCart';
  static const String wishlistEndpoint = '/api/mobile/Wishlist';
  static const String checkoutEndpoint = '/api/mobile/Checkout';

  // Public endpoints (no auth required for browsing)
  static const String productsEndpoint = '/api/extended/products';
  static const String categoriesEndpoint = '/api/extended/categories';

  // Timeouts
  static const Duration connectionTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
}

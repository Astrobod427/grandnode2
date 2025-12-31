class AppConfig {
  static const String appName = 'La Baraque Shop';
  static const String appVersion = '1.0.0';

  // Currency
  static const String defaultCurrency = 'CHF';
  static const String currencySymbol = 'CHF';

  // Pagination
  static const int pageSize = 20;

  // Cache
  static const Duration cacheExpiry = Duration(hours: 1);
}

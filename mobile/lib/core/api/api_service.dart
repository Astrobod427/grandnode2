import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';

class ApiService {
  String? _token;

  String get baseUrl => ApiConfig.baseUrl;

  Future<void> setToken(String token) async {
    _token = token;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('jwt_token', token);
  }

  Future<String?> getToken() async {
    if (_token != null) return _token;
    final prefs = await SharedPreferences.getInstance();
    _token = prefs.getString('jwt_token');
    return _token;
  }

  Future<void> clearToken() async {
    _token = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('jwt_token');
  }

  Map<String, String> _headers({bool requiresAuth = false}) {
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
    if (requiresAuth && _token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }
    return headers;
  }

  Future<dynamic> get(String endpoint, {bool requiresAuth = false, Map<String, String>? queryParams}) async {
    var uri = Uri.parse('$baseUrl$endpoint');
    if (queryParams != null && queryParams.isNotEmpty) {
      uri = uri.replace(queryParameters: queryParams);
    }
    final response = await http.get(uri, headers: _headers(requiresAuth: requiresAuth))
        .timeout(ApiConfig.connectionTimeout);

    if (response.statusCode == 200) {
      return json.decode(response.body);
    } else {
      throw ApiException(response.statusCode, response.body);
    }
  }

  Future<Map<String, dynamic>> post(String endpoint, Map<String, dynamic> body, {bool requiresAuth = false}) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final response = await http.post(
      url,
      headers: _headers(requiresAuth: requiresAuth),
      body: json.encode(body),
    ).timeout(ApiConfig.connectionTimeout);

    if (response.statusCode == 200 || response.statusCode == 201) {
      return json.decode(response.body);
    } else {
      throw ApiException(response.statusCode, response.body);
    }
  }

  Future<Map<String, dynamic>> put(String endpoint, Map<String, dynamic> body, {bool requiresAuth = true}) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final response = await http.put(
      url,
      headers: _headers(requiresAuth: requiresAuth),
      body: json.encode(body),
    ).timeout(ApiConfig.connectionTimeout);

    if (response.statusCode == 200) {
      return json.decode(response.body);
    } else {
      throw ApiException(response.statusCode, response.body);
    }
  }

  Future<void> delete(String endpoint, {bool requiresAuth = true}) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final response = await http.delete(url, headers: _headers(requiresAuth: requiresAuth))
        .timeout(ApiConfig.connectionTimeout);

    if (response.statusCode != 200 && response.statusCode != 204) {
      throw ApiException(response.statusCode, response.body);
    }
  }

  // Auth methods
  Future<String> login(String email, String password) async {
    final base64Password = base64Encode(utf8.encode(password));
    final response = await post(ApiConfig.loginEndpoint, {
      'email': email,
      'password': base64Password,
    }, requiresAuth: false);

    if (response['token'] != null) {
      final token = response['token'] as String;
      await setToken(token);
      return token;
    } else {
      throw ApiException(400, 'No token in response');
    }
  }

  Future<Map<String, dynamic>> register({
    required String email,
    required String password,
    String? firstName,
    String? lastName,
  }) async {
    final base64Password = base64Encode(utf8.encode(password));
    return post(ApiConfig.registerEndpoint, {
      'email': email,
      'password': base64Password,
      'firstName': firstName ?? '',
      'lastName': lastName ?? '',
    });
  }

  // Catalog methods (public, no auth)
  Future<Map<String, dynamic>> getFeaturedProducts({int limit = 10}) async {
    final result = await get('${ApiConfig.catalogFeaturedEndpoint}?limit=$limit');
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getCategories() async {
    final result = await get(ApiConfig.catalogCategoriesEndpoint);
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getProductsByCategory(String categoryId, {int page = 0, int pageSize = 20}) async {
    final result = await get('${ApiConfig.catalogCategoriesEndpoint}/$categoryId/products?pageIndex=$page&pageSize=$pageSize');
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> searchProducts(String query, {int page = 0, int pageSize = 20}) async {
    final result = await get('${ApiConfig.catalogSearchEndpoint}?q=${Uri.encodeComponent(query)}&pageIndex=$page&pageSize=$pageSize');
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getProductDetails(String productId) async {
    final result = await get('${ApiConfig.catalogProductsEndpoint}/$productId');
    return result as Map<String, dynamic>;
  }

  // Cart methods (requires auth)
  Future<Map<String, dynamic>> getCart() async {
    final result = await get(ApiConfig.cartEndpoint, requiresAuth: true);
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> addToCart(String productId, int quantity) async {
    return post(ApiConfig.cartEndpoint, {
      'productId': productId,
      'quantity': quantity,
    }, requiresAuth: true);
  }

  // Checkout methods (requires auth)
  Future<Map<String, dynamic>> getCheckoutSummary() async {
    final result = await get(ApiConfig.checkoutEndpoint, requiresAuth: true);
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getAddresses() async {
    final result = await get('${ApiConfig.checkoutEndpoint}/addresses', requiresAuth: true);
    return result as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> setBillingAddress(String addressId) async {
    return post('${ApiConfig.checkoutEndpoint}/billing-address/$addressId', {}, requiresAuth: true);
  }

  Future<Map<String, dynamic>> setShippingAddress(String addressId) async {
    return post('${ApiConfig.checkoutEndpoint}/shipping-address/$addressId', {}, requiresAuth: true);
  }

  Future<Map<String, dynamic>> placeOrder({
    required String paymentMethodSystemName,
    String? shippingOptionName,
    String? shippingRateProviderSystemName,
    bool useLoyaltyPoints = false,
  }) async {
    return post(ApiConfig.checkoutEndpoint, {
      'paymentMethodSystemName': paymentMethodSystemName,
      if (shippingOptionName != null) 'shippingOptionName': shippingOptionName,
      if (shippingRateProviderSystemName != null)
        'shippingRateProviderSystemName': shippingRateProviderSystemName,
      'useLoyaltyPoints': useLoyaltyPoints,
    }, requiresAuth: true);
  }
}

class ApiException implements Exception {
  final int statusCode;
  final String message;

  ApiException(this.statusCode, this.message);

  @override
  String toString() => 'ApiException: $statusCode - $message';
}

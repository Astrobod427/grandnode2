import 'package:flutter/foundation.dart';
import '../core/api/api_service.dart';
import '../core/config/api_config.dart';
import '../models/product.dart';
import '../models/category.dart' as models;

class ProductProvider extends ChangeNotifier {
  final ApiService _apiService;

  List<Product> _products = [];
  List<Product> _featuredProducts = [];
  List<models.Category> _categories = [];
  Product? _selectedProduct;
  bool _isLoading = false;
  String? _errorMessage;
  int _totalCount = 0;
  int _currentPage = 0;
  bool _hasMore = true;

  ProductProvider({ApiService? apiService})
      : _apiService = apiService ?? ApiService();

  List<Product> get products => _products;
  List<Product> get featuredProducts => _featuredProducts;
  List<models.Category> get categories => _categories;
  Product? get selectedProduct => _selectedProduct;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  int get totalCount => _totalCount;
  bool get hasMore => _hasMore;

  Future<void> loadCategories() async {
    try {
      final response = await _apiService.get(ApiConfig.catalogCategoriesEndpoint);

      if (response != null && response is List) {
        _categories = response
            .map<models.Category>((json) => models.Category.fromJson(json))
            .toList();
        notifyListeners();
      }
    } catch (e) {
      debugPrint('Error loading categories: $e');
    }
  }

  Future<void> loadProducts({
    String? categoryId,
    String? keywords,
    double? priceMin,
    double? priceMax,
    bool featuredOnly = false,
    int orderBy = 0,
    bool refresh = false,
  }) async {
    if (refresh) {
      _currentPage = 0;
      _products = [];
      _hasMore = true;
    }

    if (!_hasMore || _isLoading) return;

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final queryParams = <String, String>{
        'pageIndex': _currentPage.toString(),
        'pageSize': '20',
        'orderBy': orderBy.toString(),
      };

      if (categoryId != null) queryParams['categoryId'] = categoryId;
      if (keywords != null && keywords.isNotEmpty) queryParams['keywords'] = keywords;
      if (priceMin != null) queryParams['priceMin'] = priceMin.toString();
      if (priceMax != null) queryParams['priceMax'] = priceMax.toString();
      if (featuredOnly) queryParams['featuredOnly'] = 'true';

      final response = await _apiService.get(
        ApiConfig.catalogProductsEndpoint,
        queryParams: queryParams,
      );

      if (response != null) {
        _totalCount = response['totalCount'] ?? 0;
        final items = response['items'] as List? ?? [];
        final newProducts = items.map<Product>((json) => Product.fromJson(json)).toList();

        if (refresh) {
          _products = newProducts;
        } else {
          _products.addAll(newProducts);
        }

        _currentPage++;
        _hasMore = _products.length < _totalCount;
      }
    } on ApiException catch (e) {
      _errorMessage = e.message;
    } catch (e) {
      _errorMessage = 'Erreur de chargement des produits';
      debugPrint('Error loading products: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> loadFeaturedProducts() async {
    try {
      final response = await _apiService.get(
        ApiConfig.catalogFeaturedEndpoint,
        queryParams: {'limit': '10'},
      );

      if (response != null && response is List) {
        _featuredProducts = response
            .map<Product>((json) => Product.fromJson(json))
            .toList();
        notifyListeners();
      }
    } catch (e) {
      debugPrint('Error loading featured products: $e');
    }
  }

  Future<void> loadProductDetails(String productId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.get(
        '${ApiConfig.catalogProductsEndpoint}/$productId',
      );

      if (response != null) {
        _selectedProduct = Product.fromJson(response);
      }
    } on ApiException catch (e) {
      _errorMessage = e.message;
    } catch (e) {
      _errorMessage = 'Erreur de chargement du produit';
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> searchProducts(String query) async {
    if (query.length < 2) {
      _products = [];
      notifyListeners();
      return;
    }

    await loadProducts(keywords: query, refresh: true);
  }

  void clearSelectedProduct() {
    _selectedProduct = null;
    notifyListeners();
  }

  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}

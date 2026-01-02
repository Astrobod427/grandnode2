import 'package:flutter/foundation.dart';
import '../core/api/api_service.dart';
import '../core/config/api_config.dart';
import '../models/product.dart';

class ProductProvider extends ChangeNotifier {
  final ApiService apiService;

  List<Product> _products = [];
  List<Product> _featuredProducts = [];
  List<Map<String, dynamic>> _categories = [];
  Product? _selectedProduct;
  bool _isLoading = false;
  String? _error;
  int _totalCount = 0;
  int _currentPage = 0;
  bool _hasMore = true;

  ProductProvider({required this.apiService});

  List<Product> get products => _products;
  List<Product> get featuredProducts => _featuredProducts;
  List<Map<String, dynamic>> get categories => _categories;
  Product? get selectedProduct => _selectedProduct;
  bool get isLoading => _isLoading;
  String? get error => _error;
  int get totalCount => _totalCount;
  bool get hasMore => _hasMore;

  Future<void> loadCategories() async {
    try {
      final response = await apiService.get(ApiConfig.catalogCategoriesEndpoint);
      if (response != null && response['items'] != null) {
        _categories = List<Map<String, dynamic>>.from(response['items']);
        notifyListeners();
      }
    } catch (e) {
      debugPrint('Error loading categories: $e');
    }
  }

  Future<void> loadFeaturedProducts() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await apiService.getFeaturedProducts(limit: 10);
      if (response['items'] != null) {
        _featuredProducts = (response['items'] as List)
            .map<Product>((json) => Product.fromJson(json))
            .toList();
      }
    } catch (e) {
      _error = 'Erreur de chargement';
      debugPrint('Error loading featured products: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> loadProductsByCategory(String categoryId, {bool refresh = false}) async {
    if (refresh) {
      _currentPage = 0;
      _products = [];
      _hasMore = true;
    }

    if (!_hasMore || _isLoading) return;

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await apiService.getProductsByCategory(
        categoryId,
        page: _currentPage,
        pageSize: 20,
      );

      if (response['items'] != null) {
        final newProducts = (response['items'] as List)
            .map<Product>((json) => Product.fromJson(json))
            .toList();

        _totalCount = response['totalCount'] ?? 0;

        if (refresh) {
          _products = newProducts;
        } else {
          _products.addAll(newProducts);
        }

        _currentPage++;
        _hasMore = _products.length < _totalCount;
      }
    } catch (e) {
      _error = 'Erreur de chargement';
      debugPrint('Error loading products: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> searchProducts(String query, {bool refresh = true}) async {
    if (query.length < 2) {
      _products = [];
      _totalCount = 0;
      notifyListeners();
      return;
    }

    if (refresh) {
      _currentPage = 0;
      _products = [];
      _hasMore = true;
    }

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await apiService.searchProducts(
        query,
        page: _currentPage,
        pageSize: 20,
      );

      if (response['items'] != null) {
        final newProducts = (response['items'] as List)
            .map<Product>((json) => Product.fromJson(json))
            .toList();

        _totalCount = response['totalCount'] ?? 0;

        if (refresh) {
          _products = newProducts;
        } else {
          _products.addAll(newProducts);
        }

        _currentPage++;
        _hasMore = _products.length < _totalCount;
      }
    } catch (e) {
      _error = 'Erreur de recherche';
      debugPrint('Error searching products: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> loadProductDetails(String productId) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await apiService.getProductDetails(productId);
      _selectedProduct = Product.fromJson(response);
    } catch (e) {
      _error = 'Erreur de chargement du produit';
      debugPrint('Error loading product: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  void clearProducts() {
    _products = [];
    _currentPage = 0;
    _hasMore = true;
    _totalCount = 0;
    notifyListeners();
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}

import 'package:flutter/foundation.dart';
import '../core/api/api_service.dart';
import '../core/config/api_config.dart';
import '../models/cart.dart';

class CartProvider extends ChangeNotifier {
  final ApiService _apiService;

  ShoppingCart _cart = ShoppingCart.empty();
  bool _isLoading = false;
  String? _errorMessage;

  CartProvider({ApiService? apiService})
      : _apiService = apiService ?? ApiService();

  ShoppingCart get cart => _cart;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  int get itemCount => _cart.totalItems;
  double get total => _cart.subTotal;
  bool get isEmpty => _cart.isEmpty;
  bool get isNotEmpty => _cart.isNotEmpty;

  Future<void> loadCart() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.get(
        ApiConfig.cartEndpoint,
        requireAuth: true,
      );

      if (response != null) {
        _cart = ShoppingCart.fromJson(response);
      }
    } on ApiException catch (e) {
      if (e.statusCode == 401) {
        _cart = ShoppingCart.empty();
      } else {
        _errorMessage = e.message;
      }
    } catch (e) {
      _errorMessage = 'Failed to load cart';
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> addToCart(String productId, {int quantity = 1}) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.post(
        ApiConfig.cartEndpoint,
        body: {
          'productId': productId,
          'quantity': quantity,
        },
        requireAuth: true,
      );

      if (response != null && response['Success'] == true) {
        await loadCart();
        return true;
      } else {
        _errorMessage = response?['Warnings']?.join(', ') ?? 'Failed to add item';
        _isLoading = false;
        notifyListeners();
        return false;
      }
    } on ApiException catch (e) {
      _errorMessage = e.message;
      _isLoading = false;
      notifyListeners();
      return false;
    } catch (e) {
      _errorMessage = 'Failed to add item to cart';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> updateQuantity(String itemId, int quantity) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.put(
        '${ApiConfig.cartEndpoint}/$itemId',
        body: {'quantity': quantity},
        requireAuth: true,
      );

      if (response != null && response['Success'] == true) {
        await loadCart();
        return true;
      } else {
        _errorMessage = response?['Warnings']?.join(', ') ?? 'Failed to update';
        _isLoading = false;
        notifyListeners();
        return false;
      }
    } catch (e) {
      _errorMessage = 'Failed to update quantity';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> removeItem(String itemId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _apiService.delete(
        '${ApiConfig.cartEndpoint}/$itemId',
        requireAuth: true,
      );
      await loadCart();
      return true;
    } catch (e) {
      _errorMessage = 'Failed to remove item';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> clearCart() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _apiService.delete(
        ApiConfig.cartEndpoint,
        requireAuth: true,
      );
      _cart = ShoppingCart.empty();
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage = 'Failed to clear cart';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}

import 'package:flutter/foundation.dart';
import '../core/api/api_service.dart';
import '../models/product.dart';

class CartItem {
  final Product product;
  int quantity;

  CartItem({required this.product, this.quantity = 1});

  double get total => product.price * quantity;
}

class CartProvider with ChangeNotifier {
  final ApiService apiService;
  final List<CartItem> _items = [];
  bool _isLoading = false;
  String? _error;

  CartProvider({required this.apiService});

  List<CartItem> get items => List.unmodifiable(_items);
  bool get isLoading => _isLoading;
  String? get error => _error;
  int get itemCount => _items.fold(0, (sum, item) => sum + item.quantity);
  double get total => _items.fold(0, (sum, item) => sum + item.total);

  void addToCart(Product product, {int quantity = 1}) {
    final existingIndex = _items.indexWhere((item) => item.product.id == product.id);
    if (existingIndex >= 0) {
      _items[existingIndex].quantity += quantity;
    } else {
      _items.add(CartItem(product: product, quantity: quantity));
    }
    notifyListeners();
  }

  void removeFromCart(String productId) {
    _items.removeWhere((item) => item.product.id == productId);
    notifyListeners();
  }

  void updateQuantity(String productId, int quantity) {
    final index = _items.indexWhere((item) => item.product.id == productId);
    if (index >= 0) {
      if (quantity <= 0) {
        _items.removeAt(index);
      } else {
        _items[index].quantity = quantity;
      }
      notifyListeners();
    }
  }

  void clearCart() {
    _items.clear();
    notifyListeners();
  }

  Future<void> syncWithServer() async {
    if (_items.isEmpty) return;

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      for (final item in _items) {
        await apiService.addToCart(item.product.id, item.quantity);
      }
    } catch (e) {
      _error = 'Erreur de synchronisation du panier';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}

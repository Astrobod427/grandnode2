import 'package:flutter/foundation.dart';
import '../core/api/api_service.dart';
import '../models/checkout.dart';

class CheckoutProvider with ChangeNotifier {
  final ApiService apiService;

  CheckoutSummary? _summary;
  List<Address> _addresses = [];
  String? _selectedBillingAddressId;
  String? _selectedShippingAddressId;
  PaymentMethod? _selectedPaymentMethod;
  ShippingOption? _selectedShippingOption;
  bool _isLoading = false;
  String? _error;
  PlaceOrderResult? _orderResult;

  CheckoutProvider({required this.apiService});

  CheckoutSummary? get summary => _summary;
  List<Address> get addresses => _addresses;
  String? get selectedBillingAddressId => _selectedBillingAddressId;
  String? get selectedShippingAddressId => _selectedShippingAddressId;
  PaymentMethod? get selectedPaymentMethod => _selectedPaymentMethod;
  ShippingOption? get selectedShippingOption => _selectedShippingOption;
  bool get isLoading => _isLoading;
  String? get error => _error;
  PlaceOrderResult? get orderResult => _orderResult;

  bool get canPlaceOrder =>
      _summary?.canPlaceOrder == true &&
      _selectedPaymentMethod != null &&
      (!(_summary?.requiresShipping ?? false) || _selectedShippingOption != null);

  Future<void> loadCheckoutSummary() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final data = await apiService.getCheckoutSummary();
      _summary = CheckoutSummary.fromJson(data);

      // Auto-select if only one option available
      if (_summary!.availablePaymentMethods.length == 1) {
        _selectedPaymentMethod = _summary!.availablePaymentMethods.first;
      }
      if (_summary!.availableShippingOptions.length == 1) {
        _selectedShippingOption = _summary!.availableShippingOptions.first;
      }

      _error = null;
    } catch (e) {
      _error = e.toString();
      debugPrint('Error loading checkout summary: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadAddresses() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final data = await apiService.getAddresses();
      final addressList = (data['addresses'] as List?)
              ?.map((e) => Address.fromJson(e))
              .toList() ??
          [];
      _addresses = addressList;
      _selectedBillingAddressId = data['billingAddressId'];
      _selectedShippingAddressId = data['shippingAddressId'];
      _error = null;
    } catch (e) {
      _error = e.toString();
      debugPrint('Error loading addresses: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> setBillingAddress(String addressId) async {
    try {
      await apiService.setBillingAddress(addressId);
      _selectedBillingAddressId = addressId;
      notifyListeners();
      await loadCheckoutSummary(); // Refresh to update payment methods
      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<bool> setShippingAddress(String addressId) async {
    try {
      await apiService.setShippingAddress(addressId);
      _selectedShippingAddressId = addressId;
      notifyListeners();
      await loadCheckoutSummary(); // Refresh to update shipping options
      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  void selectPaymentMethod(PaymentMethod method) {
    _selectedPaymentMethod = method;
    notifyListeners();
  }

  void selectShippingOption(ShippingOption option) {
    _selectedShippingOption = option;
    notifyListeners();
  }

  Future<bool> placeOrder() async {
    if (!canPlaceOrder || _selectedPaymentMethod == null) {
      _error = 'Please complete all required fields';
      notifyListeners();
      return false;
    }

    _isLoading = true;
    _error = null;
    _orderResult = null;
    notifyListeners();

    try {
      final data = await apiService.placeOrder(
        paymentMethodSystemName: _selectedPaymentMethod!.systemName,
        shippingOptionName: _selectedShippingOption?.name,
        shippingRateProviderSystemName:
            _selectedShippingOption?.shippingRateProviderSystemName,
        useLoyaltyPoints: false,
      );

      _orderResult = PlaceOrderResult.fromJson(data);

      if (_orderResult!.success) {
        // Clear checkout state on success
        _summary = null;
        _selectedPaymentMethod = null;
        _selectedShippingOption = null;
        _error = null;
        notifyListeners();
        return true;
      } else {
        _error = _orderResult!.errors.join('\n');
        notifyListeners();
        return false;
      }
    } catch (e) {
      _error = e.toString();
      _orderResult = null;
      notifyListeners();
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }

  void reset() {
    _summary = null;
    _addresses = [];
    _selectedBillingAddressId = null;
    _selectedShippingAddressId = null;
    _selectedPaymentMethod = null;
    _selectedShippingOption = null;
    _orderResult = null;
    _error = null;
    _isLoading = false;
    notifyListeners();
  }
}

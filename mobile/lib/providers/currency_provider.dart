import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

class CurrencyProvider extends ChangeNotifier {
  String _currencyCode = 'CHF'; // Default to Swiss Franc
  String _currencySymbol = 'CHF';

  String get currencyCode => _currencyCode;
  String get currencySymbol => _currencySymbol;

  CurrencyProvider() {
    _loadCurrency();
  }

  Future<void> _loadCurrency() async {
    final prefs = await SharedPreferences.getInstance();
    _currencyCode = prefs.getString('currency_code') ?? 'CHF';
    _currencySymbol = prefs.getString('currency_symbol') ?? 'CHF';
    notifyListeners();
  }

  Future<void> setCurrency(String code, String symbol) async {
    if (_currencyCode == code) return;

    _currencyCode = code;
    _currencySymbol = symbol;
    notifyListeners();

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('currency_code', code);
    await prefs.setString('currency_symbol', symbol);
  }

  // Supported currencies
  static const List<Map<String, String>> supportedCurrencies = [
    {'code': 'CHF', 'symbol': 'CHF', 'name': 'Franc suisse'},
    {'code': 'EUR', 'symbol': '€', 'name': 'Euro'},
    {'code': 'USD', 'symbol': '\$', 'name': 'Dollar américain'},
    {'code': 'GBP', 'symbol': '£', 'name': 'Livre sterling'},
    {'code': 'CAD', 'symbol': 'C\$', 'name': 'Dollar canadien'},
  ];

  String formatPrice(double price) {
    return '$_currencySymbol ${price.toStringAsFixed(2)}';
  }
}

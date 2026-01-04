import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

class LocaleProvider extends ChangeNotifier {
  Locale _locale = const Locale('fr'); // Default to French

  Locale get locale => _locale;

  LocaleProvider() {
    _loadLocale();
  }

  Future<void> _loadLocale() async {
    final prefs = await SharedPreferences.getInstance();
    final languageCode = prefs.getString('language_code') ?? 'fr';
    _locale = Locale(languageCode);
    notifyListeners();
  }

  Future<void> setLocale(Locale locale) async {
    if (_locale == locale) return;

    _locale = locale;
    notifyListeners();

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('language_code', locale.languageCode);
  }

  // Supported locales
  static const List<Locale> supportedLocales = [
    Locale('fr', 'FR'), // French
    Locale('en', 'US'), // English
    Locale('de', 'DE'), // German
    Locale('it', 'IT'), // Italian
    Locale('es', 'ES'), // Spanish
  ];

  static String getLanguageName(String languageCode) {
    switch (languageCode) {
      case 'fr':
        return 'Français';
      case 'en':
        return 'English';
      case 'de':
        return 'Deutsch';
      case 'it':
        return 'Italiano';
      case 'es':
        return 'Español';
      default:
        return languageCode;
    }
  }
}

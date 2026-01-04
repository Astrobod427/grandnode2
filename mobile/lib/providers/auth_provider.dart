import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../core/api/api_service.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthProvider with ChangeNotifier {
  final ApiService apiService;
  AuthStatus _status = AuthStatus.unknown;
  String? _email;
  String? _firstName;
  String? _lastName;
  String? _error;

  AuthProvider({required this.apiService});

  AuthStatus get status => _status;
  String? get email => _email;
  String? get userEmail => _email;
  String? get firstName => _firstName;
  String? get lastName => _lastName;
  String get displayName {
    if (_firstName != null && _lastName != null) {
      return '$_firstName $_lastName';
    }
    return _email ?? 'Utilisateur';
  }
  String? get error => _error;
  bool get isAuthenticated => _status == AuthStatus.authenticated;

  Future<void> checkAuthStatus() async {
    final token = await apiService.getToken();
    if (token != null) {
      _status = AuthStatus.authenticated;
      // Load user info from storage
      await _loadUserInfo();
    } else {
      _status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<void> _loadUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    _email = prefs.getString('user_email');
    _firstName = prefs.getString('user_firstName');
    _lastName = prefs.getString('user_lastName');
  }

  Future<void> _saveUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    if (_email != null) await prefs.setString('user_email', _email!);
    if (_firstName != null) await prefs.setString('user_firstName', _firstName!);
    if (_lastName != null) await prefs.setString('user_lastName', _lastName!);
  }

  Future<void> _clearUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('user_email');
    await prefs.remove('user_firstName');
    await prefs.remove('user_lastName');
  }

  Future<bool> login(String email, String password) async {
    _error = null;
    try {
      final response = await apiService.login(email, password);
      _email = response['email'] as String?;
      _firstName = response['firstName'] as String?;
      _lastName = response['lastName'] as String?;
      _status = AuthStatus.authenticated;
      await _saveUserInfo();
      notifyListeners();
      return true;
    } catch (e) {
      _error = 'Identifiants invalides';
      _status = AuthStatus.unauthenticated;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    await apiService.clearToken();
    await _clearUserInfo();
    _email = null;
    _firstName = null;
    _lastName = null;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  Future<bool> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    _error = null;
    try {
      await apiService.register(
        email: email,
        password: password,
        firstName: firstName,
        lastName: lastName,
      );
      // Auto-login after registration
      final loginSuccess = await login(email, password);
      if (!loginSuccess) {
        _error = 'Inscription réussie mais échec de connexion automatique';
      }
      return loginSuccess;
    } catch (e) {
      _error = 'Erreur lors de l\'inscription: $e';
      notifyListeners();
      return false;
    }
  }
}

import 'package:flutter/foundation.dart';
import '../core/api/api_service.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthProvider with ChangeNotifier {
  final ApiService apiService;
  AuthStatus _status = AuthStatus.unknown;
  String? _email;
  String? _error;

  AuthProvider({required this.apiService});

  AuthStatus get status => _status;
  String? get email => _email;
  String? get userEmail => _email;
  String? get error => _error;
  bool get isAuthenticated => _status == AuthStatus.authenticated;

  Future<void> checkAuthStatus() async {
    final token = await apiService.getToken();
    if (token != null) {
      _status = AuthStatus.authenticated;
    } else {
      _status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<bool> login(String email, String password) async {
    _error = null;
    try {
      await apiService.login(email, password);
      _email = email;
      _status = AuthStatus.authenticated;
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
    _email = null;
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
      return await login(email, password);
    } catch (e) {
      _error = 'Erreur lors de l\'inscription';
      notifyListeners();
      return false;
    }
  }
}

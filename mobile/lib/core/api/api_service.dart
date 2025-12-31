import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;

  ApiException(this.message, {this.statusCode});

  @override
  String toString() => 'ApiException: $message (status: $statusCode)';
}

class ApiService {
  final http.Client _client;

  static const String _tokenKey = 'jwt_token';
  static const String _emailKey = 'user_email';

  ApiService({http.Client? client}) : _client = client ?? http.Client();

  // Auth headers
  Future<Map<String, String>> _getHeaders({bool requireAuth = false}) async {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (requireAuth) {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString(_tokenKey);
      if (token != null) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    return headers;
  }

  // Login and get JWT token
  Future<bool> login(String email, String password) async {
    try {
      // Encode password in base64 as required by GrandNode API
      final passwordBase64 = base64Encode(utf8.encode(password));

      final response = await _client.post(
        Uri.parse('${ApiConfig.baseUrl}${ApiConfig.tokenEndpoint}'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'email': email,
          'password': passwordBase64,
        }),
      );

      if (response.statusCode == 200) {
        final token = response.body;
        if (token.isNotEmpty && !token.contains('error')) {
          final prefs = await SharedPreferences.getInstance();
          await prefs.setString(_tokenKey, token);
          await prefs.setString(_emailKey, email);
          return true;
        }
      }

      return false;
    } catch (e) {
      throw ApiException('Login failed: $e');
    }
  }

  // Logout
  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_emailKey);
  }

  // Check if logged in
  Future<bool> isLoggedIn() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString(_tokenKey);
    return token != null && token.isNotEmpty;
  }

  // Get current user email
  Future<String?> getCurrentUserEmail() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_emailKey);
  }

  // GET request
  Future<dynamic> get(
    String endpoint, {
    Map<String, String>? queryParams,
    bool requireAuth = false,
  }) async {
    try {
      var uri = Uri.parse('${ApiConfig.baseUrl}$endpoint');
      if (queryParams != null) {
        uri = uri.replace(queryParameters: queryParams);
      }

      final headers = await _getHeaders(requireAuth: requireAuth);
      final response = await _client.get(uri, headers: headers);

      return _handleResponse(response);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException('GET request failed: $e');
    }
  }

  // POST request
  Future<dynamic> post(
    String endpoint, {
    dynamic body,
    bool requireAuth = false,
  }) async {
    try {
      final headers = await _getHeaders(requireAuth: requireAuth);
      final response = await _client.post(
        Uri.parse('${ApiConfig.baseUrl}$endpoint'),
        headers: headers,
        body: body != null ? jsonEncode(body) : null,
      );

      return _handleResponse(response);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException('POST request failed: $e');
    }
  }

  // PUT request
  Future<dynamic> put(
    String endpoint, {
    dynamic body,
    bool requireAuth = false,
  }) async {
    try {
      final headers = await _getHeaders(requireAuth: requireAuth);
      final response = await _client.put(
        Uri.parse('${ApiConfig.baseUrl}$endpoint'),
        headers: headers,
        body: body != null ? jsonEncode(body) : null,
      );

      return _handleResponse(response);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException('PUT request failed: $e');
    }
  }

  // DELETE request
  Future<dynamic> delete(
    String endpoint, {
    bool requireAuth = false,
  }) async {
    try {
      final headers = await _getHeaders(requireAuth: requireAuth);
      final response = await _client.delete(
        Uri.parse('${ApiConfig.baseUrl}$endpoint'),
        headers: headers,
      );

      return _handleResponse(response);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException('DELETE request failed: $e');
    }
  }

  // Handle response
  dynamic _handleResponse(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) {
        return null;
      }
      try {
        return jsonDecode(response.body);
      } catch (e) {
        return response.body;
      }
    } else if (response.statusCode == 401) {
      throw ApiException('Unauthorized', statusCode: 401);
    } else if (response.statusCode == 404) {
      throw ApiException('Not found', statusCode: 404);
    } else {
      String message = 'Request failed';
      try {
        final body = jsonDecode(response.body);
        if (body is Map && body.containsKey('error')) {
          message = body['error'];
        }
      } catch (_) {}
      throw ApiException(message, statusCode: response.statusCode);
    }
  }
}

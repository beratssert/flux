import 'package:flutter/foundation.dart';

class AppConfig {
  static const String _apiBaseUrlOverride =
      String.fromEnvironment('FLUX_API_BASE_URL');
  static const int _dockerApiPort = 5001;

  static String get apiBaseUrl {
    if (_apiBaseUrlOverride.isNotEmpty) {
      return _apiBaseUrlOverride;
    }

    if (kIsWeb) {
      final host = Uri.base.host.isEmpty ? 'localhost' : Uri.base.host;
      final uriScheme = Uri.base.scheme;
      final scheme =
          uriScheme == 'http' || uriScheme == 'https' ? uriScheme : 'http';
      return '$scheme://$host:$_dockerApiPort';
    }

    final host = defaultTargetPlatform == TargetPlatform.android
        ? '10.0.2.2'
        : 'localhost';

    return 'http://$host:$_dockerApiPort';
  }
}

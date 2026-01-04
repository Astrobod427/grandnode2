import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_cache_manager/flutter_cache_manager.dart';
import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';
import '../../providers/locale_provider.dart';
import '../../providers/currency_provider.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  String _appVersion = '...';

  @override
  void initState() {
    super.initState();
    _loadAppVersion();
  }

  Future<void> _loadAppVersion() async {
    final packageInfo = await PackageInfo.fromPlatform();
    setState(() {
      _appVersion = '${packageInfo.version} (${packageInfo.buildNumber})';
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Paramètres'),
      ),
      body: Consumer<ThemeProvider>(
        builder: (context, themeProvider, child) {
          return ListView(
            children: [
              // Appearance section
              const _SectionHeader(title: 'Apparence'),
              SwitchListTile(
                secondary: const Icon(Icons.dark_mode),
                title: const Text('Mode sombre'),
                subtitle: const Text('Activer le thème sombre'),
                value: themeProvider.isDarkMode,
                onChanged: (value) {
                  themeProvider.setDarkMode(value);
                },
              ),
              ListTile(
                leading: const Icon(Icons.palette),
                title: const Text('Couleur du thème'),
                subtitle: Text(_getColorName(themeProvider.primaryColor)),
                trailing: Container(
                  width: 24,
                  height: 24,
                  decoration: BoxDecoration(
                    color: themeProvider.primaryColor,
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.grey),
                  ),
                ),
                onTap: () => _showColorPicker(context, themeProvider),
              ),
              Consumer<LocaleProvider>(
                builder: (context, localeProvider, _) => ListTile(
                  leading: const Icon(Icons.language),
                  title: const Text('Langue'),
                  subtitle: Text(LocaleProvider.getLanguageName(localeProvider.locale.languageCode)),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _showLanguagePicker(context, localeProvider),
                ),
              ),
              Consumer<CurrencyProvider>(
                builder: (context, currencyProvider, _) => ListTile(
                  leading: const Icon(Icons.attach_money),
                  title: const Text('Devise'),
                  subtitle: Text('${currencyProvider.currencyCode} (${currencyProvider.currencySymbol})'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _showCurrencyPicker(context, currencyProvider),
                ),
              ),
              const Divider(),

              // Account section
              const _SectionHeader(title: 'Compte'),
              ListTile(
                leading: const Icon(Icons.location_on_outlined),
                title: const Text('Mes adresses'),
                subtitle: const Text('Gérer les adresses de livraison'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.payment_outlined),
                title: const Text('Moyens de paiement'),
                subtitle: const Text('Gérer les cartes bancaires'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.lock_outlined),
                title: const Text('Changer le mot de passe'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => _showChangePasswordDialog(context),
              ),
              ListTile(
                leading: const Icon(Icons.delete_outline, color: Colors.red),
                title: const Text('Supprimer le compte', style: TextStyle(color: Colors.red)),
                trailing: const Icon(Icons.chevron_right, color: Colors.red),
                onTap: () => _showDeleteAccountDialog(context),
              ),
              const Divider(),

              // Notifications section
              const _SectionHeader(title: 'Notifications'),
              SwitchListTile(
                secondary: const Icon(Icons.notifications_outlined),
                title: const Text('Notifications push'),
                subtitle: const Text('Recevoir les notifications'),
                value: true, // TODO: implement
                onChanged: (value) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              SwitchListTile(
                secondary: const Icon(Icons.email_outlined),
                title: const Text('Newsletter'),
                subtitle: const Text('Recevoir les promotions par email'),
                value: false, // TODO: implement
                onChanged: (value) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              const Divider(),

              // Storage section
              const _SectionHeader(title: 'Stockage'),
              ListTile(
                leading: const Icon(Icons.cleaning_services_outlined),
                title: const Text('Vider le cache'),
                subtitle: const Text('Supprimer les images en cache'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => _clearCache(context),
              ),
              const Divider(),

              // About section
              const _SectionHeader(title: 'À propos'),
              ListTile(
                leading: const Icon(Icons.info_outline),
                title: const Text('Version'),
                subtitle: Text(_appVersion),
              ),
              ListTile(
                leading: const Icon(Icons.description_outlined),
                title: const Text('Conditions d\'utilisation'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.privacy_tip_outlined),
                title: const Text('Politique de confidentialité'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              const Divider(),

              // Help & Support section
              const _SectionHeader(title: 'Aide & Support'),
              ListTile(
                leading: const Icon(Icons.help_outline),
                title: const Text('Centre d\'aide / FAQ'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.email_outlined),
                title: const Text('Nous contacter'),
                subtitle: const Text('support@labaraque.shop'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.bug_report_outlined),
                title: const Text('Signaler un problème'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bientôt disponible')),
                  );
                },
              ),
              const SizedBox(height: 24),

              // Sync theme button
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: OutlinedButton.icon(
                  onPressed: () async {
                    try {
                      await themeProvider.syncWithStore();
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Thème synchronisé')),
                        );
                      }
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Erreur: $e')),
                        );
                      }
                    }
                  },
                  icon: const Icon(Icons.sync),
                  label: const Text('Synchroniser le thème avec la boutique'),
                ),
              ),
              const SizedBox(height: 16),

              // Logout button
              Consumer<AuthProvider>(
                builder: (context, auth, _) => auth.isAuthenticated
                    ? Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        child: OutlinedButton.icon(
                          onPressed: () async {
                            final confirmed = await showDialog<bool>(
                              context: context,
                              builder: (context) => AlertDialog(
                                title: const Text('Se déconnecter'),
                                content: const Text('Êtes-vous sûr de vouloir vous déconnecter ?'),
                                actions: [
                                  TextButton(
                                    onPressed: () => Navigator.pop(context, false),
                                    child: const Text('Annuler'),
                                  ),
                                  TextButton(
                                    onPressed: () => Navigator.pop(context, true),
                                    style: TextButton.styleFrom(foregroundColor: Colors.red),
                                    child: const Text('Se déconnecter'),
                                  ),
                                ],
                              ),
                            );

                            if (confirmed == true && context.mounted) {
                              await auth.logout();
                              if (context.mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(content: Text('Vous êtes déconnecté')),
                                );
                              }
                            }
                          },
                          icon: const Icon(Icons.logout, color: Colors.red),
                          label: const Text('Se déconnecter', style: TextStyle(color: Colors.red)),
                          style: OutlinedButton.styleFrom(
                            side: const BorderSide(color: Colors.red),
                          ),
                        ),
                      )
                    : const SizedBox.shrink(),
              ),
              const SizedBox(height: 24),
            ],
          );
        },
      ),
    );
  }

  void _showChangePasswordDialog(BuildContext context) {
    final currentPasswordController = TextEditingController();
    final newPasswordController = TextEditingController();
    final confirmPasswordController = TextEditingController();

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Changer le mot de passe'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: currentPasswordController,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Mot de passe actuel',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: newPasswordController,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Nouveau mot de passe',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: confirmPasswordController,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Confirmer le mot de passe',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Annuler'),
          ),
          TextButton(
            onPressed: () {
              // TODO: Implement password change API call
              Navigator.pop(context);
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Fonctionnalité bientôt disponible')),
              );
            },
            child: const Text('Confirmer'),
          ),
        ],
      ),
    );
  }

  void _showDeleteAccountDialog(BuildContext context) {
    final passwordController = TextEditingController();

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Supprimer le compte'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
              'Cette action est irréversible. Toutes vos données seront supprimées définitivement.',
              style: TextStyle(color: Colors.red),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: passwordController,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Confirmez avec votre mot de passe',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Annuler'),
          ),
          TextButton(
            onPressed: () {
              // TODO: Implement account deletion API call
              Navigator.pop(context);
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Fonctionnalité bientôt disponible')),
              );
            },
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Supprimer'),
          ),
        ],
      ),
    );
  }

  Future<void> _clearCache(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Vider le cache'),
        content: const Text('Êtes-vous sûr de vouloir supprimer toutes les images en cache ?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Annuler'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Vider'),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      try {
        await CachedNetworkImage.evictFromCache('');
        await DefaultCacheManager().emptyCache();

        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Cache vidé avec succès')),
          );
        }
      } catch (e) {
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Erreur: $e')),
          );
        }
      }
    }
  }

  String _getColorName(Color color) {
    if (color == Colors.blue) return 'Bleu';
    if (color == Colors.red) return 'Rouge';
    if (color == Colors.green) return 'Vert';
    if (color == Colors.purple) return 'Violet';
    if (color == Colors.orange) return 'Orange';
    if (color == Colors.teal) return 'Turquoise';
    if (color == Colors.pink) return 'Rose';
    return 'Personnalisé';
  }

  void _showColorPicker(BuildContext context, ThemeProvider themeProvider) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Choisir une couleur'),
        content: Wrap(
          spacing: 12,
          runSpacing: 12,
          children: [
            Colors.blue,
            Colors.red,
            Colors.green,
            Colors.purple,
            Colors.orange,
            Colors.teal,
            Colors.pink,
          ].map((color) {
            return GestureDetector(
              onTap: () {
                themeProvider.setPrimaryColor(color);
                Navigator.pop(context);
              },
              child: Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: color,
                  shape: BoxShape.circle,
                  border: themeProvider.primaryColor == color
                      ? Border.all(color: Colors.black, width: 3)
                      : null,
                ),
              ),
            );
          }).toList(),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Annuler'),
          ),
        ],
      ),
    );
  }

  void _showLanguagePicker(BuildContext context, LocaleProvider localeProvider) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Choisir une langue'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: LocaleProvider.supportedLocales.map((locale) {
            final isSelected = localeProvider.locale.languageCode == locale.languageCode;
            return RadioListTile<String>(
              title: Text(LocaleProvider.getLanguageName(locale.languageCode)),
              value: locale.languageCode,
              groupValue: localeProvider.locale.languageCode,
              onChanged: (value) {
                if (value != null) {
                  localeProvider.setLocale(Locale(value));
                  Navigator.pop(context);
                }
              },
            );
          }).toList(),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Annuler'),
          ),
        ],
      ),
    );
  }

  void _showCurrencyPicker(BuildContext context, CurrencyProvider currencyProvider) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Choisir une devise'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: CurrencyProvider.supportedCurrencies.map((currency) {
            final code = currency['code']!;
            final symbol = currency['symbol']!;
            final name = currency['name']!;
            return RadioListTile<String>(
              title: Text(name),
              subtitle: Text('$code ($symbol)'),
              value: code,
              groupValue: currencyProvider.currencyCode,
              onChanged: (value) {
                if (value != null) {
                  currencyProvider.setCurrency(code, symbol);
                  Navigator.pop(context);
                }
              },
            );
          }).toList(),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Annuler'),
          ),
        ],
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;

  const _SectionHeader({required this.title});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Text(
        title,
        style: TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.bold,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }
}

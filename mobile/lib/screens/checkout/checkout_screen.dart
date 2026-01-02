import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/checkout_provider.dart';
import '../../providers/cart_provider.dart';
import '../../models/checkout.dart';

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key});

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<CheckoutProvider>().loadCheckoutSummary();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Paiement'),
      ),
      body: Consumer<CheckoutProvider>(
        builder: (context, checkout, child) {
          if (checkout.isLoading && checkout.summary == null) {
            return const Center(child: CircularProgressIndicator());
          }

          if (checkout.error != null && checkout.summary == null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 64, color: Colors.red),
                  const SizedBox(height: 16),
                  Text(checkout.error!,
                      style: const TextStyle(color: Colors.red)),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () => checkout.loadCheckoutSummary(),
                    child: const Text('Réessayer'),
                  ),
                ],
              ),
            );
          }

          final summary = checkout.summary;
          if (summary == null) {
            return const Center(child: Text('Panier vide'));
          }

          // Show order result if available
          if (checkout.orderResult != null && checkout.orderResult!.success) {
            return _OrderSuccessView(orderResult: checkout.orderResult!);
          }

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              // Warnings
              if (summary.warnings.isNotEmpty) ...[
                ...summary.warnings.map((warning) => Card(
                      color: Colors.orange.shade50,
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            const Icon(Icons.warning, color: Colors.orange),
                            const SizedBox(width: 12),
                            Expanded(
                                child: Text(warning,
                                    style: const TextStyle(
                                        color: Colors.orange))),
                          ],
                        ),
                      ),
                    )),
                const SizedBox(height: 16),
              ],

              // Cart items
              _CartSummaryCard(cart: summary.cart),
              const SizedBox(height: 16),

              // Addresses
              if (summary.billingAddress != null)
                _AddressCard(
                  title: 'Adresse de facturation',
                  address: summary.billingAddress!,
                ),
              if (summary.requiresShipping &&
                  summary.shippingAddress != null) ...[
                const SizedBox(height: 16),
                _AddressCard(
                  title: 'Adresse de livraison',
                  address: summary.shippingAddress!,
                ),
              ],
              const SizedBox(height: 16),

              // Shipping options
              if (summary.requiresShipping &&
                  summary.availableShippingOptions.isNotEmpty) ...[
                _ShippingOptionsCard(
                  options: summary.availableShippingOptions,
                  selectedOption: checkout.selectedShippingOption,
                  onSelect: (option) => checkout.selectShippingOption(option),
                ),
                const SizedBox(height: 16),
              ],

              // Payment methods
              if (summary.availablePaymentMethods.isNotEmpty) ...[
                _PaymentMethodsCard(
                  methods: summary.availablePaymentMethods,
                  selectedMethod: checkout.selectedPaymentMethod,
                  onSelect: (method) => checkout.selectPaymentMethod(method),
                ),
                const SizedBox(height: 16),
              ],

              // Order totals
              _OrderTotalsCard(totals: summary.totals),
              const SizedBox(height: 24),

              // Error message
              if (checkout.error != null) ...[
                Card(
                  color: Colors.red.shade50,
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Row(
                      children: [
                        Icon(Icons.error_outline, color: Colors.red.shade700),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(checkout.error!,
                              style: TextStyle(color: Colors.red.shade700)),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
              ],

              // Place order button
              SizedBox(
                height: 50,
                child: ElevatedButton(
                  onPressed: checkout.canPlaceOrder && !checkout.isLoading
                      ? () async {
                          final success = await checkout.placeOrder();
                          if (success && mounted) {
                            // Clear cart after successful order
                            context.read<CartProvider>().clearCart();
                          }
                        }
                      : null,
                  child: checkout.isLoading
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Valider la commande',
                          style: TextStyle(fontSize: 16)),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

class _CartSummaryCard extends StatelessWidget {
  final Cart cart;

  const _CartSummaryCard({required this.cart});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Panier (${cart.totalItems} articles)',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            ...cart.items.map((item) => Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    children: [
                      Expanded(
                        child: Text('${item.quantity}x ${item.productName}'),
                      ),
                      Text('${item.subTotal.toStringAsFixed(2)} ${cart.currencyCode}'),
                    ],
                  ),
                )),
          ],
        ),
      ),
    );
  }
}

class _AddressCard extends StatelessWidget {
  final String title;
  final Address address;

  const _AddressCard({required this.title, required this.address});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            Text(address.fullName),
            Text(address.fullAddress),
            if (address.phoneNumber != null) Text(address.phoneNumber!),
          ],
        ),
      ),
    );
  }
}

class _ShippingOptionsCard extends StatelessWidget {
  final List<ShippingOption> options;
  final ShippingOption? selectedOption;
  final Function(ShippingOption) onSelect;

  const _ShippingOptionsCard({
    required this.options,
    required this.selectedOption,
    required this.onSelect,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Mode de livraison',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            ...options.map((option) => RadioListTile<ShippingOption>(
                  contentPadding: EdgeInsets.zero,
                  title: Text(option.name),
                  subtitle: option.description != null
                      ? Text(option.description!)
                      : null,
                  secondary: Text('${option.rate.toStringAsFixed(2)} CHF'),
                  value: option,
                  groupValue: selectedOption,
                  onChanged: (value) {
                    if (value != null) onSelect(value);
                  },
                )),
          ],
        ),
      ),
    );
  }
}

class _PaymentMethodsCard extends StatelessWidget {
  final List<PaymentMethod> methods;
  final PaymentMethod? selectedMethod;
  final Function(PaymentMethod) onSelect;

  const _PaymentMethodsCard({
    required this.methods,
    required this.selectedMethod,
    required this.onSelect,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Mode de paiement',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            ...methods.map((method) => RadioListTile<PaymentMethod>(
                  contentPadding: EdgeInsets.zero,
                  title: Text(method.name),
                  subtitle:
                      method.description != null ? Text(method.description!) : null,
                  value: method,
                  groupValue: selectedMethod,
                  onChanged: (value) {
                    if (value != null) onSelect(value);
                  },
                )),
          ],
        ),
      ),
    );
  }
}

class _OrderTotalsCard extends StatelessWidget {
  final OrderTotals totals;

  const _OrderTotalsCard({required this.totals});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            _TotalRow('Sous-total', totals.subTotal, totals.currencyCode),
            if (totals.subTotalDiscount > 0)
              _TotalRow('Remise', -totals.subTotalDiscount, totals.currencyCode,
                  color: Colors.green),
            if (totals.shipping != null)
              _TotalRow('Livraison',
                  totals.isFreeShipping ? 0 : totals.shipping!, totals.currencyCode,
                  suffix: totals.isFreeShipping ? ' (Gratuite)' : ''),
            _TotalRow('TVA', totals.tax, totals.currencyCode),
            const Divider(),
            _TotalRow('Total', totals.total ?? 0, totals.currencyCode,
                isBold: true),
          ],
        ),
      ),
    );
  }

  Widget _TotalRow(String label, double amount, String currency,
      {bool isBold = false, Color? color, String suffix = ''}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label,
              style: TextStyle(
                  fontWeight: isBold ? FontWeight.bold : null, color: color)),
          Text('${amount.toStringAsFixed(2)} $currency$suffix',
              style: TextStyle(
                  fontWeight: isBold ? FontWeight.bold : null, color: color)),
        ],
      ),
    );
  }
}

class _OrderSuccessView extends StatelessWidget {
  final PlaceOrderResult orderResult;

  const _OrderSuccessView({required this.orderResult});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.check_circle, size: 80, color: Colors.green),
            const SizedBox(height: 24),
            Text(
              'Commande confirmée !',
              style: Theme.of(context).textTheme.headlineMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            Text(
              'Numéro de commande : ${orderResult.orderNumber}',
              style: Theme.of(context).textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              'Total : ${orderResult.orderTotal?.toStringAsFixed(2)} CHF',
              style: Theme.of(context).textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 32),
            ElevatedButton(
              onPressed: () {
                context.read<CheckoutProvider>().reset();
                Navigator.of(context).popUntil((route) => route.isFirst);
              },
              child: const Text('Retour à l\'accueil'),
            ),
          ],
        ),
      ),
    );
  }
}

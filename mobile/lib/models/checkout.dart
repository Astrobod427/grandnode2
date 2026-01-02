class CheckoutSummary {
  final Cart cart;
  final OrderTotals totals;
  final Address? billingAddress;
  final Address? shippingAddress;
  final bool requiresShipping;
  final List<PaymentMethod> availablePaymentMethods;
  final List<ShippingOption> availableShippingOptions;
  final bool canPlaceOrder;
  final List<String> warnings;

  CheckoutSummary({
    required this.cart,
    required this.totals,
    this.billingAddress,
    this.shippingAddress,
    required this.requiresShipping,
    required this.availablePaymentMethods,
    required this.availableShippingOptions,
    required this.canPlaceOrder,
    required this.warnings,
  });

  factory CheckoutSummary.fromJson(Map<String, dynamic> json) {
    return CheckoutSummary(
      cart: Cart.fromJson(json['cart'] ?? {}),
      totals: OrderTotals.fromJson(json['totals'] ?? {}),
      billingAddress: json['billingAddress'] != null
          ? Address.fromJson(json['billingAddress'])
          : null,
      shippingAddress: json['shippingAddress'] != null
          ? Address.fromJson(json['shippingAddress'])
          : null,
      requiresShipping: json['requiresShipping'] ?? false,
      availablePaymentMethods: (json['availablePaymentMethods'] as List?)
              ?.map((e) => PaymentMethod.fromJson(e))
              .toList() ??
          [],
      availableShippingOptions: (json['availableShippingOptions'] as List?)
              ?.map((e) => ShippingOption.fromJson(e))
              .toList() ??
          [],
      canPlaceOrder: json['canPlaceOrder'] ?? false,
      warnings:
          (json['warnings'] as List?)?.map((e) => e.toString()).toList() ?? [],
    );
  }
}

class Cart {
  final List<CartItem> items;
  final int totalItems;
  final double subTotal;
  final String currencyCode;

  Cart({
    required this.items,
    required this.totalItems,
    required this.subTotal,
    required this.currencyCode,
  });

  factory Cart.fromJson(Map<String, dynamic> json) {
    return Cart(
      items: (json['items'] as List?)
              ?.map((e) => CartItem.fromJson(e))
              .toList() ??
          [],
      totalItems: json['totalItems'] ?? 0,
      subTotal: (json['subTotal'] ?? 0).toDouble(),
      currencyCode: json['currencyCode'] ?? 'USD',
    );
  }
}

class CartItem {
  final String id;
  final String productId;
  final String productName;
  final String? productSku;
  final String? productImageUrl;
  final double unitPrice;
  final int quantity;
  final double subTotal;

  CartItem({
    required this.id,
    required this.productId,
    required this.productName,
    this.productSku,
    this.productImageUrl,
    required this.unitPrice,
    required this.quantity,
    required this.subTotal,
  });

  factory CartItem.fromJson(Map<String, dynamic> json) {
    return CartItem(
      id: json['id'] ?? '',
      productId: json['productId'] ?? '',
      productName: json['productName'] ?? '',
      productSku: json['productSku'],
      productImageUrl: json['productImageUrl'],
      unitPrice: (json['unitPrice'] ?? 0).toDouble(),
      quantity: json['quantity'] ?? 0,
      subTotal: (json['subTotal'] ?? 0).toDouble(),
    );
  }
}

class OrderTotals {
  final double subTotal;
  final double subTotalDiscount;
  final double? shipping;
  final bool isFreeShipping;
  final double tax;
  final double? total;
  final String currencyCode;

  OrderTotals({
    required this.subTotal,
    required this.subTotalDiscount,
    this.shipping,
    required this.isFreeShipping,
    required this.tax,
    this.total,
    required this.currencyCode,
  });

  factory OrderTotals.fromJson(Map<String, dynamic> json) {
    return OrderTotals(
      subTotal: (json['subTotal'] ?? 0).toDouble(),
      subTotalDiscount: (json['subTotalDiscount'] ?? 0).toDouble(),
      shipping: json['shipping'] != null ? (json['shipping']).toDouble() : null,
      isFreeShipping: json['isFreeShipping'] ?? false,
      tax: (json['tax'] ?? 0).toDouble(),
      total: json['total'] != null ? (json['total']).toDouble() : null,
      currencyCode: json['currencyCode'] ?? 'USD',
    );
  }
}

class Address {
  final String id;
  final String firstName;
  final String lastName;
  final String? email;
  final String? company;
  final String? countryId;
  final String? stateProvinceId;
  final String city;
  final String address1;
  final String? address2;
  final String? zipPostalCode;
  final String? phoneNumber;

  Address({
    required this.id,
    required this.firstName,
    required this.lastName,
    this.email,
    this.company,
    this.countryId,
    this.stateProvinceId,
    required this.city,
    required this.address1,
    this.address2,
    this.zipPostalCode,
    this.phoneNumber,
  });

  factory Address.fromJson(Map<String, dynamic> json) {
    return Address(
      id: json['id'] ?? '',
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      email: json['email'],
      company: json['company'],
      countryId: json['countryId'],
      stateProvinceId: json['stateProvinceId'],
      city: json['city'] ?? '',
      address1: json['address1'] ?? '',
      address2: json['address2'],
      zipPostalCode: json['zipPostalCode'],
      phoneNumber: json['phoneNumber'],
    );
  }

  String get fullAddress {
    final parts = [
      address1,
      if (address2?.isNotEmpty == true) address2,
      city,
      if (zipPostalCode?.isNotEmpty == true) zipPostalCode,
    ];
    return parts.join(', ');
  }

  String get fullName => '$firstName $lastName';
}

class PaymentMethod {
  final String systemName;
  final String name;
  final String? description;
  final double additionalFee;

  PaymentMethod({
    required this.systemName,
    required this.name,
    this.description,
    required this.additionalFee,
  });

  factory PaymentMethod.fromJson(Map<String, dynamic> json) {
    return PaymentMethod(
      systemName: json['systemName'] ?? '',
      name: json['name'] ?? '',
      description: json['description'],
      additionalFee: (json['additionalFee'] ?? 0).toDouble(),
    );
  }
}

class ShippingOption {
  final String name;
  final String? description;
  final double rate;
  final String? shippingRateProviderSystemName;

  ShippingOption({
    required this.name,
    this.description,
    required this.rate,
    this.shippingRateProviderSystemName,
  });

  factory ShippingOption.fromJson(Map<String, dynamic> json) {
    return ShippingOption(
      name: json['name'] ?? '',
      description: json['description'],
      rate: (json['rate'] ?? 0).toDouble(),
      shippingRateProviderSystemName: json['shippingRateProviderSystemName'],
    );
  }
}

class PlaceOrderResult {
  final bool success;
  final String? orderId;
  final int? orderNumber;
  final double? orderTotal;
  final List<String> errors;

  PlaceOrderResult({
    required this.success,
    this.orderId,
    this.orderNumber,
    this.orderTotal,
    required this.errors,
  });

  factory PlaceOrderResult.fromJson(Map<String, dynamic> json) {
    return PlaceOrderResult(
      success: json['success'] ?? false,
      orderId: json['orderId'],
      orderNumber: json['orderNumber'],
      orderTotal:
          json['orderTotal'] != null ? (json['orderTotal']).toDouble() : null,
      errors:
          (json['errors'] as List?)?.map((e) => e.toString()).toList() ?? [],
    );
  }
}

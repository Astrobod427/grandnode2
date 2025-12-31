class CartItem {
  final String id;
  final String productId;
  final String? productName;
  final String? productSku;
  final String? productImageUrl;
  final double unitPrice;
  final int quantity;
  final double subTotal;
  final String? warehouseId;
  final DateTime? createdOnUtc;
  final DateTime? updatedOnUtc;
  final bool isFreeShipping;
  final bool isGiftVoucher;

  CartItem({
    required this.id,
    required this.productId,
    this.productName,
    this.productSku,
    this.productImageUrl,
    required this.unitPrice,
    required this.quantity,
    required this.subTotal,
    this.warehouseId,
    this.createdOnUtc,
    this.updatedOnUtc,
    this.isFreeShipping = false,
    this.isGiftVoucher = false,
  });

  factory CartItem.fromJson(Map<String, dynamic> json) {
    return CartItem(
      id: json['Id'] ?? json['id'] ?? '',
      productId: json['ProductId'] ?? json['productId'] ?? '',
      productName: json['ProductName'] ?? json['productName'],
      productSku: json['ProductSku'] ?? json['productSku'],
      productImageUrl: json['ProductImageUrl'] ?? json['productImageUrl'],
      unitPrice: (json['UnitPrice'] ?? json['unitPrice'] ?? 0).toDouble(),
      quantity: json['Quantity'] ?? json['quantity'] ?? 1,
      subTotal: (json['SubTotal'] ?? json['subTotal'] ?? 0).toDouble(),
      warehouseId: json['WarehouseId'] ?? json['warehouseId'],
      createdOnUtc: json['CreatedOnUtc'] != null
          ? DateTime.tryParse(json['CreatedOnUtc'])
          : null,
      updatedOnUtc: json['UpdatedOnUtc'] != null
          ? DateTime.tryParse(json['UpdatedOnUtc'])
          : null,
      isFreeShipping:
          json['IsFreeShipping'] ?? json['isFreeShipping'] ?? false,
      isGiftVoucher: json['IsGiftVoucher'] ?? json['isGiftVoucher'] ?? false,
    );
  }

  CartItem copyWith({int? quantity}) {
    return CartItem(
      id: id,
      productId: productId,
      productName: productName,
      productSku: productSku,
      productImageUrl: productImageUrl,
      unitPrice: unitPrice,
      quantity: quantity ?? this.quantity,
      subTotal: unitPrice * (quantity ?? this.quantity),
      warehouseId: warehouseId,
      createdOnUtc: createdOnUtc,
      updatedOnUtc: DateTime.now(),
      isFreeShipping: isFreeShipping,
      isGiftVoucher: isGiftVoucher,
    );
  }
}

class ShoppingCart {
  final List<CartItem> items;
  final int totalItems;
  final double subTotal;
  final String currencyCode;

  ShoppingCart({
    required this.items,
    required this.totalItems,
    required this.subTotal,
    required this.currencyCode,
  });

  factory ShoppingCart.fromJson(Map<String, dynamic> json) {
    final itemsList = json['Items'] ?? json['items'] ?? [];
    return ShoppingCart(
      items: (itemsList as List).map((e) => CartItem.fromJson(e)).toList(),
      totalItems: json['TotalItems'] ?? json['totalItems'] ?? 0,
      subTotal: (json['SubTotal'] ?? json['subTotal'] ?? 0).toDouble(),
      currencyCode: json['CurrencyCode'] ?? json['currencyCode'] ?? 'CHF',
    );
  }

  factory ShoppingCart.empty() {
    return ShoppingCart(
      items: [],
      totalItems: 0,
      subTotal: 0,
      currencyCode: 'CHF',
    );
  }

  bool get isEmpty => items.isEmpty;
  bool get isNotEmpty => items.isNotEmpty;
}

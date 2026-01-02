class Product {
  final String id;
  final String name;
  final String? shortDescription;
  final String? fullDescription;
  final String? sku;
  final double price;
  final double? oldPrice;
  final int stockQuantity;
  final bool inStock;
  final bool isFeatured;
  final bool isNew;
  final bool published;
  final DateTime? createdOnUtc;
  final String? imageUrl;
  final List<String>? images;
  final List<String>? categoryIds;

  Product({
    required this.id,
    required this.name,
    this.shortDescription,
    this.fullDescription,
    this.sku,
    required this.price,
    this.oldPrice,
    this.stockQuantity = 0,
    this.inStock = true,
    this.isFeatured = false,
    this.isNew = false,
    this.published = true,
    this.createdOnUtc,
    this.imageUrl,
    this.images,
    this.categoryIds,
  });

  factory Product.fromJson(Map<String, dynamic> json) {
    return Product(
      id: json['id'] ?? json['Id'] ?? '',
      name: json['name'] ?? json['Name'] ?? '',
      shortDescription: json['shortDescription'] ?? json['ShortDescription'],
      fullDescription: json['fullDescription'] ?? json['FullDescription'],
      sku: json['sku'] ?? json['Sku'],
      price: (json['price'] ?? json['Price'] ?? 0).toDouble(),
      oldPrice: json['oldPrice'] != null ? (json['oldPrice'] as num).toDouble() : null,
      stockQuantity: json['stockQuantity'] ?? json['StockQuantity'] ?? 0,
      inStock: json['inStock'] ?? true,
      isFeatured: json['isFeatured'] ?? false,
      isNew: json['isNew'] ?? false,
      published: json['published'] ?? json['Published'] ?? true,
      createdOnUtc: json['createdOnUtc'] != null
          ? DateTime.tryParse(json['createdOnUtc'])
          : null,
      imageUrl: json['imageUrl'] ?? json['ImageUrl'],
      images: json['images'] != null ? List<String>.from(json['images']) : null,
      categoryIds: json['categoryIds'] != null
          ? List<String>.from(json['categoryIds'])
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'shortDescription': shortDescription,
      'fullDescription': fullDescription,
      'sku': sku,
      'price': price,
      'oldPrice': oldPrice,
      'stockQuantity': stockQuantity,
      'inStock': inStock,
      'isFeatured': isFeatured,
      'published': published,
      'createdOnUtc': createdOnUtc?.toIso8601String(),
      'imageUrl': imageUrl,
      'images': images,
      'categoryIds': categoryIds,
    };
  }

  bool get hasDiscount => oldPrice != null && oldPrice! > price;
  double get discountPercent => hasDiscount ? ((oldPrice! - price) / oldPrice! * 100) : 0;
}

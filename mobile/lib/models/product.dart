class Product {
  final String id;
  final String name;
  final String? shortDescription;
  final String? sku;
  final double price;
  final int stockQuantity;
  final bool published;
  final DateTime? createdOnUtc;
  final String? imageUrl;
  final List<String>? categoryIds;

  Product({
    required this.id,
    required this.name,
    this.shortDescription,
    this.sku,
    required this.price,
    this.stockQuantity = 0,
    this.published = true,
    this.createdOnUtc,
    this.imageUrl,
    this.categoryIds,
  });

  factory Product.fromJson(Map<String, dynamic> json) {
    return Product(
      id: json['id'] ?? json['Id'] ?? '',
      name: json['name'] ?? json['Name'] ?? '',
      shortDescription: json['shortDescription'] ?? json['ShortDescription'],
      sku: json['sku'] ?? json['Sku'],
      price: (json['price'] ?? json['Price'] ?? 0).toDouble(),
      stockQuantity: json['stockQuantity'] ?? json['StockQuantity'] ?? 0,
      published: json['published'] ?? json['Published'] ?? true,
      createdOnUtc: json['createdOnUtc'] != null
          ? DateTime.tryParse(json['createdOnUtc'])
          : null,
      imageUrl: json['imageUrl'] ?? json['ImageUrl'],
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
      'sku': sku,
      'price': price,
      'stockQuantity': stockQuantity,
      'published': published,
      'createdOnUtc': createdOnUtc?.toIso8601String(),
      'imageUrl': imageUrl,
      'categoryIds': categoryIds,
    };
  }

  bool get isInStock => stockQuantity > 0;
}

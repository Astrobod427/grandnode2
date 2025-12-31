class Category {
  final String id;
  final String name;
  final String? description;
  final String? imageUrl;
  final String? parentCategoryId;
  final int displayOrder;
  final bool published;

  Category({
    required this.id,
    required this.name,
    this.description,
    this.imageUrl,
    this.parentCategoryId,
    this.displayOrder = 0,
    this.published = true,
  });

  factory Category.fromJson(Map<String, dynamic> json) {
    return Category(
      id: json['id'] ?? json['Id'] ?? '',
      name: json['name'] ?? json['Name'] ?? '',
      description: json['description'] ?? json['Description'],
      imageUrl: json['imageUrl'] ?? json['ImageUrl'],
      parentCategoryId: json['parentCategoryId'] ?? json['ParentCategoryId'],
      displayOrder: json['displayOrder'] ?? json['DisplayOrder'] ?? 0,
      published: json['published'] ?? json['Published'] ?? true,
    );
  }

  bool get isRoot => parentCategoryId == null || parentCategoryId!.isEmpty;
}

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../providers/product_provider.dart';
import '../../providers/cart_provider.dart';
import '../../widgets/product_card.dart';
import '../../core/config/api_config.dart';
import '../catalog/catalog_screen.dart';
import '../cart/cart_screen.dart';
import '../account/account_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentIndex = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ProductProvider>().loadFeaturedProducts();
      context.read<ProductProvider>().loadCategories();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('La Baraque Shop'),
        actions: [
          Consumer<CartProvider>(
            builder: (context, cart, child) {
              return Stack(
                children: [
                  IconButton(
                    icon: const Icon(Icons.shopping_cart),
                    onPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (_) => const CartScreen()),
                      );
                    },
                  ),
                  if (cart.itemCount > 0)
                    Positioned(
                      right: 4,
                      top: 4,
                      child: Container(
                        padding: const EdgeInsets.all(4),
                        decoration: const BoxDecoration(
                          color: Colors.red,
                          shape: BoxShape.circle,
                        ),
                        child: Text(
                          '${cart.itemCount}',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                          ),
                        ),
                      ),
                    ),
                ],
              );
            },
          ),
        ],
      ),
      body: IndexedStack(
        index: _currentIndex,
        children: const [
          _HomeContent(),
          CatalogScreen(),
          AccountScreen(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (index) => setState(() => _currentIndex = index),
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.home),
            label: 'Accueil',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.category),
            label: 'Catalogue',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person),
            label: 'Compte',
          ),
        ],
      ),
    );
  }
}

class _HomeContent extends StatelessWidget {
  const _HomeContent();

  String _getFullImageUrl(String? imageUrl) {
    if (imageUrl == null || imageUrl.isEmpty) return '';
    if (imageUrl.startsWith('http')) return imageUrl;
    return '${ApiConfig.baseUrl}$imageUrl';
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<ProductProvider>(
      builder: (context, provider, child) {
        if (provider.isLoading && provider.featuredProducts.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }

        if (provider.error != null && provider.featuredProducts.isEmpty) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(provider.error!, style: const TextStyle(color: Colors.red)),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: () => provider.loadFeaturedProducts(),
                  child: const Text('Réessayer'),
                ),
              ],
            ),
          );
        }

        return LayoutBuilder(
          builder: (context, constraints) {
            // Responsive: sidebar for wide screens, top bar for narrow
            final isWide = constraints.maxWidth > 600;

            if (isWide) {
              return Row(
                children: [
                  // Categories sidebar
                  SizedBox(
                    width: 200,
                    child: _CategoriesSidebar(
                      categories: provider.categories,
                      getFullImageUrl: _getFullImageUrl,
                    ),
                  ),
                  const VerticalDivider(width: 1),
                  // Products
                  Expanded(
                    child: _ProductsGrid(
                      provider: provider,
                      getFullImageUrl: _getFullImageUrl,
                      crossAxisCount: (constraints.maxWidth - 200) > 800 ? 3 : 2,
                    ),
                  ),
                ],
              );
            } else {
              // Narrow layout with categories on top
              return RefreshIndicator(
                onRefresh: () async {
                  await provider.loadFeaturedProducts();
                  await provider.loadCategories();
                },
                child: CustomScrollView(
                  slivers: [
                    // Categories horizontal list
                    if (provider.categories.isNotEmpty)
                      SliverToBoxAdapter(
                        child: _CategoriesHorizontal(
                          categories: provider.categories,
                          getFullImageUrl: _getFullImageUrl,
                        ),
                      ),
                    // Products header
                    const SliverToBoxAdapter(
                      child: Padding(
                        padding: EdgeInsets.fromLTRB(16, 8, 16, 12),
                        child: Text(
                          'Produits en vedette',
                          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                        ),
                      ),
                    ),
                    // Products grid
                    SliverPadding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      sliver: SliverGrid(
                        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 2,
                          childAspectRatio: 0.7,
                          crossAxisSpacing: 12,
                          mainAxisSpacing: 12,
                        ),
                        delegate: SliverChildBuilderDelegate(
                          (context, index) {
                            return ProductCard(product: provider.featuredProducts[index]);
                          },
                          childCount: provider.featuredProducts.length,
                        ),
                      ),
                    ),
                    const SliverToBoxAdapter(child: SizedBox(height: 16)),
                  ],
                ),
              );
            }
          },
        );
      },
    );
  }
}

// Sidebar for wide screens
class _CategoriesSidebar extends StatelessWidget {
  final List<Map<String, dynamic>> categories;
  final String Function(String?) getFullImageUrl;

  const _CategoriesSidebar({
    required this.categories,
    required this.getFullImageUrl,
  });

  @override
  Widget build(BuildContext context) {
    // Filter to show only root categories (no parent)
    final rootCategories = categories
        .where((c) => c['parentCategoryId'] == null || c['parentCategoryId'] == '')
        .toList();

    return Container(
      color: Theme.of(context).colorScheme.surfaceContainerLow,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.all(16),
            child: Text(
              'Catégories',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ),
          Expanded(
            child: ListView.builder(
              itemCount: rootCategories.length,
              itemBuilder: (context, index) {
                final category = rootCategories[index];
                final imageUrl = getFullImageUrl(category['imageUrl']);
                final catId = category['id'] ?? '';
                final catName = category['name'] ?? 'Catégorie';

                return ListTile(
                  leading: ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: SizedBox(
                      width: 48,
                      height: 48,
                      child: imageUrl.isNotEmpty
                          ? CachedNetworkImage(
                              imageUrl: imageUrl,
                              fit: BoxFit.cover,
                              placeholder: (_, __) => Container(
                                color: Colors.grey[200],
                                child: const Icon(Icons.category, size: 24),
                              ),
                              errorWidget: (_, __, ___) => Container(
                                color: Colors.grey[200],
                                child: const Icon(Icons.category, size: 24),
                              ),
                            )
                          : Container(
                              color: Colors.grey[200],
                              child: const Icon(Icons.category, size: 24),
                            ),
                    ),
                  ),
                  title: Text(
                    catName,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  onTap: () {
                    context.read<ProductProvider>().loadProductsByCategory(catId);
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => CatalogScreen(
                          categoryId: catId,
                          categoryName: catName,
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

// Horizontal scrolling categories for narrow screens
class _CategoriesHorizontal extends StatelessWidget {
  final List<Map<String, dynamic>> categories;
  final String Function(String?) getFullImageUrl;

  const _CategoriesHorizontal({
    required this.categories,
    required this.getFullImageUrl,
  });

  @override
  Widget build(BuildContext context) {
    // Filter to show only root categories
    final rootCategories = categories
        .where((c) => c['parentCategoryId'] == null || c['parentCategoryId'] == '')
        .toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Padding(
          padding: EdgeInsets.fromLTRB(16, 16, 16, 12),
          child: Text(
            'Catégories',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
          ),
        ),
        SizedBox(
          height: 110,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12),
            itemCount: rootCategories.length,
            itemBuilder: (context, index) {
              final category = rootCategories[index];
              final imageUrl = getFullImageUrl(category['imageUrl']);
              final catId = category['id'] ?? '';
              final catName = category['name'] ?? 'Catégorie';

              return GestureDetector(
                onTap: () {
                  context.read<ProductProvider>().loadProductsByCategory(catId);
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => CatalogScreen(
                        categoryId: catId,
                        categoryName: catName,
                      ),
                    ),
                  );
                },
                child: Container(
                  width: 90,
                  margin: const EdgeInsets.symmetric(horizontal: 4),
                  child: Column(
                    children: [
                      // Image circle
                      Container(
                        width: 64,
                        height: 64,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: Theme.of(context).colorScheme.primaryContainer,
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withAlpha(20),
                              blurRadius: 4,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: ClipOval(
                          child: imageUrl.isNotEmpty
                              ? CachedNetworkImage(
                                  imageUrl: imageUrl,
                                  fit: BoxFit.cover,
                                  placeholder: (_, __) => Icon(
                                    Icons.category,
                                    color: Theme.of(context).colorScheme.onPrimaryContainer,
                                  ),
                                  errorWidget: (_, __, ___) => Icon(
                                    Icons.category,
                                    color: Theme.of(context).colorScheme.onPrimaryContainer,
                                  ),
                                )
                              : Icon(
                                  Icons.category,
                                  color: Theme.of(context).colorScheme.onPrimaryContainer,
                                ),
                        ),
                      ),
                      const SizedBox(height: 8),
                      // Name
                      Text(
                        catName,
                        textAlign: TextAlign.center,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}

// Products grid widget
class _ProductsGrid extends StatelessWidget {
  final ProductProvider provider;
  final String Function(String?) getFullImageUrl;
  final int crossAxisCount;

  const _ProductsGrid({
    required this.provider,
    required this.getFullImageUrl,
    required this.crossAxisCount,
  });

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: () async {
        await provider.loadFeaturedProducts();
        await provider.loadCategories();
      },
      child: CustomScrollView(
        slivers: [
          const SliverToBoxAdapter(
            child: Padding(
              padding: EdgeInsets.all(16),
              child: Text(
                'Produits en vedette',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              ),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            sliver: SliverGrid(
              gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                childAspectRatio: 0.7,
                crossAxisSpacing: 12,
                mainAxisSpacing: 12,
              ),
              delegate: SliverChildBuilderDelegate(
                (context, index) {
                  return ProductCard(product: provider.featuredProducts[index]);
                },
                childCount: provider.featuredProducts.length,
              ),
            ),
          ),
          const SliverToBoxAdapter(child: SizedBox(height: 16)),
        ],
      ),
    );
  }
}

# Changelog

All notable changes to the ricardo.ch Integration plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-29

### Added
- Initial release of ricardo.ch Integration plugin
- Core API client with JWT authentication
  - `RicardoApiClient` for HTTP communication with ricardo.ch
  - Automatic token management and renewal
  - Support for both Sandbox and Production environments
  - JSON-RPC 2.0 protocol implementation

- Product Publishing Service
  - `RicardoProductService` for business logic
  - Single product publishing to ricardo.ch
  - Automatic price markup calculation
  - Product validation before publishing
  - Image synchronization (up to 10 images)
  - HTML description cleanup and formatting

- Stock Management
  - Manual stock quantity updates
  - Article closing/removal functionality
  - Real-time sync capabilities

- Admin Configuration Interface
  - Full settings page at Admin → Configuration → Plugins → ricardo.ch Integration
  - API credentials management (Partner ID, Partner Key, Account credentials)
  - Environment selection (Sandbox/Production)
  - Price markup configuration
  - Stock sync settings
  - Connection testing functionality

- Models and DTOs
  - `RicardoSettings` for plugin configuration
  - Complete API request/response models:
    - Authentication (TokenCredentialLogin)
    - Article management (InsertArticle, UpdateArticleQuantity, CloseArticle)
    - Picture handling

- Infrastructure
  - Dependency injection setup via `StartupApplication`
  - HttpClient configuration for API communication
  - Comprehensive logging throughout

- Documentation
  - Complete README.md with installation and usage instructions
  - Credential acquisition guide
  - Troubleshooting section
  - API reference links
  - Roadmap for Phase 2 and Phase 3

### Technical Details
- Built with .NET 9.0
- Uses System.Text.Json for serialization
- Follows GrandNode plugin architecture
- ASP.NET Core MVC Areas for admin interface
- Razor views with proper MVC pattern

### Known Limitations
- Phase 1 MVP only supports manual/programmatic product publishing
- No bulk operations yet (planned for Phase 2)
- No order import from ricardo.ch (planned for Phase 2)
- No bidirectional stock sync (planned for Phase 2)
- No UI in product edit page (planned for Phase 2)

### Requirements
- GrandNode 2.3+
- .NET 9.0
- ricardo.ch professional seller account
- ricardo.ch API credentials (Partner ID, Partner Key)

### Security
- Passwords stored in GrandNode settings system
- JWT tokens managed in memory (not persisted)
- Automatic token expiration handling
- Secure HTTPS communication with ricardo.ch API

### Future Roadmap

#### Phase 2 (Planned)
- Bulk product publishing
- Order import from ricardo.ch
- Bidirectional stock synchronization
- Product performance analytics
- UI integration in product edit page

#### Phase 3 (Future)
- Auction support
- Custom templates
- Multi-account management
- Advanced reporting and analytics

---

## Version History Format

### [Unreleased]
Changes that are in development but not yet released

### [X.Y.Z] - YYYY-MM-DD
- **Added** for new features
- **Changed** for changes in existing functionality
- **Deprecated** for soon-to-be removed features
- **Removed** for now removed features
- **Fixed** for any bug fixes
- **Security** in case of vulnerabilities

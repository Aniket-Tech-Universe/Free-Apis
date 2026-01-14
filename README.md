# UnsecuredAPIKeys - Open Source Version

A comprehensive platform for discovering, validating, and tracking unsecured API keys across various code repositories and platforms. This project serves educational and security awareness purposes by demonstrating how easily API keys can be exposed in public repositories.

## ⚠️ Educational Purpose Only

This project is designed for educational and security awareness purposes. It demonstrates common security vulnerabilities in API key management. Please use responsibly and in accordance with applicable laws and regulations.

## 🏗️ Architecture

The project consists of several interconnected components:

- **WebAPI** (.NET 9): Core backend providing REST endpoints and real-time SignalR communication
- **UI** (Next.js): Frontend interface with educational content and API key discovery features
- **Data Layer** (Entity Framework + PostgreSQL): Comprehensive data modeling and persistence
- **Providers Library**: Extensible validation framework for different API providers
- **Verification Bot**: Automated validation of discovered keys
- **Scraper Bot**: Automated discovery of API keys across platforms

## 🚀 Features

### Core Functionality
- **API Key Discovery**: Search and discover exposed API keys across multiple platforms
- **Validation Engine**: Verify the validity and functionality of discovered keys
- **Real-time Updates**: Live statistics and updates using SignalR
- **Educational Interface**: Learn about API security through interactive examples

### Technical Features
- **Modular Design**: Clean separation between discovery, validation, and presentation layers
- **Extensible Provider System**: Easy addition of new API validation providers
- **Comprehensive Analytics**: Track discoveries, validations, and security metrics
- **Rate Limiting**: Intelligent rate limiting with user-based overrides
- **Discord Integration**: Enhanced features for authenticated users

## 🛠️ Technology Stack

### Backend
- **.NET 9** - Modern web API framework
- **Entity Framework Core** - ORM for database operations
- **PostgreSQL** - Primary database
- **SignalR** - Real-time communication
- **Docker** - Containerization support

### Frontend
- **Next.js 14** - React framework with TypeScript
- **HeroUI** - Modern component library
- **Tailwind CSS** - Utility-first CSS framework
- **Framer Motion** - Animation library

### Development Tools
- **Docker Compose** - Multi-container development
- **Entity Framework Migrations** - Database schema management
- **Sentry** - Error tracking and monitoring
- **GitHub Actions** - CI/CD pipeline

## 📋 Prerequisites

- **Docker** and **Docker Compose**
- **PostgreSQL** database
- **.NET 9 SDK**
- **Node.js 18+** and **npm/yarn**
- **(Optional)** Discord Application for OAuth
- **(Optional)** Sentry account for error tracking

## 🚀 Quick start (PowerShell-friendly)

Below are safe, PowerShell-friendly steps to run the project locally for development. These commands assume you're on Windows with PowerShell and have Docker, .NET 9 SDK and Node.js 18+ installed.

### 1) Clone the repository
```powershell
git clone https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource.git
Set-Location UnsecuredAPIKeys-OpenSource
```

### 2) Copy example config files (PowerShell)
Use copies of example files and edit the copies before running. Never commit secrets.
```powershell
Copy-Item UnsecuredAPIKeys.WebAPI/appsettings.example.json -Destination UnsecuredAPIKeys.WebAPI/appsettings.json
Copy-Item UnsecuredAPIKeys.UI/.env.example -Destination UnsecuredAPIKeys.UI/.env.development
Copy-Item UnsecuredAPIKeys.Bots.Verifier/appsettings.example.json -Destination UnsecuredAPIKeys.Bots.Verifier/appsettings.json
```

Edit the copied files and set safe values (use a local-only password and do NOT commit credentials).

### 3) Start a local PostgreSQL (Docker)
```powershell
docker run --name unsecured-api-keys-db `
  -e POSTGRES_DB=UnsecuredAPIKeys `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=ChangeMeLocal123! `
  -p 5432:5432 `
  -d postgres:15
```

Tip: Use a strong local-only password and consider creating a Docker Compose file if you run multiple services.

### 4) Run EF Core migrations (PowerShell)
```powershell
Set-Location UnsecuredAPIKeys.WebAPI
dotnet tool restore
dotnet ef database update --project ../UnsecuredAPIKeys.Data --startup-project .
```

### 5) Run services (each in its own terminal)

WebAPI
```powershell
Set-Location UnsecuredAPIKeys.WebAPI
dotnet run
```

UI
```powershell
Set-Location ..\UnsecuredAPIKeys.UI
npm install
npm run dev
```

Verification bot
```powershell
Set-Location ..\UnsecuredAPIKeys.Bots.Verifier
dotnet run
```

### 6) Access the running app
- UI: http://localhost:3000
- API: http://localhost:7227
- API docs: http://localhost:7227/scalar/v1

If ports differ, check each project's launchSettings.json or logs to confirm the URL/port.

### Environment variables and secrets

- GITHUB_TOKEN: Optional — set this if you want the Scraper bot to access the GitHub API for authenticated requests.

  In PowerShell:

  ```powershell
  $env:GITHUB_TOKEN = 'your_token_here'
  # or set it persistently in Windows environment variables
  ```

  The code reads this variable at startup; do not hardcode tokens in source or commit them to the repository.

Security note: Keep any real API keys, OAuth client secrets, or tokens out of the repository. Use environment variables, a secrets manager, or secured configuration during deployment.

## 📚 Documentation

- **[Deployment Guide](docs/DEPLOYMENT_GUIDE.md)** - Comprehensive deployment instructions
- **[Open Source Cleanup Plan](docs/OPEN_SOURCE_CLEANUP_PLAN.md)** - Details about the open source preparation
- **[API Documentation](docs/API_PROVIDER_BOT_USAGE_GUIDE.md)** - API provider integration guide
- **[UI Implementation Summary](docs/UI_IMPLEMENTATION_SUMMARY.md)** - Frontend architecture overview

## 🏗️ Development

### Project Structure
```
UnsecuredAPIKeys-OpenSource/
├── UnsecuredAPIKeys.WebAPI/          # Main API server
├── UnsecuredAPIKeys.UI/              # Next.js frontend
├── UnsecuredAPIKeys.Data/            # Entity Framework data layer
├── UnsecuredAPIKeys.Providers/       # API provider validation logic
├── UnsecuredAPIKeys.Bots.Verifier/   # Verification bot
├── UnsecuredAPIKeys.Bots.Scraper/    # Scraper bot
├── UnsecuredAPIKeys.Common/          # Shared utilities
└── docs/                             # Documentation
```

### Key Design Patterns
- **Repository Pattern**: Clean data access abstraction
- **Provider Pattern**: Extensible API validation system
- **CQRS**: Separation of read/write operations
- **Event-Driven**: Real-time updates using SignalR
- **Modular Architecture**: Independent, testable components

## 🔧 Configuration

### Environment Variables

#### WebAPI
```bash
CONNECTION_STRING="Host=localhost;Database=UnsecuredAPIKeys;Username=postgres;Password=your_password;Port=5432"
PRODUCTION_DOMAIN="yourdomain.com"
SCRAPER_SERVICE_NAME="api-scraper"
VERIFIER_SERVICE_NAME="api-verifier"
```

#### UI
```bash
NEXT_PUBLIC_API_URL="http://localhost:7227"
NEXT_PUBLIC_GA_MEASUREMENT_ID="YOUR_GA_MEASUREMENT_ID"
SENTRY_ORG="your-sentry-org"
SENTRY_PROJECT="your-sentry-project"
```

### Optional Integrations
- **Discord OAuth**: Enhanced rate limits and user features
- **Google Analytics**: Usage tracking and insights
- **Sentry**: Error tracking and performance monitoring

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Commit your changes**: `git commit -m 'Add amazing feature'`
4. **Push to the branch**: `git push origin feature/amazing-feature`
5. **Open a Pull Request**

### Development Guidelines
- Follow .NET and React best practices
- Include tests for new features
- Update documentation for API changes
- Ensure all builds pass before submitting

## 📝 License

This project is licensed under a **custom attribution-required license** based on MIT - see the [LICENSE](LICENSE) file for complete details.

### ⚠️ IMPORTANT ATTRIBUTION REQUIREMENT

**Any use of this code (even partial) requires UI attribution.** If you use ANY portion of this project in software with a public-facing interface, you MUST:

- Display a link to this GitHub repository in your UI
- Link text should be "Based on UnsecuredAPIKeys Open Source" or similar  
- Link to: `https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource`
- Must be visible on main page or footer

This applies whether you use the entire project, just the backend APIs, validation logic, bots, or any other component. **Removing attribution violates the license and constitutes copyright infringement.**

## ⚖️ Legal and Ethical Considerations

- **Educational Purpose**: This tool is designed for security education and awareness
- **Responsible Use**: Users are responsible for compliance with applicable laws
- **No Warranty**: The software is provided as-is without warranty
- **Ethical Guidelines**: Use only for legitimate security research and education

## 🙏 Acknowledgments

- The open source community for inspiration and tools
- Security researchers who highlight the importance of proper API key management
- Contributors who help improve the project

## 🌐 Domain Available

The domain **unsecuredapikeys.com** is available for sale. If you're interested in acquiring this domain for your own security-focused project or business, please reach out through the GitHub repository.

## 📞 Support

For issues specific to this open source version:
- Check the [Issues](https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource/issues) section
- Create a new issue with detailed information about your setup
- Provide logs and configuration details (without sensitive information)

---

**Remember**: This project is for educational purposes. Always use responsibly and in accordance with applicable laws and regulations.

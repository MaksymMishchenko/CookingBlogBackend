# CookingBlog Backend

## Project Overview
This repository contains the backend for the CookingBlog project, built with C#. The backend provides a RESTful API that the frontend, written in Angular, interacts with. 

**Frontend Repository:** [CookingBlog Frontend](https://github.com/MaksymMishchenko/CookingBlogFrontend)

## Key Features
* **Result Pattern Implementation**: Unified error handling using `Result<T>` for services and controllers.
* **Authentication & Authorization**: Secure access using JWT Tokens.
* **Content Safety:** Advanced HTML sanitization for user-generated content (comments and articles) to prevent XSS attacks.
* **Unified Database**: Integrated Identity and Application contexts into a single PostgreSQL database for streamlined data management.
* **Postman Collection**: Automated API testing scripts with environment variable synchronization.
* **Logging**: Structured logging with Serilog (Console and File sinks).

## Technologies Used
### Core Backend
* **Framework:** .NET 8 (ASP.NET Core)
* **ORM:** Entity Framework Core 8.0
* **Database:** PostgreSQL
* **API Documentation:** Swagger / OpenAPI

## DevOps & Deployment
* **Containerization:** Docker & Docker Compose.
* **Resilience:** Custom Retry Policy for database connectivity and user seeding.
* **Health Monitoring:** Integrated Health Checks to ensure service readiness and proper startup sequencing.

### Security & Authentication
* **Identity:** Microsoft Identity Framework (User management and Role-based access).
* **Tokens:** JWT Bearer Authentication.
* **Content Safety:** [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer) (XSS protection for user posts and comments)
* **Rate Limiting:** ASP.NET Core Rate Limiting Middleware (Fixed Window/Token Bucket policies).

### SEO & Routing
* **Slug Generation:** Custom logic for SEO-friendly URLs (transliteration and normalization).
* **Routing:** Attribute routing with slug constraints.

### Testing & Quality Assurance
* **Unit Testing:** xUnit
* **Integration Testing:** [Testcontainers](https://testcontainers.com/) (spinning up real PostgreSQL instances in Docker for isolated testing)
* **Database Lifecycle:** [Respawn](https://github.com/jbogard/Respawn) (intelligent database state reset between test runs)
* **Data Generation:** [Bogus](https://github.com/bchavez/Bogus) (fake data for seeding and tests)
* **Code Coverage:** Coverlet

### Utilities & Tooling
* **Versioning:** [MinVer](https://github.com/adamralph/minver) (automated versioning based on Git tags)
* **Logging:** Serilog (structured logging)
* **HTML Processing:** HtmlAgilityPack

## Installation & Setup

### Step 1: Clone the repository
```
git clone https://github.com/MaksymMishchenko/CookingBlogBackend.git
cd CookingBlogBackend
```

### Step 2: Database Setup (Choose ONE option)

## Option A: Docker (Recommended)
If you have Docker installed, you can spin up the database instance instantly:

```
docker-compose up -d
```

This starts a PostgreSQL container with all necessary settings. The API is configured to wait for this container to be ready.

## Option B: Manual Installation
Ensure you have PostgreSQL installed and running on your machine. Create a database named cooking_blog.

### Step 3: Configuration & Secrets
Sensitive credentials must be configured via User Secrets (Navigate to the PostApiService/ directory):

Navigate to the `PostApiService/` directory and run:

1. Database Connection:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=cooking_blog;Username=postgres;Password=your_password"
```

2. JWT Authentication:

```
dotnet user-secrets set "JwtConfiguration:SecretKey" "your_secure_32_char_secret_key"
```

(Note: These settings will override any empty values in your local json files during development).

### Step 4: Run the application
 ```
dotnet run --project PostApiService
```
Navigate to http://localhost:7030/swagger to view the Swagger documentation.

### Step 5: Quick Start with Docker Compose

If you prefer to run the entire stack (API + Database) without installing PostgreSQL locally:

```
docker-compose up -d
```

## Resilience & Startup
The API handles infrastructure challenges automatically:

- Connection Retry: Built-in policy (5 attempts) to handle "cold starts" of Docker or Cloud databases (Neon).
- Automated Lifecycle: Every startup applies pending Migrations and seeds Identity/Bogus data (if the DB is empty).
- Zero-Touch Setup: Just run the app — the environment configures itself.
 
## API Documentation & Usage

The API is fully documented using **Swagger/OpenAPI**. It provides an interactive interface to explore and test all available endpoints.

* **Interactive UI**: [https://localhost:7030/swagger](https://localhost:7030/swagger)
* **Specification**: `/swagger/v1/swagger.json`

### Core Business Modules:
Instead of listing every endpoint, here is an overview of the main functional areas:

* **Authentication**: Registration and JWT-based login.
* **Posts Management**: Public feed with pagination, search, and Admin CRUD operations.
* **Comments**: User interactions linked to posts (requires authentication).
* **Categories**: Classification system for culinary recipes.

> **Tip**: Use the included **Postman Collection** in the `/postman` folder for automated testing workflows.

## Project Structure

```
Solution PostApiService/
├── postman/                 # API Collection and environment variables
├── CookingBlogBackend/      # Core solution folder
│   ├── PostApiService/      # Main ASP.NET Core API project
│   │   ├── Contexts/        # DB contexts (Application & Identity)
│   │   ├── Controllers/     # API endpoints and request handling
│   │   ├── Extensions/      # Dependency Injection and Service extensions
│   │   ├── Helper/          # Static utilities and constants
│   │   ├── Infrastructure/  # Core logic (Slug generation, Content safety)
│   │   ├── Interfaces/      # Abstractions for Services and Repositories
│   │   ├── Middlewares/     # Custom HTTP request pipeline logic
│   │   ├── Migrations/      # Entity Framework database migrations
│   │   ├── Models/          # Database entities and DTOs
│   │   ├── Repositories/    # Data access layer (Direct DB operations)
│   │   └── Services/        # Business logic (Result Pattern implementation)
│   └── PostApiService.Tests/# Automated Testing Suite (Integration & Unit)
├── .gitignore               # Git exclusion rules
└── ReadMe.md                # Project documentation                                     

```
## Testing

The project uses **xUnit** for unit and integration testing. 

* **Run Tests**: `dotnet test`
* **Code Coverage**: Managed via **Coverlet** and **ReportGenerator**.
* **To generate a visual coverage report, run the provided test script or use:**

```
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## API Testing

* **Interactive UI**: Swagger at `http://localhost:7030/swagger`
* **Postman Collection**: Import `CookingBlogBackend.postman_collection.json` (located in the root directory) into Postman for automated API testing workflows.
* **Test Data Generation**: Bogus library is used for generating realistic test data.

## Roadmap / To-Do
* **Redis**: Implement caching for popular posts to improve performance.

## Contact
- Author: [Maksym Mishchenko](https://github.com/MaksymMishchenko)
- Email: [mischenkomv@hotmail.com](mailto:mischenkomv@hotmail.com)
- GitHub: [MaksymMishchenko](https://github.com/MaksymMishchenko)
# CookingBlog Backend

## Project Overview
This repository contains the backend for the CookingBlog project, built with C#. The backend provides a RESTful API that the frontend, written in Angular, interacts with. 

**Frontend Repository:** [CookingBlog Frontend](https://github.com/MaksymMishchenko/CookingBlogFrontend)

## Key Features
* **Result Pattern Implementation**: Unified error handling using `Result<T>` for services and controllers.
* **Authentication & Authorization**: Secure access using JWT Tokens.
* **Content Safety:** Advanced HTML sanitization for user-generated content (comments and articles) to prevent XSS attacks.
* **Unified Database**: Integrated Identity and Application contexts for streamlined data management.
* **Postman Collection**: Automated API testing scripts with environment variable synchronization.
* **Logging**: Structured logging with Serilog (Console and File sinks).

## Technologies Used
### Core Backend
* **Framework:** .NET 8 (ASP.NET Core)
* **ORM:** Entity Framework Core 8.0
* **Database:** Microsoft SQL Server
* **API Documentation:** Swagger / OpenAPI

### Security & Authentication
* **Identity:** Microsoft Identity Framework
* **Tokens:** JWT Bearer Authentication
* **Content Safety:** [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer) (XSS protection for user posts and comments)

### Testing & Quality Assurance
* **Unit Testing:** xUnit & [FluentAssertions](https://fluentassertions.com/)
* **Integration Testing:** [Testcontainers](https://testcontainers.com/) (spinning up real SQL Server instances in Docker for isolated testing)
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

### Step 2: Configure the application
1. **Settings**: Copy `appsettings.Example.json` to `appsettings.json` in the `PostApiService/` directory.
2. **Connection String**: Update the `DefaultConnection` in `appsettings.json` with your local SQL Server instance details.
3. **Security**: Set a unique `SecretKey` (at least 32 characters) in the `JwtConfiguration` section.

### Step 3: Create and Migrate Database
Run the following command to set up the database:
```
dotnet ef database update
```
  
### Step 4: Run the application
1. Start the application:
 ```
dotnet run
```

2. Navigate to http://localhost:7030/swagger to view the Swagger documentation.
 
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
├── Solution Items/         # API Collection for automated testing
├── PostApiService/         # Main Backend Application
│   ├── Contexts/           # Unified Database Context (Identity & Application)
│   ├── Controllers/        # API Endpoints and Routing
│   ├── Models/             # Domain Entities and DTOs
│   ├── Repositories/       # Data Access Layer (Direct DB operations)
│   ├── Services/           # Business Logic Layer (Result Pattern implementation)
│   └── appsettings.Example.json # Template for local configuration
└── PostApiService.Tests/   # Unit and Integration Tests                                         

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
* **Security Improvements**: Implement `RateLimiting` for API throttling.
* **Comment System**: Implement an endpoint to retrieve a paginated list of comments by Post ID.
* **Post Management (Admin)**: 
    * Add filtering/sorting for posts by active and inactive status (for frontend display).
    * Implement dedicated admin endpoints to retrieve all posts (including inactive/drafts).
* **SEO & Routing**: Implement a method in `PostService` to retrieve posts by **Slug** for SEO-friendly URLs.
* **Docker**: Containerize the application using multi-stage builds.
* **Redis**: Implement caching for popular posts to improve performance.

## Contact
- Author: [Maksym Mishchenko](https://github.com/MaksymMishchenko)
- Email: [mischenkomv@hotmail.com](mailto:mischenkomv@hotmail.com)
- GitHub: [MaksymMishchenko](https://github.com/MaksymMishchenko)
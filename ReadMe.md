
# CookingBlog Backend

## Project Overview

This repository contains the backend for the CookingBlog project, built with C#. The backend provides a RESTful API that the frontend, written in Angular, interacts with. The API handles all data processing, storage, and business logic for the CookingBlog application.
The frontend repository can be found here: [CookingBlog Frontend](https://github.com/MaksymMishchenko/CookingBlogFrontend)
Ensure that the backend is properly configured to handle incoming requests from the frontend, including setting up the necessary API routes, authentication, and database connections.

## Description

CookingBlog is a culinary blog where users can share recipes and cooking experiences. The backend of the project is built with C# and provides a RESTful API that supports the functionality for creating, editing, and deleting posts. It also handles user authentication, data storage, and communication with the frontend.
The frontend interacts with this API to display a list of posts, submit new posts, and update or delete existing ones. The backend ensures data consistency, security, and business logic for the CookingBlog application.

## Features
 - **Authentication and Authorization:** JWT Token
 - **CRUD operations:** Manage data with Entity Framework Core
 - **Logging:** Integrated with Serilog for structured logging
 - **Swagger:** API documentation for ease of use
 - **Unit Testing:** Covers APIs and services with XUnit

## Technologies Used
 - **ASP.NET Core 8.0**
 - **Entity Framework Core 8.0**
 - **SQL Server**
 - **Swagger/OpenAPI**
 -**XUnit** (for testing)

## Installation

### Step 1: Clone the repository
 `git clone https://github.com/MaksymMishchenko/CookingBlogBackend.git
  cd CookingBlogBackend`

### Step 2: Configure the database
1. Open or create the appsettings.json file and update the database connection strings:
### `appsettings.json` Template

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server_instance;Database=YourAppDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;",
    "IdentityConnection": "Server=your_server_instance;Database=YourAppIdentityDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "your_strong_secret_key_32_chars_minimum",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "ExpiryInMinutes": 15,
    "RefreshTokenExpiryInDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

2. Apply migrations to create the database tables:

`dotnet ef database update`
  
### Step 3: Run the project
1. Start the application:

`dotnet run`

2. Navigate to http://localhost:5000/swagger to view the Swagger documentation.
 
## How to Use
### REST API
The project provides an API for managing resources. Below are examples of key endpoints:

### Key Endpoints Summary

| Controller | Method | Endpoint | Description | Auth Required |
| :--- | :---: | :--- | :--- | :---: |
| **Auth** | POST | `/api/auth/register` | Register new user | No |
| **Auth** | POST | `/api/auth/login` | Get JWT Token | No |
| **Category** | GET | `/api/category` | List all culinary categories | No |
| **Category** | GET | `/api/category/{id}` | Specific culinary category | Yes (Admin) |
| **Category** | POST | `/api/category` | Create a new category | Yes (Admin) |
| **Category** | PUT | `/api/category/{id}` | Update a new category | Yes (Admin) |
| **Category** | DELETE | `/api/category/{id}` | Delete specific category | Yes (Admin) |
| **Posts** | GET | `/api/posts?pageNumber={int}&pageSize={int}` | Get paginated list of posts | No |
| **Posts** | GET | `/api/posts/search?queryString={test}&pageNumber={int}&pageSize={int}` | Get paginated search list & search | No |
| **Posts** | GET | `/api/posts/{id}` | Get specific post | No |
| **Posts** | POST | `/api/posts` | Create a new blog post | Yes (Admin) |
| **Posts** | PUT | `/api/posts/{id}` | Update specific post |  Yes (Admin) |
| **Posts** | DELETE | `/api/posts/{id}` | Delete specefic post |  Yes (Admin) |
| **Comments** | POST | `/api/comments/{postid}` | Add a comment to a post |  Yes (Admin) |
| **Comments** | PUT | `/api/comments/{id}` | Update specific comment |  Yes (Admin) |
| **Comments** | DELETE | `/api/comments/{id}` | Remove a comment |  Yes (Admin) |

> **Note:** For all protected endpoints, include the header `Authorization: Bearer <your_jwt_token>`.

## Project Structure

```
Solution PostApiService/
│
├── Solution Items/                    
│   ├── CookingBlogBackend.postman_collection.json  // Postman collection file for API testing, providing an alternative to Swagger for testing endpoints.
│
├── PostApiService/                    // Main project containing the API implementation
│   ├── Properties/                    // Auto-generated properties folder (e.g., launch settings)
│   ├── Contexts/                      // Contains Entity Framework database context for managing database interactions
│   ├── Controllers/                   // API controllers handling HTTP requests and routing
│   ├── Exceptions/                    // Handling exceptions and error responses in API controllers
│   ├── Extensions/                    // Result extensions etc
│   ├── Helper/                        // Utility classes or methods to support business logic
│   ├── Images/                        // Directory for storing images related to posts or other resources
│   ├── Infrastructure/                // Core infrastructure-related code (e.g., dependency injection setup, configurations)
│   ├── Interfaces/                    // Interface definitions for services and repositories, ensuring abstraction and testability
│   ├── Middlewares/                   // Implementations of middlewares for handling HTTP requests and responses
│   ├── Migrations/                    // Database migrations created via Entity Framework for schema evolution
│   ├── Models/                        // Data models representing entities and request/response DTOs
│   ├── Repositories/                  // Data access layer implementation (Direct database operations)
│   ├── Services/                      // Business logic implementation for handling operations
│   ├── appsettings.json               // Configuration file for app settings, including connection strings and other configurations
│   ├── GlobalUsings.cs                // Centralized global using directives to keep code files clean
│   ├── Program.cs                     // Entry point for the application, setting up middleware, services, and configurations
│   ├── SeedData.cs                    // Initial database population with sample categories, posts, and users
                                           

```
# Testing

## Project Structure

```
Solution PostApiService/
│
├── PostApiService.Tests/                     // Testing project                              
│   ├── Fixtures/                             // Test fixtures
│   ├── Helper/                               // Test utilities
│   ├── IntegrationTests/                     // Integration tests
│   │   ├── Controllers/                      // Integration tests for API controllers
│   │   ├── Middlewares/                      // Integration tests for middlewares
│   │   ├── Services/                         // Integration tests for services
│   ├── UnitTests/                            // Unit tests
│   │   ├── Controllers/                      // Unit tests for API controllers
│   │   ├── Extensions/                       // Unit tests for Result extension
│   │   ├── Filters/                          // Unit tests for endpoint filters
│   │   ├── Services/                         // Unit tests for services
│   ├── Usings.cs                             // Global using directives for tests
                         
```

**Run Tests**
To run unit tests, use the following command:

`dotnet test`

**Tests Covarage Tools**
**Test Coverage and Report Generation for Visual Studio Community**

The Analyze Code Coverage for All Tests feature is unavailable in Visual Studio Community Edition. Follow these steps to generate code coverage reports for free:

**Step 1: Add Coverlet Dependency**
Add the Coverlet collector to your test project:

`dotnet add package coverlet.collector`

**Step 2: Run Tests and Collect Coverage**
Run the following command to execute tests and generate coverage data:

`dotnet test --collect:"XPlat Code Coverage"`

Coverage data will be saved in the TestResults folder as coverage.cobertura.xml.

**Step 3: Generate an HTML Report**
Use the reportgenerator tool to convert coverage data into an HTML report:

Install the tool (if not already installed):

`dotnet tool install --global dotnet-reportgenerator-globaltool`

Generate the report:

`reportgenerator -reports:"<path-to-coverage-file>" -targetdir:"coverage-report" -reporttypes:Html`

Replace <path-to-coverage-file> with the path to coverage.cobertura.xml.

Open the HTML file in the coverage-report folder to view the results.

**Key Metrics**

Focus on:

Line Coverage: Percentage of lines executed by tests.
Branch Coverage: Percentage of conditional branches tested.
This method ensures comprehensive test coverage without needing Visual Studio Enterprise.

**Postman Collection for Testing**
A Postman collection named CookingBlogBackend.postman_collection is included in the Solution Items folder. You can use this collection to test the API endpoints directly, in addition to testing through Swagger or using the test coverage tools mentioned above.

To use the collection:

- Import the file into Postman.
- Adjust environment variables as needed.
- Run the collection to interact with the API endpoints.

This provides an alternative testing method for verifying API functionality.

## To-Do 
- **Architecture Refactoring (PostService):** Implement the **Result Pattern** (`Result<T>`) to improve error handling, decouple business logic from HTTP responses, and ensure consistent API outcomes.
- **Docker:** Containerize the application using multi-stage builds for easier deployment.
- **Redis:** Implement caching for popular posts to improve performance.

## Contact
- Author: [Maksym Mishchenko](https://github.com/MaksymMishchenko)
- Email: [mischenkomv@hotmail.com](mailto:mischenkomv@hotmail.com)
- GitHub: [MaksymMishchenko](https://github.com/MaksymMishchenko)
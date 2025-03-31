
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

### 1. Register User
**Endpoint: POST /api/Auth/register**

**Request body:**
```
json
{
  "username": "user@example.com",
  "email": "test@mail.com",
  "password": "password123"
}
```

**Response body:**
```
json
{
    "success": true,
    "message": "User with username {userName} registered successfully."
}
```

### 2. Authentication
**Endpoint: POST /api/Auth/login**

**Request body:**
```
json
{
  "username": "user@example.com",
  "password": "password123"
}
```

**Response body:**
```
json
{
    "success": true,
    "message": "User with username {username} logged in successfully",
    "token": "eyJhbGciOiJIU..."
}
```

### 3. Comments
**Endpoint: POST /api/Comments/{postId}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{  
  "author": "Bob",
  "content": "Lorem ipsum dolor sit amet"
}
```

**Response body:**
```
json
{
    "success": true,
    "message": "Comment added successfully."
}
```
**Validation errors**

json
```
{
  "success": false,
  "message": "Validation errors occurred.",
  "errors": [
    { "field": "author", "message": "Author name exceeds maximum length of 50 characters." },
    { "field": "content", "message": "Content is required." }
  ]
}
```

**Endpoint: PUT /api/Comments/{commentId}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{  
  "content": "This is a sample comment with enough length."
}
```

**Response body:**
```
{
    "success": true,
    "message": "Comment updated successfully."
}
```
**Validation errors**

json
```
{
    "success": false,
    "message": "Validation failed.",
    "errors": [
        "Content must be between 10 and 500 characters."
    ]
}
```

**Endpoint: DELETE /api/Comments/{commentId}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
`json
{}`

**Response body:**
json
```
{
    "success": true,
    "message": "Comment deleted successfully."
}
```

### 4. Posts

**Endpoint: GET /api/posts?pageNumber=1&pageSize=10&commentPageNumber=1&commentsPerPage=10&includeComments=true**

**Request body:**
`json
{}`

**Response body:**
```
json
{
    "success": true,
    "message": "Successfully retrieved 2 posts.",
    "dataList": [
        {
            "postId": 1,
            "title": "First Post",
            "description": "Description for first post",
            "content": "This is the content of the first post.",
            "author": "Peter Jack",
            "createAt": "2025-03-31T18:22:44.1494039",
            "imageUrl": "/images/placeholder.jpg",
            "metaTitle": "Meta title info",
            "metaDescription": "This is meta description",
            "slug": "http://localhost:4200/first-post",
            "comments": [
                {
                    "commentId": 2,
                    "author": "Jane Doe",
                    "content": "I totally agree with this!",
                    "createdAt": "2025-03-31T18:22:44.954684",
                    "postId": 1,
                    "userId": "testUserId"
                },
                {
                    "commentId": 4,
                    "author": "Maks",
                    "content": "Content must be at least 10 symbols",
                    "createdAt": "2025-03-31T18:27:00.9048119",
                    "postId": 1,
                    "userId": "f1b207f3-1a6a-4862-b377-4089fb59d803"
                }
            ]
        },
        ...
    ]
}
```

**Endpoint: GET /api/posts/1?includeComments=true**

**Request body:**
`json
{}`

**Response body:**
```
json
{
    "success": true,
    "message": "Post with ID 1 retrieved successfully.",
    "data": {
        "postId": 1,
        "title": "First Post",
        "description": "Description for first post",
        "content": "This is the content of the first post.",
        "author": "Peter Jack",
        "createAt": "2025-03-31T18:22:44.1494039",
        "imageUrl": "/images/placeholder.jpg",
        "metaTitle": "Meta title info",
        "metaDescription": "This is meta description",
        "slug": "http://localhost:4200/first-post",
        "comments": [
            {
                "commentId": 2,
                "author": "Jane Doe",
                "content": "I totally agree with this!",
                "createdAt": "2025-03-31T18:22:44.954684",
                "postId": 1,
                "userId": "testUserId"
            },
            ...
        ]
    }
}
```

**Endpoint: POST /api/Posts**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{
    "title": "New test post title",
    "description": "This is a sample description for the post.",
    "content": "This is the detailed content of the post. It provides in-depth information about the topic.",
    "author": "Peter",
    "imageUrl": "https://example.com/sample-image.jpg",
    "metaTitle": "Sample Meta Title",
    "metaDescription": "This is a sample meta description for SEO purposes.",
    "slug": "sample-post-title-1"
}
```

**Response body**
```
json
{
    "success": true,
    "message": "Post added successfully.",
    "entityId": 3
}
```

**Endpoint: PUT api/posts**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{
    "postId": 2,
    "title": "Updated post title",
    "description": "This is a sample description for the post.",
    "content": "This is the detailed content of the post. It provides in-depth information about the topic.",
    "author": "John Doe",
    "imageUrl": "https://example.com/changed-image.jpg",
    "metaTitle": "Sample changed Meta Title",
    "metaDescription": "This is a changed sample meta description for SEO purposes.",
    "slug": "sample-post-title-changed"
}
```

**Response body:**
```
json
{
    "success": true,
    "message": "Post with ID 2 updated successfully.",
    "entityId": 2
}
```

**Endpoint: DELETE /api/Posts/{Id}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
`json
{}`

**Response body:**
```
{
    "success": true,
    "message": "Post with ID 2 deleted successfully.",
    "entityId": 2
}
```

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
│   ├── Helper/                        // Utility classes or methods to support business logic
│   ├── Images/                        // Directory for storing images related to posts or other resources
│   ├── Infrastructure/                // Core infrastructure-related code (e.g., dependency injection setup, configurations)
│   ├── Interfaces/                    // Interface definitions for services and repositories, ensuring abstraction and testability
│   ├── Middlewares/                   // Implementations of middlewares for handling HTTP requests and responses
│   ├── Migrations/                    // Database migrations created via Entity Framework for schema evolution
│   ├── Models/                        // Data models representing entities and request/response DTOs
│   ├── Services/                      // Business logic implementation for handling operations
│   ├── appsettings.json               // Configuration file for app settings, including connection strings and other configurations
│   ├── Program.cs                     // Entry point for the application, setting up middleware, services, and configurations
                                           

```
# Testing

## Project Structure

```
Solution PostApiService/
│
├── PostApiService.Tests/                     // Testing project
│   ├── coverage-report/                      // Test coverage reports                               
│   ├── Fixtures/                             // Test fixtures
│   ├── Helper/                               // Test utilities
│   ├── IntegrationTests/                     // Integration tests
│   │   ├── Controllers/                      // Integration tests for API controllers
│   │   ├── Services/                         // Integration tests for services
│   ├── UnitTests/                            // Unit tests
│   │   ├── Controllers/                      // Unit tests for API controllers
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
- **Docker** (for containerization)

## Contact
- Author: [Maksym Mishchenko](https://github.com/MaksymMishchenko)
- Email: [mischenkomv@hotmail.com](mailto:mischenkomv@hotmail.com)
- GitHub: [MaksymMishchenko](https://github.com/MaksymMishchenko)
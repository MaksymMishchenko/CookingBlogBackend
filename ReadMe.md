
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
1. Open or create the appsettings.json file and update the database connection string:

`"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDatabase;User Id=sa;Password=your_password;"
}`

2. Apply migrations to create the database tables:

`dotnet ef database update`
  
### Step 3: Run the project
1. Start the application:

`dotnet run`

2. Navigate to http://localhost:5000/swagger to view the Swagger documentation.
 
## How to Use
### REST API
The project provides an API for managing resources. Below are examples of key endpoints:

### 1. Authentication
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
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  expires: "2025-01-06T22:21:58.206517+02:00"
}
```

### 2. Comments
**Endpoint: POST /api/Comments/posts/{postId}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{
  "commentId": 0,
  "author": "string",
  "content": "stringstri",
  "createdAt": "2025-01-06T19:57:05.423Z",
  "postId": 0
}
```

**Response body:**

`true`

**Endpoint: PUT /api/Comments/{commentId}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{
  "commentId": 1,
  "author": "Bob",
  "content": "updated comment",
  "createdAt": "2025-01-06T20:00:02.719Z",
  "postId": 1
}
```

**Response body:**

`Updated successfully`

**Endpoint: DELETE /api/Comments/{commentId}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
`json
{
  "id": 1
}`

### 3. Posts

**Endpoint: GET /api/Posts/GetAllPosts**

**Request body:**
`json
{
  "pageNumber": 1,
  "pageSize": 10,
  "commentPageNumber": 1,
  "commentsPerPage": 1,
  "includeComments": true
}`

**Response body:**
```
json
[
  {
    "postId": 1,
    "title": "First Post",
    "description": "Description for first post",
    "content": "This is the content of the first post.",
    "author": "Peter Jack",
    "createAt": "2024-11-23T18:16:57.5541777",
    "imageUrl": "/images/placeholder.jpg",
    "metaTitle": "Meta title info",
    "metaDescription": "This is meta description",
    "slug": "http://localhost:4200/first-post",
    "comments": [
      {
        "commentId": 1,
        "author": "Jane Doe",
        "content": "I totally agree with this!",
        "createdAt": "2024-11-23T18:16:58.5548371",
        "postId": 1
      },
      ...
    ]
  },
  ...
]
```

**Endpoint: GET /api/Posts/GetPost/{postId}**

**Request body:**
```
json
{
  "postId": 1,
  "title": "First Post",
  "description": "Description for first post",
  "content": "This is the content of the first post.",
  "author": "Peter Jack",
  "createAt": "2024-11-23T18:16:57.5541777",
  "imageUrl": "/images/placeholder.jpg",
  "metaTitle": "Meta title info",
  "metaDescription": "This is meta description",
  "slug": "http://localhost:4200/first-post",
  "comments": [
    {
      "commentId": 2,
      "author": "Jane Doe",
      "content": "I totally agree with this!",
      "createdAt": "2024-11-23T18:16:58.5548371",
      "postId": 1
    },
    ...
  ]
}
```

**Endpoint: POST /api/Posts/AddNewPost**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{
  "postId": 0,
  "title": "Some title here",
  "description": "Some desc here",
  "content": "Some content here",
  "author": "Michael",
  "createAt": "2025-01-06T20:19:00.733Z",
  "imageUrl": "src/img/img.jpg",
  "metaTitle": "Some meta title",
  "metaDescription": "Some meta desc",
  "slug": "some-post-slug",
  "comments": [
    {
      "commentId": 1,
      "author": "Kevin",
      "content": "Some comment text",
      "createdAt": "2025-01-06T20:19:00.740Z",
      "postId": 1
    }
  ]
}
```

**Response body**
```
json
{
    Success = true,
    postId = 1,
    Message = "Post added successfully."
}
```

**Endpoint: PUT /api/Posts/{id}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
```
json
{
  "postId": 1,
  "title": "Some title here",
  "description": "Some desc here",
  "content": "Some content here",
  "author": "Michael",
  "createAt": "2025-01-06T20:19:00.733Z",
  "imageUrl": "src/img/img.jpg",
  "metaTitle": "Some meta title",
  "metaDescription": "Some meta desc",
  "slug": "some-post-slug"
}
```

**Response body:**
`Updated post successfully`

**Endpoint: DELETE /api/Posts/{Id}**

**Headers:**

`Authorization: Bearer <your_token>`

**Request body:**
`json
{
  "id": 1
}`

**Response body**
-

## Project Structure

```
Solution PostApiService/
  ├── PostApiService/         // ASP.NET Core project
  ├── Controllers/        // API controllers
  ├── Images/             // Posts images
  ├── Interfaces/         // Interfaces
  ├── Migrations/         // API controllers
  ├── Models/             // Data models
  ├── Services/           // Business logic (services)
  ├── Middleware/         // Middleware components
  ├── appsettings.json    // Application configuration
  ├── Program.cs          // Application entry point
```

## Testing
**Run Tests**
To run unit tests, use the following command:

`dotnet test`

## To-Do
- **Registration**
- **Global Exception Handling Middleware:** Centralized error handling
- **Docker** (for containerization)

## Contact
- Author: [Maksym Mishchenko](https://github.com/MaksymMishchenko)
- Email: [mischenkomv@hotmail.com](mailto:mischenkomv@hotmail.com)
- GitHub: [MaksymMishchenko](https://github.com/MaksymMishchenko)
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Implementation of `HtmlSanitizer` to protect comment and post content from XSS attacks (in progress).
- Implementation of `RateLimiting` to protect API from brute-force and spam (in progress).

## [1.0.0] - 2026-01-15
### Added
- **Result Pattern Implementation**: 
    - Introduced a unified `Result<T>` and non-generic `Result` pattern for error handling across `PostService` and `CommentService`.
    - Split the `Result` class into generic and non-generic versions for better type safety.
- **DTOs & Mapping**: 
    - Implemented specialized DTOs for comments (`CommentCreatedDto`, `CommentUpdatedDto`).
    - Added extension methods for clean entity-to-DTO transformation.
- **Security & Data Tracking**:
    - Added `IsEditedByAdmin` property to the `Comment` model to track administrative changes.
    - Added `IsActive` status to the `Post` model.
- **Database & Architecture**:
    - Merged `Identity` and `Application` contexts into a single unified database for simplified management.
    - Decoupled data access by introducing `CommentRepository` and `PostRepository`.
- **Project Setup & Documentation**:
    - Added a comprehensive **Postman Collection** with automated test scripts (saving IDs for sequential requests).
    - Added `appsettings.Example.json` with configuration placeholders for local deployment.
    - Created the initial project documentation and `README.md`.

### Changed
- **Service Refactoring**: 
    - Refactored `CommentService` and `PostService` logic to return `Result` objects instead of throwing custom exceptions.
    - Updated integration tests for `PostService` using `SetupAsync` for more reliable test data initialization.

### Fixed
- Resolved Postman script issues related to JSON parsing and environment variable synchronization.
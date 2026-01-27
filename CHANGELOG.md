# [2.2.0](https://github.com/MaksymMishchenko/CookingBlogBackend/compare/v2.1.0...v2.2.0) (2026-01-27)


### Features

* update release workflow and fix changelog generation ([718e100](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/718e1003e0b7958bc91ea69fd26252c8ad55150b))



# [2.1.0](https://github.com/MaksymMishchenko/CookingBlogBackend/compare/v1.0.0...v2.1.0) (2026-01-27)


* feat!: refactor authentication to Result pattern and update API to v2.0.0 ([c8f4446](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/c8f4446764956ed0c8c31003c51afae637269979))


### Features

* implement automatic data auditing with UpdatedAt timestamp ([9945c95](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/9945c9554b3a94339592dc89d430bd6f273cef1f))
* overhaul integration tests with Testcontainers, Respawn, and Docker support ([9e90f14](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/9e90f14011d8f3c7b9741a6a981b81726d36453e))
* **security:** Implement HTML sanitization to ensure malicious scripts are stripped from user comments. ([6e2cd99](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/6e2cd999b16d16e8732a70db4c9862ec61f4dbbf))
* **security:** implement HTML Sanitizer and content stripping for posts ([7ada6d4](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/7ada6d4cc54a6b9c5ae5089c0d1f7daa4ee61a4a))
* **security:** implement HtmlSanitizationService and infrastructure ([5d73298](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/5d73298e7dfd358294c343f8f6655b09f7069702))


### BREAKING CHANGES

* Authentication endpoints now use a unified Result pattern, wrapping responses in DTOs and changing the structure of the JSON response. Existing clients must be updated to handle the new data envelope.

- Refactored AuthService (Register/Login) to use Result<T> instead of throwing exceptions.
- Implemented dedicated DTOs (LoginUserDto, RegisteredUserDto, etc.) for cleaner data transfer.
- Updated project version to 2.0.0 in .csproj (Assembly and File versions).
- Standardized error handling in GlobalExceptionMiddleware and AuthController.
- Added comprehensive unit and integration tests for new authentication flows.
- Updated Postman collection and removed obsolete message constants.
- Logged technical debt for future TransactionScope and Account Lockout implementations.



# [1.0.0](https://github.com/MaksymMishchenko/CookingBlogBackend/compare/c75bfb662681305e32292a69faea69b34eab4c2d...v1.0.0) (2026-01-15)


### Bug Fixes

* Added postId to URL in UpdatePost endpoint path to fix failing test ([d8db3c6](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/d8db3c6592f76ff548fa62bafc82dd35b922a2cf))
* fix broken unit tests for PostService ([96f8730](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/96f8730819ebd9c19e66fad734521cc5219aa7c0))
* Fix integration tests for AddCommentAsync endpoint in ExceptionMiddlewareTests. ([11ae252](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/11ae2524a79aa7c6b3b2680354f91553833dad83))
* fixed integration tests for CommentService methods ([55674ae](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/55674ae1ed20813352ba8738f8b75cb1c3737bb1))
* Pass postId to service method and fix related tests ([c8781ac](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/c8781acfbeca2891f538f165d871f42e384dfab6))


### Features

* add AnyAsync to IRepository and implement in Repository ([70dbd14](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/70dbd1453cacda5b044bb0eda617cc6a8481836d))
* Add commentId validation attribute to DeleteCommentAsync method. Removed unnecessary tests. ([3143a90](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/3143a9091716796b327c1277463ee1c2fc211415))
* add PostQueryParameters DTO for cleaner query parameter handling in GetAllPostsAsync ([bf3e289](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/bf3e2890201ffeb8ab911c2e4580c1bf04fe197d))
* add query parameter validation filter with tests ([9ea415b](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/9ea415b9f9c9cbd5e53136c667cfd2acf8143c44))
* Add validation attribute to AddCommentAsync endpoint. ([29a0197](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/29a0197198eabaee8c48c3d6016b839b63255067))
* Added GlobalExceptionMiddleware with logging and exception handling ([e439d3d](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/e439d3d12f52e7af72128ed26be5fb8e07d9e8c5))
* Apply commentId validation attribute. Remove unnecessary tests. ([e771c59](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/e771c5977bbeaa49c02b782b6ea42a7a835f30ff))
* **auth:** Enhance authorization for CommentController endpoints ([ee8c87d](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/ee8c87d3cc4ebd6e45d1114a25048a6dcdf7582c))
* **auth:** implement AddClaimsAsync in AuthRepository and fix parameter in interface ([035125c](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/035125cce0a728bd813daca3c8153c61ca8e69fb))
* **auth:** implement CheckPasswordAsync to validate user password ([61527aa](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/61527aa54f06a2de5857745ead165659f5ab3a07))
* **auth:** implement CreateAsync in AuthRepository and fix return type bug ([64a34af](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/64a34afda5a319a712a35f63c54e001f6c536f9f))
* **auth:** implement GetClaimsAsync to retrieve user claims ([7e5d49a](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/7e5d49a664524d064eac711484d5d498751e4c4c))
* **auth:** implement GetRolesAsync to retrieve roles of user ([041af20](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/041af2091afd3e4cfc18f4704cf537650cc074ea))
* **auth:** implement GetUserAsync to retrieve user from HTTP context. ([15faaf4](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/15faaf4572aa2fa734ab594563771d9117c3dcf8))
* **auth:** implement LoginUser with custom errors and exception handling ([420b359](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/420b3590071c0fa7932c04486660a1846ddc53ac))
* **auth:** implement RegisterUser endpoint in AuthController ([1ea36e2](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/1ea36e2b746705f54f79e2d34aaf9f21781a6ef0))
* **category:** implement category management and update project docs ([ed3dd73](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/ed3dd73c9451a8bd99414642ed56f86df1823ca7))
* **category:** implement category service with full CRUD and testing ([85654b0](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/85654b08ccbbfeb4370836491b17b80a0415fd97))
* **category:** implement CRUD logic, controller endpoints, and Result pattern ([#15](https://github.com/MaksymMishchenko/CookingBlogBackend/issues/15)) ([9ed8ac3](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/9ed8ac3b6b04d1f047b95a52c35f41fb7ec6f4f2))
* **comment:** add UserId property to Comment model ([f94363b](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/f94363b03ea03921b8253a7c46a4d723c0f6d537))
* **comment:** refactor comment management to Result pattern and specialized DTOs ([4d4ee4a](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/4d4ee4af4fc0016c5a9aef1e3c3ed5d8184f554a))
* **filter:** implement unified endpoint validation system and refactor API responses ([d5eb308](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/d5eb308620f6a525fe145a0861d6c4c551e0b475))
* Implement a filter to validate the incoming 'id' parameter and return an error if invalid. ([cca8662](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/cca8662e5118cc0f07f7d2bc106efa03fe027e84))
* Implement attribute to validate if query parameter PostId matches PostId in Comment object. ([509dee7](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/509dee776b9549dba2029d8b82900a51836e77e2))
* Implement generic ApiResponse<T> and update controllers & tests ([ef7d4f3](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/ef7d4f3e1470cbf30b455867d2694a17862e5965))
* implement Serilog logging with console and file sinks ([fc898eb](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/fc898eba281118dc48c6ce3107a560f0fae656e9))
* improve ValidateModelAttribute with custom responses per model ([b302473](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/b302473989cee3097a11342f3259c741b35dd94d))
* introduce custom exception AddPostFailedException ([1906daa](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/1906daa831067e3a0022f315d766d4789f1d1b90))
* Paginated Search with Keyword Highlighting and Model Hardening ([#13](https://github.com/MaksymMishchenko/CookingBlogBackend/issues/13)) ([79f75d0](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/79f75d0d6b9bb30083e4c35cb08780590fa5153d))
* **post, test:** Implement paginated post retrieval and refactor testing infrastructure ([1c993fc](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/1c993fcffd44b7f37033eef46c522f4e3dcf7b86))
* **post, test:** Implement paginated post retrieval and refactor testing infrastructure ([e2aa892](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/e2aa892ebfe2c60d6162b5916aec10267e6d1426))
* **post:** add categories support and update database schema ([449bf66](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/449bf668bf34bc281db44f78e2244dc6da6b420c))
* **post:** add categories, update schema, and fix integration tests ([#14](https://github.com/MaksymMishchenko/CookingBlogBackend/issues/14)) ([44025ea](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/44025ea08e11d9237997c2bf3b097685056eb8f0))
* **PostController:** add pagination support to ApiResponse and update posts endpoint ([a278c48](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/a278c48781390fd80b97bb13687b21bdce88ec2b))
* **post:** implement Result pattern, IsActive status and refactor PostService ([bdddd6b](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/bdddd6b17eb6d48316b24317bb795f9a3ecd55b2))
* **PostService:** Implement paged retrieval with total count ([bc7b6fb](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/bc7b6fb2f7fa38ae90615917588e8d2c41937110))
* **PostService:** Service Refactoring, Query Simplification, and DB Schema Enforcement ([988915f](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/988915f90157d369bb86438e5504cde21a80fda2))
* **posts:** implement paginated search with keyword highlighting ([dd25642](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/dd25642dc1f9d87c21f22c4267f32b7177355013))
* **post:** Standardize Post update flow and fix related tests ([c9e43f5](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/c9e43f54ed067e2beec7560fc1d091431257e6b1))
* **Repository:** Implement GetTotalCountAsync to retrieve total post count ([c5cf923](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/c5cf9235eeb93cfe8c9caf61350d392a0ce22cd2))
* **seed-data:** Add Bogus library and implement post/comment seeding logic ([b4a9e6c](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/b4a9e6cdb5db4929d645144b4587fefd63f134fa))
* **tests:** Add BaseTestFixture and PostFixture for integration tests ([04cc447](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/04cc447d1cbda9d3efce40c794ff0b05c720588d))
* **tests:** Add CommentFixture for integration tests ([1974bfd](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/1974bfd913745111d1bf6cf93034a37001be9e22))
* **tests:** Add mock services and ExceptionMiddlewareTests fixture ([ba93705](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/ba9370569a075d1963ade1030d231a930abb9c3e))
* Update Comment model and database configuration ([c75bfb6](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/c75bfb662681305e32292a69faea69b34eab4c2d))
* Update UpdatePostAsync to accept 'id' as a query parameter and applied ID validation filter ([0ae9419](https://github.com/MaksymMishchenko/CookingBlogBackend/commit/0ae9419d25ba047ad731becfe1301ad036f02c6e))


### BREAKING CHANGES

* **post:** Service methods now return `Result` type instead of
raw objects or throwing custom exceptions.
* **PostService:** / DETAILS:
* **PostService:** Renamed method (e.g., from `GetPostsAsync` to `GetPostsWithTotalAsync`) and changed its contract to return a tuple `(List<Post> Posts, int TotalCount)`.
* **Repository:** Utilized the new `GetTotalCountAsync` method for fetching the total count within the service logic.
* **Tests:** Updated all corresponding unit tests to reflect the new tuple return type and correctly assert both the paginated list (`result.Posts`) and the total count (`result.TotalCount`).




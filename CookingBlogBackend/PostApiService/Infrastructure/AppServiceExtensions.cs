using PostApiService.Models.TypeSafe;

namespace PostApiService.Infrastructure
{
    public static class AppServiceExtensions
    {
        public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ClaimBasedPolicy", policy =>
                {
                    policy.RequireClaim("Post");
                });

                options.AddPolicy(TS.Policies.FullControlPolicy, policy =>
                {
                    policy.RequireClaim(TS.Controller.Post,
                        TS.Permissions.Write.ToString(),
                        TS.Permissions.Delete.ToString());
                });

                options.AddPolicy(TS.Policies.ReadAndWritePolicy, policy =>
                {
                    policy.RequireClaim(TS.Controller.Post,
                        TS.Permissions.Write.ToString());
                });

                options.AddPolicy("ClaimBasedCommentPolicy", policy =>
                {
                    policy.RequireClaim("Comment");
                });

                options.AddPolicy(TS.Policies.FullControlPolicy, policy =>
                {
                    policy.RequireClaim(TS.Controller.Comment,
                        TS.Permissions.Write.ToString(),
                        TS.Permissions.Update.ToString(),
                        TS.Permissions.Delete.ToString());
                });
            });
            return services;
        }

        /// <summary>
        /// Configures CORS (Cross-Origin Resource Sharing) to allow requests from specific origins.
        /// In this case, it allows requests from "http://localhost:4200".
        /// This is useful to enable cross-origin requests between the frontend (Angular) and backend (API).
        /// </summary>
        /// <param name="services">The IServiceCollection to register the CORS policy.</param>
        /// <returns>The updated IServiceCollection with the CORS policy registered.</returns>
        public static IServiceCollection AddAppCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}

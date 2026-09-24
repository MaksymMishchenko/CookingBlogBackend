using PostApiService.Models.TypeSafe;

namespace PostApiService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserRepository(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<List<IdentityUser>> GetAdminAndContributorUsersAsync(CancellationToken ct = default)
        {
            var targetRoles = new[] { TS.Roles.Admin, TS.Roles.Contributor };

            return await _context.Users
                .Join(_context.UserRoles, user => user.Id, userRole => userRole.UserId, (user, userRole) => new { user, userRole })
                .Join(_context.Roles, x => x.userRole.RoleId, role => role.Id, (x, role) => new { x.user, role })
                .Where(x => targetRoles.Contains(x.role.Name))
                .Select(x => x.user)
                .Distinct()
                .ToListAsync(ct);
        }        
    }
}

using PostApiService.Models.TypeSafe;
using System.Security.Claims;

namespace PostApiService.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AuthRepository(UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IdentityResult> AddClaimAsync(IdentityUser user,
            Claim claim, CancellationToken ct = default)
        {
            return await _userManager.AddClaimAsync(user, claim);
        }

        public async Task<bool> CheckPasswordAsync(IdentityUser user,
            string password, CancellationToken ct = default)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IdentityResult> CreateAsync(IdentityUser user,
            string password, CancellationToken ct = default)
        {
            return await _userManager.CreateAsync(user, password);
        }

        public async Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityUser?> FindByNameAsync(string userName, CancellationToken ct = default)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken ct = default)
        {
            return await _userManager.GetClaimsAsync(user);
        }

        public async Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken ct = default)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<List<IdentityUser>> GetAuthorsAsync(CancellationToken ct = default)
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

using PostApiService.Interfaces;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = TS.Roles.Admin)]
    public class AdminUserController : Controller
    {
        private readonly IAdminUserService _userService;

        public AdminUserController(IAdminUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Retrieves all users for filtering purposes.
        /// </summary>        
        [HttpGet("Authors")]
        public async Task<IActionResult> GetAdminAndContributorUsersAsync(CancellationToken ct = default)
        {
            var result = await _userService.GetAdminAndContributorUsersAsync(ct);

            return result.ToActionResult();
        }
    }
}

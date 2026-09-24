using PostApiService.Interfaces;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Retrieves all users for filtering purposes.
        /// </summary>
        [Authorize(Roles = TS.Roles.Admin)]
        [HttpGet("Authors")]
        public async Task<IActionResult> GetAdminAndContributorUsersAsync(CancellationToken ct = default)
        {
            var result = await _userService.GetAdminAndContributorUsersAsync(ct);

            return result.ToActionResult();
        }
    }
}

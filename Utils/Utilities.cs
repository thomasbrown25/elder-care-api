using System.Security.Claims;
using elder_care_api.Data;

namespace elder_care_api.Utils
{
    public static class Utilities
    {
        public static User GetCurrentUser(DataContext _context, IHttpContextAccessor _httpContextAccessor)
        {
            try
            {
                string email = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (email == null)
                    return null;

                // Get current user from sql db
                User user = _context.Users.FirstOrDefault(u => u.Email.ToLower().Equals(email.ToLower()));

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static int GetUserId(IHttpContextAccessor _httpContextAccessor)
        {
            try
            {
                return int.Parse(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
    }
}

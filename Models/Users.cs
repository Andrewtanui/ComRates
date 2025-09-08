using Microsoft.AspNetCore.Identity;

namespace TanuiApp.Models
{
    public class Users : IdentityUser
    {
        public required String FullName { get; set; }
    }
}

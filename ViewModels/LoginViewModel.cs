using System.ComponentModel.DataAnnotations;

namespace TanuiApp.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress]
        public String Email { get; set; }
        [Required(ErrorMessage = "Password is Required")]
        [DataType(DataType.Password)]
        public String Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}

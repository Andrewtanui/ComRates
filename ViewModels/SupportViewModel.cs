using System.Collections.Generic;
using TanuiApp.Controllers;

namespace TanuiApp.ViewModels
{
    public class SupportViewModel
    {
        public string? UserQuestion { get; set; }
        public string? Answer { get; set; }
        public List<FAQ>? Faqs { get; set; }
    }
}

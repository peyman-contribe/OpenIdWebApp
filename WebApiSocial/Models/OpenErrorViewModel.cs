using System.ComponentModel.DataAnnotations;

namespace WebApiSocial.Models
{
    public class OpenErrorViewModel
    {
        [Display(Name = "Error")]
        public string Error { get; set; }

        [Display(Name = "Description")]
        public string ErrorDescription { get; set; }
    }
}

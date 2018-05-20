using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApiSocial.Models
{
    public class LogoutViewModel
    {
        [BindNever]
        public string RequestId { get; set; }
    }
}

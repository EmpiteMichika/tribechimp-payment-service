using System;
using System.Collections.Generic;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Dto
{
    public class CreateContact
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
    }
}

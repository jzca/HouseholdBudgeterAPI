using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.BindingModel
{
    public class InviteUserBindingModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
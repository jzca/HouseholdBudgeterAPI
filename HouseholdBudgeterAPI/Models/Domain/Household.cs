using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.Domain
{
    public class Household
    {
        public Household()
        {
            Categories = new List<Category>();
            JoinedUsers = new List<ApplicationUser>();
            InvitedUsers = new List<ApplicationUser>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public virtual ApplicationUser Owner { get; set; }
        public string OwnerId { get; set; }
        public virtual List<Category> Categories { get; set; }
        public virtual List<ApplicationUser> JoinedUsers { get; set; }
        public virtual List<ApplicationUser> InvitedUsers  { get; set; }
    }
}
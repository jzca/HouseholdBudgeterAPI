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
            BankAccounts = new List<BankAccount>();
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
        public virtual List<ApplicationUser> InvitedUsers { get; set; }
        public virtual List<BankAccount> BankAccounts { get; set; }

        public bool IsOwner(string userId)
        {
            return OwnerId == userId;
        }

        public bool IsNotOwner(string userId)
        {
            return OwnerId != userId;
        }

        public bool AlreadyInvitedByEmail(string email)
        {
            return InvitedUsers.Any(p => p.Email == email);
        }

        public bool AlreadyJoinedByEmail(string email)
        {
            return JoinedUsers.Any(p => p.Email == email);
        }

        public bool IsInvitedById(string userId)
        {
            return InvitedUsers.Any(p => p.Id == userId);
        }

        public bool IsJoinedById(string userId)
        {
            return JoinedUsers.Any(p => p.Id == userId);
        }

    }
}
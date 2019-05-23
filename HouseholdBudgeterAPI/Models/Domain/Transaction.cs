using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.Domain
{
    public class Transaction
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime DateTransacted { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public virtual BankAccount BankAccount { get; set; }
        public int BankAccountId { get; set; }
        public virtual Category Category { get; set; }
        public int CategoryId { get; set; }
        public bool IsVoid { get; set; }
        public virtual ApplicationUser Creator { get; set; }
        public string CreatorId { get; set; }

        public bool IsCreator(string userId)
        {
            return CreatorId == userId;
        }


    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.ViewModel
{
    public class TranscationDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateTransacted { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int BankAccountId { get; set; }
        public int CategoryId { get; set; }
        public string CreatorId { get; set; }
        public bool IsVoid { get; set; }
        public bool IsCreator { get; set; }
        public bool IsHhOwner { get; set; }
    }
}
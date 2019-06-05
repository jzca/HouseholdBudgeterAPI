using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.ViewModel
{
    public class BankAccountHouseholdViewModel
    {
        public int Id { get; set; }
        public int HouseholdId { get; set; }
        public decimal TotalBalance { get; set; }
        public List<TranscationHouseholdViewModel> transcationHouseholdViews { get; set; }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.ViewModel
{
    public class EaBankAccDetailViewModel
    {
        public int BankAccId { get; set; }
        public string BankAccName { get; set; }
        public decimal Amount { get; set; }

    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.ViewModel
{
    public class TranscationHouseholdViewModel
    {
        public int CategoryId { get; set; }
        //public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }
}
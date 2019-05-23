﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.Models.BindingModel
{
    public class TranscationBindingModel
    {
        [Required]
        public int BankAccountId { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public DateTime DateTransacted { get; set; }
        [Required]
        public int CategoryId { get; set; }
    }
}
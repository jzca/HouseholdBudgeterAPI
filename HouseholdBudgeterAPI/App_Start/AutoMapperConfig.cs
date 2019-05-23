using AutoMapper;
using HouseholdBudgeterAPI.Models.BindingModel;
using HouseholdBudgeterAPI.Models.Domain;
using HouseholdBudgeterAPI.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HouseholdBudgeterAPI.App_Start
{
    public static class AutoMapperConfig
    {
        public static void Init()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Household, HouseholdBindingModel>().ReverseMap();
                cfg.CreateMap<Household, HouseholdViewModel>().ReverseMap();
                cfg.CreateMap<Category, CategoryBindingModel>().ReverseMap();
                cfg.CreateMap<Category, EditCategoryBindingModel>().ReverseMap();
                cfg.CreateMap<Category, CategoryViewModel>().ReverseMap();
                cfg.CreateMap<BankAccount, BankAccountBindingModel>().ReverseMap();
                cfg.CreateMap<BankAccount, EditBankAccountBindingModel>().ReverseMap();
                cfg.CreateMap<BankAccount, BankAccountViewModel>().ReverseMap();
                cfg.CreateMap<Transaction, TranscationBindingModel>().ReverseMap();
                cfg.CreateMap<Transaction, EditTranscationBindingModel>().ReverseMap();
                cfg.CreateMap<Transaction, TranscationViewModel>().ReverseMap();
                
            });
        }
    }
}
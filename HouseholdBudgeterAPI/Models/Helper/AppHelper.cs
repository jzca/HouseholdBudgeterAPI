using HouseholdBudgeterAPI.Models;
using HouseholdBudgeterAPI.Models.BindingModel;
using HouseholdBudgeterAPI.Models.Domain;
using HouseholdBudgeterAPI.Models.ViewModel;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Http;

namespace HouseholdBudgeterAPI.Models.Helper
{
    public class HouseholdHelper
    {
        private readonly ApplicationDbContext DbContext;
        public HouseholdHelper(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public Household GetById(int id)
        {
            return DbContext.Households
                .FirstOrDefault(p => p.Id == id);
        }

        public Household GetByIdWithJoinedUsers(int id)
        {
            return DbContext.Households
                .Include(p => p.JoinedUsers)
                .FirstOrDefault(p => p.Id == id);
        }

        public Household GetByIdWithCategories(int id)
        {
            return DbContext.Households
                .Include(p => p.Categories)
                .FirstOrDefault(p => p.Id == id);
        }


        public Household GetByIdWithInvitedJoinedUsers(int id)
        {
            return DbContext.Households
                .Include(p => p.InvitedUsers)
                .Include(p => p.JoinedUsers)
                .FirstOrDefault(p => p.Id == id);
        }

        public string GetHhOwnerIdByHhId(int id)
        {
            return DbContext.Households
                .Where(p => p.Id == id)
                .Select(p => p.OwnerId)
                .FirstOrDefault();
        }

    }

    public class UserHelper
    {
        private readonly ApplicationDbContext DbContext;
        public UserHelper(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }


        public ApplicationUser GetUserById(string appUserId)
        {
            return DbContext.Users
                .FirstOrDefault(p => p.Id == appUserId);
        }

        public ApplicationUser GetUserByEmail(string email)
        {
            return DbContext.Users
                .FirstOrDefault(p => p.Email == email);
        }

        public List<ApplicationUser> GetJoinedUsersByBaId(int BaId)
        {
            return DbContext.BankAccounts.
                Where(p=> p.Id == BaId)
                .SelectMany(p => p.Household.JoinedUsers)
                .ToList();
        }

        //public List<ApplicationUser> GetJoinedUsersByBaIdWithHh(int BaId)
        //{
        //    return DbContext.BankAccounts.
        //        Where(p => p.Id == BaId)
        //        .Include(p=> p.Household)
        //        .SelectMany(p => p.Household.JoinedUsers)
        //        .ToList();
        //}


    }

    public class CategoryHelper
    {
        private readonly ApplicationDbContext DbContext;

        public CategoryHelper(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        //public Category GetById(int id)
        //{
        //    return DbContext.Categories
        //       .Where(p => p.Id == id)
        //       .FirstOrDefault();
        //}

        public Category GetByIdWithHh(int id)
        {
            return DbContext.Categories
               .Include(p => p.Household)
               .Where(p => p.Id == id)
               .FirstOrDefault();
        }

        //public List<Category> GetAllByHhId(int id)
        //{
        //    return DbContext.Categories
        //       .Where(p => p.HouseholdId == id)
        //       .ToList();
        //}

    }

    public class BankAccountHelper
    {
        private readonly ApplicationDbContext DbContext;
        public BankAccountHelper(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        //public BankAccount GetById(int id)
        //{
        //    return DbContext.BankAccounts
        //       .FirstOrDefault(p => p.Id == id);
        //}

        public BankAccount GetByIdWithHh(int id)
        {
            return DbContext.BankAccounts
               .Where(p => p.Id == id)
               .Include(p => p.Household)
               .FirstOrDefault();
        }

        //public int GetHhIdByIdWithJoinedUsers(int id)
        //{
        //    return DbContext.BankAccounts
        //       .Where(p => p.Id == id)
        //       .Include(p => p.Household.JoinedUsers)
        //       .Select(p=> p.HouseholdId)
        //       .FirstOrDefault();
        //}

        public BankAccount GetByIdWithHhTrans(int id)
        {
            return DbContext.BankAccounts
               .Where(p => p.Id == id)
               .Include(p => p.Household)
               .Include(p=> p.Transactions)
               .FirstOrDefault();
        }

        public BankAccount GetByIdWithTrans(int id)
        {
            return DbContext.BankAccounts
               .Where(p => p.Id == id)
               .Include(p => p.Transactions)
               .FirstOrDefault();
        }

    }

    public class TransactionHelper
    {
        private readonly ApplicationDbContext DbContext;
        public TransactionHelper(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public bool IsCategoryBelongToSameHhByBaId(int id, int CatId)
        {
            return DbContext.BankAccounts
               .Where(p => p.Id == id)
               .Any(p => p.Household.Categories.Any(b => b.Id == CatId));
        }

        public bool IsCategoryBelongToSameHhByTransId(int id, int CatId)
        {
            return DbContext.Transactions
               .Where(p => p.Id == id)
               .Any(p => p.CategoryId== CatId);
        }


        //public Transaction GetById(int id)
        //{
        //    return DbContext.Transactions
        //            .Where(p => p.Id == id)
        //            .FirstOrDefault();
        //}

        public Transaction GetByIdWithHhViaCat(int id)
        {
            return DbContext.Transactions
                    .Where(p => p.Id == id)
                    .Include(p=> p.Category.Household)
                    .FirstOrDefault();
        }

        //public List<string> GetAllTranAmtByBaId(int? id)
        //{
        //    return DbContext.BankAccounts
        //            .Where(p => p.Id == id)
        //            .SelectMany(n => n.Transactions.Select(m=> m.Amount.ToString()))
        //            .ToList();
        //}

        public decimal GetSumOfAllTransByBaId(int? id)
        {
            return DbContext.BankAccounts
                    .Where(p => p.Id == id)
                    .SelectMany(n => 
                    n.Transactions.Select(m => m.Amount)).Sum();
        }

        public decimal GetSumOfAllByTrans(List<Transaction> trans)
        {
            return trans.Select(n => n.Amount).Sum();
        }


    }





    }
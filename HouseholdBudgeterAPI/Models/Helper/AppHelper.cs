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
    }

    public class CategoryHelper
    {
        private readonly ApplicationDbContext DbContext;
        public CategoryHelper(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public Category GetById(int id)
        {
            return DbContext.Categories
               .Where(p => p.Id == id)
               .FirstOrDefault();
        }

        public Category GetByIdWithHhOwnerId(int id)
        {
            return DbContext.Categories
               .Include(p => p.Household)
               .Where(p => p.Id == id)
               .FirstOrDefault();
        }

        public List<Category> GetAllByHhId(int id)
        {
            return DbContext.Categories
               .Where(p => p.HouseholdId == id)
               .ToList();
        }

    }


}
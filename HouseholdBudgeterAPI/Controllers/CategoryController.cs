using AutoMapper;
using AutoMapper.QueryableExtensions;
using HouseholdBudgeterAPI.Models;
using HouseholdBudgeterAPI.Models.BindingModel;
using HouseholdBudgeterAPI.Models.Domain;
using HouseholdBudgeterAPI.Models.Filter;
using HouseholdBudgeterAPI.Models.Helper;
using HouseholdBudgeterAPI.Models.ViewModel;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace HouseholdBudgeterAPI.Controllers
{
    [Authorize]
    public class CategoryController : ApiController
    {
        private readonly ApplicationDbContext DbContext;
        private readonly HouseholdHelper HouseholdHelper;
        private readonly CategoryHelper CategoryHelper;
        private readonly TransactionHelper TransactionHelper;
        private readonly BankAccountHelper BankAccountHelper;

        public CategoryController()
        {
            DbContext = new ApplicationDbContext();
            HouseholdHelper = new HouseholdHelper(DbContext);
            CategoryHelper = new CategoryHelper(DbContext);
            TransactionHelper = new TransactionHelper(DbContext);
            BankAccountHelper = new BankAccountHelper(DbContext);

        }

        [HttpPost]
        [FormDataNullAF]
        public IHttpActionResult Create(CategoryBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var householdOwnerId = HouseholdHelper.GetHhOwnerIdByHhId(model.HouseholdId);

            if (householdOwnerId == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = householdOwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            var category = Mapper.Map<Category>(model);
            category.DateCreated = DateTime.Now;

            DbContext.Categories.Add(category);
            DbContext.SaveChanges();

            var viewModel = Mapper.Map<CategoryViewModel>(category);
            var url = Url.Link("DefaultApi",
                new { Action = "GetAllByHhId", model.HouseholdId });

            return Created(url, viewModel);
        }

        [HttpPut]
        [FormDataNullAF]
        public IHttpActionResult Edit(int id, EditCategoryBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = CategoryHelper.GetByIdWithHh(id);

            if (category == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = category.Household.OwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            Mapper.Map(model, category);
            category.DateUpdated = DateTime.Now;

            DbContext.SaveChanges();

            var viewModel = Mapper.Map<CategoryViewModel>(category);
            return Ok(viewModel);

        }

        [HttpGet]
        public IHttpActionResult GetAllByHhId(int id)
        {
            var allCategoriesModel = DbContext.Households
                .Where(p => p.Id == id)
                .SelectMany(b => b.Categories)
                .ProjectTo<CategoryViewModel>()
                .ToList();

            if (!allCategoriesModel.Any())
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            bool isJoined = DbContext
                .Households
                .Where(p => p.Id == id)
                .Any(a => a.JoinedUsers
                .Any(b => b.Id == currentUserId));

            if (!isJoined)
            {
                return Unauthorized();
            }

            return Ok(allCategoriesModel);
        }

        [HttpGet]
        public IHttpActionResult GetCreatedByHhId(int id)
        {
            var currentUserId = User.Identity.GetUserId();

            var myCategoriesModel = DbContext.Categories
                .Where(p => p.HouseholdId == id &&
                p.Household.OwnerId == currentUserId)
                .ProjectTo<CategoryViewModel>()
                .ToList();

            if (!myCategoriesModel.Any())
            {
                return NotFound();
            }

            return Ok(myCategoriesModel);
        }

        [HttpGet]
        public IHttpActionResult GetByCatId(int id)
        {
            var currentUserId = User.Identity.GetUserId();

            var myCategory = DbContext.Categories
                .Where(p => p.Id == id &&
                p.Household.OwnerId == currentUserId)
                .ProjectTo<CategoryViewModel>()
                .FirstOrDefault();

            if (myCategory == null)
            {
                return NotFound();
            }

            return Ok(myCategory);
        }

        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            var category = CategoryHelper.GetByIdWithHh(id);

            if (category == null)
            {
                return NotFound();
            }

            var householdOwnerId = category.Household.OwnerId;

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = householdOwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            var manyBankAccId = TransactionHelper.GetAllBaIdByCatId(id);

            DbContext.Categories.Remove(category);
            DbContext.SaveChanges();

            UpdateBalanceForBankAccs(manyBankAccId);

            return Ok();
        }

        private void UpdateBalanceForBankAccs(List<int> baIds)
        {
            foreach (var p in baIds)
            {
                var bankAcc = BankAccountHelper.GetById(p);
                bankAcc.Balance = TransactionHelper.GetSumOfAllTransByBaId(p);
                DbContext.SaveChanges();
            }
        }


    }
}

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
    public class BankAccountController : ApiController
    {
        private readonly ApplicationDbContext DbContext;
        private readonly HouseholdHelper HouseholdHelper;
        private readonly BankAccountHelper BankAccountHelper;
        private readonly TransactionHelper TransactionHelper;

        public BankAccountController()
        {
            DbContext = new ApplicationDbContext();
            HouseholdHelper = new HouseholdHelper(DbContext);
            BankAccountHelper = new BankAccountHelper(DbContext);
            TransactionHelper = new TransactionHelper(DbContext);
            BankAccountHelper = new BankAccountHelper(DbContext);
        }

        [HttpPost]
        [FormDataNullAF]
        public IHttpActionResult Create(BankAccountBindingModel formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var householdOwnerId = HouseholdHelper.GetHhOwnerIdByHhId(formData.HouseholdId);

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

            var bankAccount = Mapper.Map<BankAccount>(formData);

            DbContext.BankAccounts.Add(bankAccount);
            DbContext.SaveChanges();

            var viewModel = Mapper.Map<BankAccountViewModel>(bankAccount);

            var url = Url.Link("DefaultApi",
                new { Action = "GetAllByHhId"});

            return Created(url, viewModel);
        }

        [HttpPut]
        [FormDataNullAF]
        public IHttpActionResult Edit(int id, EditBankAccountBindingModel formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var bankAccount = BankAccountHelper.GetByIdWithHh(id);

            if (bankAccount == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = bankAccount.Household.OwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            Mapper.Map(formData, bankAccount);
            bankAccount.DateUpdated = DateTime.Now;

            DbContext.SaveChanges();

            var viewModel = Mapper.Map<BankAccountViewModel>(bankAccount);
            return Ok(viewModel);

        }

        [HttpGet]
        public IHttpActionResult GetAllByHhId(int id)
        {
            var allBankAccountModel = DbContext.Households
                .Where(p => p.Id == id)
                .SelectMany(b => b.BankAccounts)
                .ProjectTo<BankAccountViewModel>()
                .ToList();

            if (!allBankAccountModel.Any())
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

            return Ok(allBankAccountModel);
        }

        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            var bankAccount = BankAccountHelper.GetByIdWithHh(id);

            if (bankAccount == null)
            {
                return NotFound();
            }

            var householdOwnerId = bankAccount.Household.OwnerId;

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = householdOwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            DbContext.BankAccounts.Remove(bankAccount);
            DbContext.SaveChanges();

            return Ok();
        }

        [HttpPut]
        public IHttpActionResult UpdateBalance(int id)
        {

            var bankAccount = BankAccountHelper.GetByIdWithHhTrans(id);

            if (bankAccount == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = bankAccount.Household.OwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            bankAccount.Balance = TransactionHelper
                .GetSumOfAllByTrans(bankAccount.Transactions);

            DbContext.SaveChanges();

            var viewModel = Mapper.Map<BankAccountViewModel>(bankAccount);
            return Ok(viewModel);

        }


    }
}

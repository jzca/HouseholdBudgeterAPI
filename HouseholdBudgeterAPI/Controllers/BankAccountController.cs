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

        [HttpGet]
        public IHttpActionResult GetTotalBalanceByHhId(int id)
        {
            var allBankAccountModel = DbContext.Households
                .Where(p => p.Id == id)
                .SelectMany(p=> p.BankAccounts)
                .ProjectTo<BankAccountHouseholdViewModel>()
                .FirstOrDefault();

            if (allBankAccountModel== null)
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


        [HttpGet]
        public IHttpActionResult BigEaBankAccBalanceByHhId(int id)
        {
            var currentUserId = User.Identity.GetUserId();

            var allBankAccs = DbContext.Households
                .Where(p => p.Id == id && (
                p.JoinedUsers.Any(b => b.Id == currentUserId) 
                || p.OwnerId == currentUserId)
                )
                .SelectMany(p => p.BankAccounts)
                .Select(n => new BigEaBankAccDetailViewModel
                {
                    BankAccId = n.Id,
                    BankAccName = n.Name,
                    Amount = n.Balance
                })
                .ToList();

            if (!allBankAccs.Any())
            {
                return NotFound();
            }

            foreach(var ba in allBankAccs)
            {
                var myTranscation = DbContext.BankAccounts
              .Where(p => p.Id == ba.BankAccId &&
                      (p.Household.OwnerId == currentUserId
                      || p.Household.JoinedUsers.Any(c => c.Id == currentUserId)
                      ))
                      .SelectMany(p => p.Transactions)
                      .GroupBy(p => p.Category.Name)
              .Select(c => new TransAmtByCatViewModel
              {
                  CategoryName = c.Key,
                  Amount = c.Sum(d => (decimal)d.Amount)
              })
              .ToList();

                ba.TransAmtByCats=myTranscation;
            }

            return Ok(allBankAccs);
        }

        //[HttpGet]
        //public IHttpActionResult EaBankAccBalanceByHhId(int id)
        //{
        //    var transactionsByCategory = DbContext.Households
        //        .Where(p => p.Id == id)
        //        .SelectMany(p => p.BankAccounts)
        //        .Select(n => new EaBankAccDetailViewModel
        //        {
        //            BankAccId=n.Id,
        //            BankAccName = n.Name,
        //            Amount = n.Balance
        //        })
        //        .ToList();

        //    if (!transactionsByCategory.Any())
        //    {
        //        return NotFound();
        //    }

        //    var currentUserId = User.Identity.GetUserId();
        //    bool isJoined = DbContext
        //        .Households
        //        .Where(p => p.Id == id)
        //        .Any(a => a.JoinedUsers
        //        .Any(b => b.Id == currentUserId));

        //    if (!isJoined)
        //    {
        //        return Unauthorized();
        //    }

        //    return Ok(transactionsByCategory);
        //}

        //[HttpGet]
        //public IHttpActionResult GetTranSumByBaIdByCat(int id)
        //{
        //    var currentUserId = User.Identity.GetUserId();
        //    var myTranscation = DbContext.BankAccounts
        //        .Where(p => p.Id == id &&
        //                (p.Household.OwnerId == currentUserId
        //                || p.Household.JoinedUsers.Any(c => c.Id == currentUserId)
        //                ))
        //                .SelectMany(p => p.Transactions)
        //                .GroupBy(p => p.CategoryId)
        //        .Select(c => new TranscationHouseholdViewModel
        //        {
        //            CategoryId = c.Key,
        //            Amount = c.Sum(d=> (decimal)d.Amount)
        //        })
        //        .ToList();

        //    if (myTranscation == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(myTranscation);
        //}

        [HttpGet]
        public IHttpActionResult GetCreatedByHhId(int id)
        {
            var currentUserId = User.Identity.GetUserId();

            var myBankAccounts = DbContext.BankAccounts
                .Where(p => p.HouseholdId == id &&
                p.Household.OwnerId == currentUserId)
                .ProjectTo<BankAccountViewModel>()
                .ToList();

            if (!myBankAccounts.Any())
            {
                return NotFound();
            }

            return Ok(myBankAccounts);
        }

        [HttpGet]
        public IHttpActionResult GetByBaId(int id)
        {
            var currentUserId = User.Identity.GetUserId();

            var oneBankAccount = DbContext.BankAccounts
                .Where(p => p.Id == id &&
                p.Household.OwnerId == currentUserId)
                .ProjectTo<CategoryViewModel>()
                .FirstOrDefault();

            if (oneBankAccount == null)
            {
                return NotFound();
            }

            return Ok(oneBankAccount);
        }

        [HttpGet]
        public IHttpActionResult GetAllByUserId()
        {
            var currentUserId = User.Identity.GetUserId();

            var allBankAccounts = DbContext.BankAccounts
                .Where(p => p.Household.JoinedUsers
                .Any(b=> b.Id == currentUserId) 
                || p.Household.OwnerId == currentUserId)
                .ProjectTo<BankAccountViewModel>()
                .ToList();

            if (!allBankAccounts.Any())
            {
                return NotFound();
            }

            return Ok(allBankAccounts);
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

using AutoMapper;
using AutoMapper.QueryableExtensions;
using HouseholdBudgeterAPI.Models;
using HouseholdBudgeterAPI.Models.BindingModel;
using HouseholdBudgeterAPI.Models.Domain;
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
    public class TransactionController : ApiController
    {
        private readonly ApplicationDbContext DbContext;
        private readonly HouseholdHelper HouseholdHelper;
        private readonly UserHelper UserHelper;
        private readonly BankAccountHelper BankAccountHelper;
        private readonly TransactionHelper TransactionHelper;
        private string CurrentUserID { get { return User.Identity.GetUserId(); } }

        public TransactionController()
        {
            DbContext = new ApplicationDbContext();
            HouseholdHelper = new HouseholdHelper(DbContext);
            UserHelper = new UserHelper(DbContext);
            BankAccountHelper = new BankAccountHelper(DbContext);
            TransactionHelper = new TransactionHelper(DbContext);
        }

        [HttpPost]
        public IHttpActionResult Create(int id, TranscationBindingModel formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var joinedUsers = UserHelper.GetJoinedUsersByBaId(id);

            if (!joinedUsers.Any())
            {
                return NotFound();
            }

            var validCatId = TransactionHelper.IsCategoryBelongToSameHhByBaId(id, formData.CategoryId);

            var isJoined = joinedUsers.Any(p => p.Id == CurrentUserID);

            if (!validCatId || !isJoined)
            {
                return Unauthorized();
            }

            var transcation = Mapper.Map<Transaction>(formData);
            transcation.BankAccountId = id;
            transcation.DateCreated = DateTime.Now;
            transcation.CreatorId = CurrentUserID;

            DbContext.Transactions.Add(transcation);
            DbContext.SaveChanges();

            CalculateBalance(transcation.BankAccountId);

            var viewModel = Mapper.Map<TranscationViewModel>(transcation);

            return Ok(viewModel);
        }

        [HttpPut]
        public IHttpActionResult Edit(int id, TranscationBindingModel formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var validCatId = TransactionHelper
                .IsCategoryBelongToSameHhByTransId(id, formData.CategoryId);

            if (!validCatId)
            {
                return Unauthorized();
            }

            var transcation = TransactionHelper.GetByIdWithHhViaCat(id);

            if (transcation == null)
            {
                return NotFound();
            }

            if (IsAuthorized(transcation, CurrentUserID))
            {
                return Unauthorized();
            }

            Mapper.Map(formData, transcation);
            transcation.DateUpdated = DateTime.Now;

            DbContext.SaveChanges();

            CalculateBalance(transcation.BankAccountId);

            var viewModel = Mapper.Map<TranscationViewModel>(transcation);
            return Ok(viewModel);

        }

        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            var transcation = TransactionHelper.GetByIdWithHhViaCat(id);

            if (transcation == null)
            {
                return NotFound();
            }

            if (IsAuthorized(transcation, CurrentUserID))
            {
                return Unauthorized();
            }

            var bankAccId = transcation.BankAccountId;

            DbContext.Transactions.Remove(transcation);
            DbContext.SaveChanges();

            CalculateBalance(bankAccId);

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult Void(int id)
        {
            var transcation = TransactionHelper.GetByIdWithHhViaCat(id);

            if (transcation == null)
            {
                return NotFound();
            }

            if (IsAuthorized(transcation, CurrentUserID))
            {
                return Unauthorized();
            }

            transcation.IsVoid = true;

            DbContext.SaveChanges();

            CalculateBalance(transcation.BankAccountId);

            var viewModel = Mapper.Map<TranscationViewModel>(transcation);

            return Ok(viewModel);
        }

        [HttpGet]
        public IHttpActionResult GetAllByBaId(int id)
        {
            var allTransactionsModel = DbContext.BankAccounts
                .Where(p => p.Id == id)
                .ProjectTo<TranscationViewModel>()
                .ToList();

            if (!allTransactionsModel.Any())
            {
                return NotFound();
            }

            bool isJoined = DbContext
                .BankAccounts
                .Where(p => p.Id == id)
                .Any(a => a.Household.JoinedUsers
                .Any(b => b.Id == CurrentUserID));

            if (!isJoined)
            {
                return Unauthorized();
            }

            return Ok(allTransactionsModel);
        }

        private bool IsAuthorized(Transaction trans, string userId)
        {
            var isCreator = trans.IsCreator(userId);
            var isHhOwner = trans.IsHhOwner(userId);
            if (!isCreator && !isHhOwner)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CalculateBalance(int BaId)
        {
            var bankAcc = BankAccountHelper.GetByIdWithTrans(BaId);
            var allAmt = TransactionHelper.GetSumOfAllByTrans(bankAcc.Transactions);
            var changed = bankAcc.Balance != allAmt;
            if (changed)
            {
                bankAcc.Balance = allAmt;
                DbContext.SaveChanges();
            }
        }

    }
}

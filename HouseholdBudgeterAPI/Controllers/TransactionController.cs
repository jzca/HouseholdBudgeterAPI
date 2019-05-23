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
        public IHttpActionResult Create(TranscationBindingModel formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var joinedUsers = UserHelper.GetJoinedUsersByBaId(formData.BankAccountId);

            if (!joinedUsers.Any())
            {
                return NotFound();
            }

            var validCatId = TransactionHelper
                .IsCategoryBelongToSameHhByBaId(formData.BankAccountId, formData.CategoryId);

            if (!validCatId)
            {
                ModelState.AddModelError("CategoryId", "You don't share the same Household with this Cat.");
                return BadRequest(ModelState);
            }

            var isJoined = joinedUsers.Any(p => p.Id == CurrentUserID);

            if (!isJoined)
            {
                return Unauthorized();
            }


            var transcation = Mapper.Map<Transaction>(formData);
            transcation.DateCreated = DateTime.Now;
            transcation.CreatorId = CurrentUserID;

            DbContext.Transactions.Add(transcation);

            SimpleCalculateBalance(true,true, transcation.BankAccountId, transcation, formData.Amount);

            DbContext.SaveChanges();

            var viewModel = Mapper.Map<TranscationViewModel>(transcation);

            return Ok(viewModel);
        }

        [HttpPut]
        public IHttpActionResult Edit(int id, EditTranscationBindingModel formData)
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

            if(transcation.IsVoid == true)
            {
                ModelState.AddModelError("IsVoid", "Cannot edit void transcation");
                return BadRequest(ModelState);
            }

            if (IsAuthorized(transcation, CurrentUserID))
            {
                return Unauthorized();
            }

            Mapper.Map(formData, transcation);
            transcation.DateUpdated = DateTime.Now;

            SimpleCalculateBalance(true,false, transcation.BankAccountId, transcation, formData.Amount);

            DbContext.SaveChanges();

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

            var oldAmt = transcation.Amount;

            DbContext.Transactions.Remove(transcation);

            SimpleCalculateBalance(false,false, transcation.BankAccountId, transcation, oldAmt);

            DbContext.SaveChanges();

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

            var oldAmt = transcation.Amount;

            SimpleCalculateBalance(false,false, transcation.BankAccountId, transcation, oldAmt);

            DbContext.SaveChanges();


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
            var isHhOwner = trans.BankAccount.Household.IsOwner(userId);
            if (!isCreator && !isHhOwner)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SimpleCalculateBalance(bool plus, bool onCreated, int BaId, Transaction trans, decimal inputVal)
        {
            var bankAcc = BankAccountHelper.GetByIdWithTrans(BaId);

            if (plus)
            {
                var changed = trans.Amount != inputVal;
                if (changed || onCreated)
                {
                    bankAcc.Balance += inputVal;
                }
            }
            else
            {
                bankAcc.Balance -= inputVal;
            }

        }


    }
}

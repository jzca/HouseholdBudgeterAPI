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
    public class TransactionController : ApiController
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserHelper UserHelper;
        private readonly BankAccountHelper BankAccountHelper;
        private readonly TransactionHelper TransactionHelper;
        private string CurrentUserID { get { return User.Identity.GetUserId(); } }

        public TransactionController()
        {
            DbContext = new ApplicationDbContext();
            UserHelper = new UserHelper(DbContext);
            BankAccountHelper = new BankAccountHelper(DbContext);
            TransactionHelper = new TransactionHelper(DbContext);
        }

        [HttpPost]
        [FormDataNullAF]
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
            transcation.CreatorId = CurrentUserID;

            DbContext.Transactions.Add(transcation);

            DirectCalculateBalance(true, transcation.BankAccountId, formData.Amount);

            DbContext.SaveChanges();

            var viewModel = Mapper.Map<TranscationViewModel>(transcation);

            return Ok(viewModel);
        }

        [HttpPut]
        [FormDataNullAF]
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
                ModelState
                    .AddModelError("CategoryId", "You don't share the same Household with this Cat.");
                return BadRequest(ModelState);
            }

            var transcation = TransactionHelper.GetByIdWithHhViaCat(id);

            if (transcation == null)
            {
                return NotFound();
            }

            if (transcation.IsVoid == true)
            {
                ModelState.AddModelError("IsVoid", "Cannot edit void transcation");
                return BadRequest(ModelState);
            }

            if (IsAuthorized(transcation, CurrentUserID))
            {
                return Unauthorized();
            }

            // For Calculation of Balance For Editing
            var oldVal = transcation.Amount;

            Mapper.Map(formData, transcation);
            transcation.DateUpdated = DateTime.Now;
            DbContext.SaveChanges();

            EditCalculateBalance(transcation.BankAccountId, transcation, oldVal);

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

            if (!transcation.IsVoid)
            {
                DirectCalculateBalance(false, transcation.BankAccountId, oldAmt);
            }

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

            if (transcation.IsVoid)
            {
                ModelState.AddModelError("IsVoid", "Cannot void a void transcation");
                return BadRequest(ModelState);
            }

            transcation.IsVoid = true;

            var oldAmt = transcation.Amount;

            DirectCalculateBalance(false, transcation.BankAccountId, oldAmt);

            DbContext.SaveChanges();

            return Ok();
        }

        [HttpGet]
        public IHttpActionResult GetAllByBaId(int id)
        {
            var allTransactionsModel = DbContext.BankAccounts
                .Where(p => p.Id == id)
                .SelectMany(p=> p.Transactions)
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
            var isHhOwner = trans.Category.Household.IsOwner(userId);
            if (!isCreator && !isHhOwner)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DirectCalculateBalance(bool plus, int BaId, decimal inputVal)
        {
            var bankAcc = BankAccountHelper.GetById(BaId);

            if (plus)
            {
                bankAcc.Balance += inputVal;
            }
            else
            {
                bankAcc.Balance -= inputVal;
            }

        }

        private void EditCalculateBalance(int BaId, Transaction trans, decimal oldVal)
        {
            var bankAcc = BankAccountHelper.GetById(BaId);

            var newVal = trans.Amount;

            var diff = oldVal - newVal;

            if (oldVal != newVal)
            {
                if (oldVal < newVal)
                {
                    bankAcc.Balance += (-diff);
                }
                else if (oldVal > newVal)
                {
                    bankAcc.Balance -= diff;
                }

                DbContext.SaveChanges();

            }

        }


    }
}

using AutoMapper;
using AutoMapper.QueryableExtensions;
using HouseholdBudgeterAPI.Models;
using HouseholdBudgeterAPI.Models.BindingModel;
using HouseholdBudgeterAPI.Models.Domain;
using HouseholdBudgeterAPI.Models.Helper;
using HouseholdBudgeterAPI.Models.ViewModel;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using HouseholdBudgeterAPI.Models.Filter;

namespace HouseholdBudgeterAPI.Controllers
{
    [Authorize]
    public class HouseholdController : ApiController
    {
        private readonly ApplicationDbContext DbContext;
        private readonly HouseholdHelper HouseholdHelper;
        private readonly UserHelper UserHelper;

        public HouseholdController()
        {
            DbContext = new ApplicationDbContext();
            HouseholdHelper = new HouseholdHelper(DbContext);
            UserHelper = new UserHelper(DbContext);
        }


        [HttpPost]
        [FormDataNullAF]
        public IHttpActionResult Create(HouseholdBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var appUserId = User.Identity.GetUserId();
            var user = UserHelper.GetUserById(appUserId);
            var household = Mapper.Map<Household>(model);
            household.OwnerId = appUserId;
            household.DateCreated = DateTime.Now;

            DbContext.Households.Add(household);
            household.JoinedUsers.Add(user);
            DbContext.SaveChanges();

            var viewModel = Mapper.Map<HouseholdViewModel>(household);
            return Ok(viewModel);
        }

        [HttpPut]
        [FormDataNullAF]
        public IHttpActionResult Edit(int id, HouseholdBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var household = HouseholdHelper.GetById(id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = household.IsOwner(currentUserId);
            if (IsOwner)
            {
                Mapper.Map(model, household);
                household.DateUpdated = DateTime.Now;

                DbContext.SaveChanges();

                var viewModel = Mapper.Map<HouseholdViewModel>(household);
                return Ok(viewModel);

            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public IHttpActionResult GetByUserId(int id)
        {
            var appUserId = User.Identity.GetUserId();

            var myHousehold = DbContext.Households
                .Where(p => p.Id == id && p.OwnerId == appUserId)
                .ProjectTo<HouseholdViewModel>()
                .FirstOrDefault();

            if (myHousehold == null)
            {
                return NotFound();
            }

            return Ok(myHousehold);
        }

        [HttpGet]
        public IHttpActionResult GetByUserId()
        {
            var appUserId = User.Identity.GetUserId();

            var myHouseholds = DbContext.Households
                .Where(p => p.JoinedUsers.Any(b => b.Id == appUserId)
                    || p.OwnerId == appUserId)
                .ProjectTo<MyHouseholdViewModel>()
                .ToList();

            if (!myHouseholds.Any())
            {
                return NotFound();
            }

            myHouseholds.ForEach(p =>
            {
                p.IsOwner = p.OwnerId == appUserId;
            });


            return Ok(myHouseholds);
        }

        [HttpGet]
        public IHttpActionResult GetByInvitedUser()
        {
            var appUserId = User.Identity.GetUserId();

            var invitedHouseholds = DbContext.Households
                .Where(p => p.InvitedUsers.Any(b => b.Id == appUserId)
                && p.OwnerId != appUserId)
                .ProjectTo<HouseholdViewModel>()
                .ToList();

            if (!invitedHouseholds.Any())
            {
                return NotFound();
            }

            return Ok(invitedHouseholds);
        }

        [HttpGet]
        public IHttpActionResult GetUsersByHhId(int id)
        {
            var joinedUsersModel = DbContext.Households
                .Where(p => p.Id == id)
                .SelectMany(b => b.JoinedUsers)
                .ProjectTo<ShowUsersViewModel>()
                .ToList();

            if (!joinedUsersModel.Any())
            {
                return NotFound();
            }
            var currentUserId = User.Identity.GetUserId();
            var isJoined = joinedUsersModel.Any(p => p.Id == currentUserId);

            if (!isJoined)
            {
                return Unauthorized();
            }

            return Ok(joinedUsersModel);
        }

        [HttpPost]
        public IHttpActionResult InviteUserByHhIdEmail(int id, InviteUserBindingModel formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var email = formData.Email;

            var household = HouseholdHelper.GetByIdWithInvitedJoinedUsers(id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsNotOwner = household.IsNotOwner(currentUserId);

            if (IsNotOwner)
            {
                return Unauthorized();
            }

            var user = UserHelper.GetUserByEmail(email);

            if (user == null)
            {
                ModelState.AddModelError("email", "The user doesn't not exist");
                return BadRequest(ModelState);
            }

            var alreadyInvited = household.AlreadyInvitedByEmail(email);
            var alreadyJoined = household.AlreadyJoinedByEmail(email);

            if (alreadyInvited)
            {
                ModelState.AddModelError("Email", "The user was already invitated.");
                return BadRequest(ModelState);
            }
            else if (alreadyJoined)
            {
                ModelState.AddModelError("Email", "The user was already joined. Cannot be invitated.");
                return BadRequest(ModelState);
            }
            else
            {
                household.InvitedUsers.Add(user);
                DbContext.SaveChanges();
            }

            var eService = new EmailService();
            var subject = $"Invitation to Household {id}";
            var callbackUrl = Url.Link("DefaultApi",
                new { Action = "JoinHouseholdById", household.Id });
            var body = $"If you would like to join in it" +
                $", plesae post the link: <a href=\"" + callbackUrl + "\">here</a> .";
            eService.Send(email, subject, body);


            var model = Mapper.Map<ShowUsersViewModel>(user);

            return Ok(model);
        }

        [HttpPost]
        public IHttpActionResult JoinHouseholdById(int id)
        {
            var household = HouseholdHelper.GetByIdWithInvitedJoinedUsers(id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();

            var IsOwner = household.IsOwner(currentUserId);

            if (IsOwner)
            {
                return Unauthorized();
            }

            var isInvited = household.IsInvitedById(currentUserId);
            var user = UserHelper.GetUserById(currentUserId);

            if (user == null)
            {
                return NotFound();
            }

            var alreadyJoined = household.IsJoinedById(currentUserId);


            if (alreadyJoined)
            {
                ModelState.AddModelError("id", "The user is already joined. Cannot join again.");
                return BadRequest(ModelState);
            }


            if (isInvited)
            {
                household.JoinedUsers.Add(user);
                household.InvitedUsers.Remove(user);
                DbContext.SaveChanges();
            }
            else
            {
                ModelState.AddModelError("id", "You are not invited");
                return BadRequest(ModelState);
            }

            var joinedUsersModel = Mapper.Map<List<ShowUsersViewModel>>(household.JoinedUsers);
            return Ok(joinedUsersModel);
        }

        [HttpPost]
        public IHttpActionResult Leave(int id)
        {
            var household = HouseholdHelper.GetByIdWithJoinedUsers(id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = household.OwnerId == currentUserId;
            var isJoined = household.IsJoinedById(currentUserId);

            if (IsOwner || !isJoined)
            {
                return Unauthorized();
            }

            var user = UserHelper.GetUserById(currentUserId);

            if (user == null)
            {
                return NotFound();
            }

            household.JoinedUsers.Remove(user);
            DbContext.SaveChanges();


            return Ok();
        }

        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            var household = HouseholdHelper.GetByIdWithCategories(id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = household.IsOwner(currentUserId);

            if (!IsOwner)
            {
                return Unauthorized();
            }

            var AllBelongedCat = household.Categories;

            if (AllBelongedCat.Any())
            {
                DbContext.Categories.RemoveRange(AllBelongedCat);
            }

            DbContext.Households.Remove(household);
            DbContext.SaveChanges();

            return Ok();
        }




    }
}

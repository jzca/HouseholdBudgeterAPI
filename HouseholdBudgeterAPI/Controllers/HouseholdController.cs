using AutoMapper;
using AutoMapper.QueryableExtensions;
using HouseholdBudgeterAPI.Models;
using HouseholdBudgeterAPI.Models.BindingModel;
using HouseholdBudgeterAPI.Models.Domain;
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
    public class HouseholdController : ApiController
    {
        private ApplicationDbContext DbContext;

        public HouseholdController()
        {
            DbContext = new ApplicationDbContext();
        }


        [HttpPost]
        public IHttpActionResult Create(HouseholdBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var appUserId = User.Identity.GetUserId();
            var household = Mapper.Map<Household>(model);
            household.OwnerId = appUserId;
            household.DateCreated = DateTime.Now;

            DbContext.Households.Add(household);
            DbContext.SaveChanges();

            var viewModel = Mapper.Map<HouseholdViewModel>(household);
            return Ok(viewModel);
        }

        [HttpPut]
        public IHttpActionResult Edit(int id, HouseholdBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var household = DbContext.Households
                .FirstOrDefault(p => p.Id == id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = household.OwnerId == currentUserId;
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
        public IHttpActionResult InviteUserByHhIdEmail(int id, string email)
        {

            var household = DbContext.Households
            .FirstOrDefault(p => p.Id == id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsNotOwner = household.OwnerId != currentUserId;

            if (IsNotOwner)
            {
                return Unauthorized();
            }

            var user = DbContext.Users
                .FirstOrDefault(p => p.Email == email);

            if (user == null)
            {
                return NotFound();
            }

            var alreadyInvited = household.InvitedUsers.Any(p => p.Email == email);
            var alreadyJoined = household.JoinedUsers.Any(p => p.Email == email);

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
            var household = DbContext.Households
            .FirstOrDefault(p => p.Id == id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var isInvited = household.InvitedUsers.Any(p => p.Id == currentUserId);
            var user = DbContext.Users
                .FirstOrDefault(p => p.Id == currentUserId);

            if (user == null)
            {
                return NotFound();
            }

            var alreadyJoined = household.JoinedUsers.Any(p => p.Id == currentUserId);

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

            var model = Mapper.Map<ShowUsersViewModel>(user);

            var joinedUsersModel = Mapper.Map<List<ShowUsersViewModel>>(household.JoinedUsers);
            return Ok(joinedUsersModel);
        }

        [HttpPost]
        public IHttpActionResult Leave(int id)
        {
            var household = DbContext.Households
                .FirstOrDefault(p => p.Id == id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = household.OwnerId == currentUserId;
            var isJoined = household.JoinedUsers.Any(p => p.Id == currentUserId);

            if (IsOwner || !isJoined)
            {
                return Unauthorized();
            }

            var user = DbContext.Users
                .FirstOrDefault(p => p.Id == currentUserId);

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
            var household = DbContext.Households
                .FirstOrDefault(p => p.Id == id);

            if (household == null)
            {
                return NotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var IsOwner = household.OwnerId == currentUserId;

            if (!IsOwner)
            {
                return Unauthorized();
            }

            DbContext.Households.Remove(household);
            DbContext.SaveChanges();

            return Ok();
        }




    }
}

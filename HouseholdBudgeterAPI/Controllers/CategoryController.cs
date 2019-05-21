﻿using AutoMapper;
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
    public class CategoryController : ApiController
    {
        private readonly ApplicationDbContext DbContext;
        private readonly HouseholdHelper HouseholdHelper;
        private readonly UserHelper UserHelper;
        private readonly CategoryHelper CategoryHelper;

        public CategoryController()
        {
            DbContext = new ApplicationDbContext();
            HouseholdHelper = new HouseholdHelper(DbContext);
            UserHelper = new UserHelper(DbContext);
            CategoryHelper = new CategoryHelper(DbContext);
        }

        [HttpPost]
        public IHttpActionResult Create(int id, CategoryBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var householdOwnerId = HouseholdHelper.GetHhOwnerIdByHhId(id);

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
            category.HouseholdId = id;
            category.DateCreated = DateTime.Now;

            DbContext.Categories.Add(category);
            DbContext.SaveChanges();

            var viewModel = Mapper.Map<CategoryViewModel>(category);
            var url = Url.Link("DefaultApi",
                new { Action = "GetAllByHhId", id });

            return Created(url, viewModel);
        }

        [HttpPut]
        public IHttpActionResult Edit(int id, CategoryBindingModel model)
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
                .Any(b=> b.Id == currentUserId));

            if (!isJoined)
            {
                return Unauthorized();
            }

            return Ok(allCategoriesModel);
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

            DbContext.Categories.Remove(category);
            DbContext.SaveChanges();

            return Ok();
        }

    }
}

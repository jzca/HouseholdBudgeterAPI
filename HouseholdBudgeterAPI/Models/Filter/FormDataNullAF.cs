using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace HouseholdBudgeterAPI.Models.Filter
{
    public class FormDataNullAF : ActionFilterAttribute
    {
        private readonly Func<Dictionary<string, object>, bool> _validate;

        public FormDataNullAF() : this(arguments =>
            arguments.ContainsValue(null))
        { }

        public FormDataNullAF(Func<Dictionary<string, object>, bool> checkCondition)
        {
            _validate = checkCondition;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (_validate(actionContext.ActionArguments))
            {
                actionContext.ModelState.AddModelError("formData"
                    , "It cannot be empty");
            }
        }

    }
}
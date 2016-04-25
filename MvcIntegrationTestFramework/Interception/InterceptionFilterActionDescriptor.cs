using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace MvcIntegrationTestFramework.Interception
{
    /// <summary>
    /// A special ASP.NET MVC action descriptor used to attach InterceptionFilter to all loaded controllers
    /// </summary>
    internal class InterceptionFilterActionDescriptor : ReflectedActionDescriptor
    {
        public InterceptionFilterActionDescriptor(MethodInfo methodInfo, string actionName, ControllerDescriptor controllerDescriptor)
            : base(methodInfo, actionName, controllerDescriptor)
        {
        }

        [Obsolete]
        public override FilterInfo GetFilters()
        {
            var test = new FilterInfo();
            var interceptionFilter = new InterceptionFilter();
            test.ActionFilters.Insert(0, interceptionFilter);
            test.ResultFilters.Insert(0, interceptionFilter);

            return test;
        }
    }
}
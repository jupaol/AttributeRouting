﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.Routing;
using AttributeRouting.Framework.Localization;
using AttributeRouting.Specs.Subjects;
using AttributeRouting.Specs.Subjects.Http;
using AttributeRouting.Web.Http.Constraints;
using AttributeRouting.Web.Http.Framework;
using AttributeRouting.Web.Http.WebHost;
using AttributeRouting.Web.Logging;
using AttributeRouting.Web.Mvc;
using MvcContrib.TestHelper;
using MvcRouteTester;
using NUnit.Framework;
using UrlHelper = System.Web.Mvc.UrlHelper;

namespace AttributeRouting.Specs.Tests
{
    public class BugFixTests
    {
        [Test]
        public void Issue218_Url_generation_with_optional_query_params()
        {
            // re: issue #218

            var routes = RouteTable.Routes;
            routes.Clear();
            routes.MapAttributeRoutes(config => config.AddRoutesFromController<Issue218TestController>());
            RouteTable.Routes.Cast<Route>().LogTo(Console.Out);
            
            var urlHelper = new UrlHelper(MockBuilder.BuildRequestContext());

            Assert.That(urlHelper.Action("NoQuery", "Issue218Test", new { categoryId = 12 }),
                        Is.EqualTo("/Issue-218/No-Query?categoryId=12"));

            Assert.That(urlHelper.Action("OptionalQuery", "Issue218Test", new { categoryId = 12 }),
                        Is.EqualTo("/Issue-218/Optional-Query?categoryId=12"));

            Assert.That(urlHelper.Action("DefaultQuery", "Issue218Test"),
                        Is.EqualTo("/Issue-218/Default-Query?categoryId=123"));
        }

        [Test]
        public void Issue161_Querystring_param_constraints_mucks_up_url_generation()
        {
            // re: issue #161
            
            var routes = RouteTable.Routes;
            routes.Clear();
            routes.MapAttributeRoutes(config => config.AddRoutesFromController<Issue161TestController>());

            var urlHelper = new UrlHelper(MockBuilder.BuildRequestContext());
            var routeValues = new { area = "Cms", culture = "en", p = 1 };
            var expectedUrl = urlHelper.Action("Index", "Issue161Test", routeValues);

            Assert.That(expectedUrl, Is.EqualTo("/en/Cms/Content/Items?p=1"));
        }

        [Test]
        public void Issue120_OData_style_http_url_bonks()
        {
            // re: issue #120
            
            var httpRoutes = GlobalConfiguration.Configuration.Routes;
            httpRoutes.Clear();
            httpRoutes.MapHttpAttributeRoutes(config => config.AddRoutesFromController<HttpBugFixesController>());

            // Just make sure we don't get an exception
            Assert.That(httpRoutes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Issue102_Generating_two_routes_for_api_get_requests()
        {
            // re: issue #102
            
            var httpRoutes = GlobalConfiguration.Configuration.Routes;
            httpRoutes.Clear();
            httpRoutes.MapHttpAttributeRoutes(config => config.AddRoutesFromController<Issue102TestController>());

            var routes = RouteTable.Routes.Cast<Route>().ToList();
            
            Assert.That(routes.Count, Is.EqualTo(2));
        }

        [Test]
        public void Issue25_Ensure_that_incompletely_mocked_request_context_does_not_generate_error_in_determining_http_method()
        {
            // re: issue #25

            RouteTable.Routes.Clear();
            RouteTable.Routes.MapAttributeRoutes(config => config.AddRoutesFromController<StandardUsageController>());

            //"~/Index"
            //    .ShouldMapTo<StandardUsageController>(
            //        x => x.Index());

            RouteTable.Routes.ShouldMap("/Index").To<StandardUsageController>(x => x.Index());
        }

        [Test]
        public void Issue43_Ensure_that_routes_with_optional_url_params_are_correctly_matched()
        {
            // re: issue #43

            RouteTable.Routes.Clear();
            RouteTable.Routes.MapAttributeRoutes(config => config.AddRoutesFromController<BugFixesController>());

            RouteTable.Routes.Cast<Route>().LogTo(Console.Out);

            //"~/BugFixes/Gallery/_CenterImage"
            //    .ShouldMapTo<BugFixesController>(
            //        x => x.Issue43_OptionalParamsAreMucky(null, null, null, null));

            RouteTable.Routes.ShouldMap("~/BugFixes/Gallery/_CenterImage").To<BugFixesController>(
                x => x.Issue43_OptionalParamsAreMucky(null, null, null, null));
        }

        [Test]
        public void Issue53_Ensure_that_inbound_routing_works_when_contraining_by_culture()
        {
            // re: issue #53

            var translations = new FluentTranslationProvider();
            translations.AddTranslations().ForController<CulturePrefixController>().RouteUrl(x => x.Index(), new Dictionary<String, String> { { "pt", "Inicio" } });
            translations.AddTranslations().ForController<CulturePrefixController>().RouteUrl(x => x.Index(), new Dictionary<String, String> { { "en", "Home" } });

            RouteTable.Routes.Clear();
            RouteTable.Routes.MapAttributeRoutes(config =>
            {
                config.AddRoutesFromController<CulturePrefixController>();
                config.AddTranslationProvider(translations);
                config.ConstrainTranslatedRoutesByCurrentUICulture = true;
                config.CurrentUICultureResolver = (httpContext, routeData) =>
                {
                    return (string)routeData.Values["culture"]
                           ?? Thread.CurrentThread.CurrentUICulture.Name;
                };
            });

            RouteTable.Routes.Cast<Route>().LogTo(Console.Out);

            //"~/en/cms/home".ShouldMapTo<CulturePrefixController>(x => x.Index());
            //Assert.That("~/en/cms/inicio".Route(), Is.Null);
            //Assert.That("~/pt/cms/home".Route(), Is.Null);
            //"~/pt/cms/inicio".ShouldMapTo<CulturePrefixController>(x => x.Index());

            RouteTable.Routes.ShouldMap("~/en/cms/home").To<CulturePrefixController>(x => x.Index());
            RouteTable.Routes.ShouldMap("~/en/cms/inicio").ToNoRoute();
            RouteTable.Routes.ShouldMap("~/pt/cms/home").ToNoRoute();
            RouteTable.Routes.ShouldMap("~/pt/cms/inicio").To<CulturePrefixController>(x => x.Index());
        }

		[Test]
		public void Issue84_Ensure_that_async_controller_action_can_be_mapped()
		{
			// re: issue #84
			RouteTable.Routes.Clear();
			RouteTable.Routes.MapAttributeRoutes(config => config.AddRoutesFromController<AsyncActionController>());

            //"~/WithAsync/Synchronous".ShouldMapTo<AsyncActionController>(x => x.Test1());
            //var asyncRouteData = "~/WithAsync/NotSynchronous".Route();
            //asyncRouteData.Values["controller"].ShouldEqual("AsyncAction", "Asynchronous route does not map to the AsyncActionController.");
            //asyncRouteData.Values["action"].ShouldEqual("Test2", "Asynchronous route does not map to the correct action method.");

            RouteTable.Routes.ShouldMap("~/WithAsync/Synchronous").To<AsyncActionController>(x => x.Test1());
            RouteAssert.GeneratesActionUrl(RouteTable.Routes, "/WithAsync/NotSynchronous", "Test2", "AsyncAction");
		}

        [Test]
        public void Issue191_in_memory_config_initializes_routes_with_general_http_constraints()
        {
            var inMemoryConfig = new HttpConfiguration();

            inMemoryConfig.Routes.MapHttpAttributeRoutes(x =>
                {
                    x.InMemory = true;
                    x.AddRoutesFromController<HttpStandardUsageController>();
                });

            Assert.AreEqual(6, inMemoryConfig.Routes.Count);
            Assert.True(inMemoryConfig.Routes.All(x => x.Constraints.All(c => c.Value.GetType() == typeof(InboundHttpMethodConstraint))));
        }

        [Test]
        public void Issue191_default_web_config_initializes_routes_with_web_http_constraints()
        {
            var inMemoryConfig = GlobalConfiguration.Configuration;
            inMemoryConfig.Routes.Clear();

            inMemoryConfig.Routes.MapHttpAttributeRoutes(x => x.AddRoutesFromController<HttpStandardUsageController>());

            Assert.AreEqual(6, inMemoryConfig.Routes.Count);
            Assert.True(inMemoryConfig.Routes.All(x => x.Constraints.All(c => c.Value.GetType() == typeof(Web.Http.WebHost.Constraints.InboundHttpMethodConstraint))));
        }

        [Test]
        public void Issue191_in_memory_web_config_inits_general_http_constraint_factory()
        {
            var inMemoryConfig = new HttpWebConfiguration(inMemory:true);
            Assert.IsAssignableFrom<AttributeRouting.Web.Http.Framework.RouteConstraintFactory>(inMemoryConfig.RouteConstraintFactory);
        }

        [Test]
        public void Issue191_default_web_config_inits_web_http_constraint_factory()
        {
            var inMemoryConfig = new HttpWebConfiguration();
            Assert.IsAssignableFrom<AttributeRouting.Web.Http.WebHost.Framework.RouteConstraintFactory>(inMemoryConfig.RouteConstraintFactory);
        }

        [Test]
        public void Issue241_httpRoute_matches_request_for_route_at_root()
        {
            var route = BuildHttpAttributeRoute("Controller/Action", false, false);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Controller/Action");
            var routeData = route.GetRouteData("/", request);

            Assert.That(routeData, Is.Not.Null);
            Assert.That(routeData.Route, Is.EqualTo(route));
        }

        [Test]
        public void Issue241_httpRoute_doesnt_match_root_request_for_route_under_a_virtual_path()
        {
            var route = BuildHttpAttributeRoute("Controller/Action", false, false);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Controller/Action");
            var routeData = route.GetRouteData("/virtual/", request);

            Assert.That(routeData, Is.Null);
        }

        [Test]
        public void Issue241_httpRoute_doesnt_match_virtual_path_request_for_route_at_root_path()
        {
            var route = BuildHttpAttributeRoute("Controller/Action", false, false);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/virtual/Controller/Action");
            var routeData = route.GetRouteData("/", request);

            Assert.That(routeData, Is.Null);
        }

        [Test]
        public void Issue241_httpRoute_matches_virtual_path_request_for_route_under_a_virtual_path()
        {
            var route = BuildHttpAttributeRoute("Controller/Action", false, false);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/virtual/Controller/Action");
            var routeData = route.GetRouteData("/virtual/", request);

            Assert.That(routeData, Is.Not.Null);
            Assert.That(routeData.Route, Is.EqualTo(route));
        }

        private HttpRoute BuildHttpAttributeRoute(string url, bool useLowercaseRoutes, bool appendTrailingSlash)
        {
            var configuration = new Web.Http.HttpConfiguration
            {
                UseLowercaseRoutes = useLowercaseRoutes,
                AppendTrailingSlash = appendTrailingSlash,
            };

            return new HttpAttributeRoute(url,
                                      new HttpRouteValueDictionary(),
                                      new HttpRouteValueDictionary(),
                                      new HttpRouteValueDictionary(),
                                      configuration);
        }
    }
}

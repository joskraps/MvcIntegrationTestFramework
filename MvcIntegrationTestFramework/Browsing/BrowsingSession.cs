﻿using System;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using MvcIntegrationTestFramework.Interception;

namespace MvcIntegrationTestFramework.Browsing
{
    public class BrowsingSession
    {
        public HttpSessionState Session { get; private set; }
        public HttpCookieCollection Cookies { get; private set; }

        public BrowsingSession()
        {
            Cookies = new HttpCookieCollection();
        }

        public RequestResult Get(string url)
        {
            return Get(url, "");
        }

        public RequestResult Get(string url, string acceptHeader)
        {
            return Get(url, acceptHeader, "", "");
        }

        public RequestResult Get(string url, string acceptHeader, string authorizeUsername, string authorizePassword)
        {
            return ProcessRequest(url, HttpVerbs.Get, new NameValueCollection(), acceptHeader, authorizeUsername, authorizePassword);
        }

        /// <summary>
        /// Sends a post to your url. Url should NOT start with a /
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formData"></param>
        /// <example>
        /// <code>
        /// var result = Post("registration/create", new
        /// {
        ///     Form = new
        ///     {
        ///         InvoiceNumber = "10000",
        ///         AmountDue = "10.00",
        ///         Email = "chriso@innovsys.com",
        ///         Password = "welcome",
        ///         ConfirmPassword = "welcome"
        ///     }
        /// });
        /// </code>
        /// </example>
        public RequestResult Post(string url, object formData)
        {
            return Post(url, formData, "");
        }

        public RequestResult Post(string url, object formData, NameValueCollection headers)
        {
            return ProcessRequest(url, HttpVerbs.Post, formData, headers);
        }

        public RequestResult Post(string url, object formData, string acceptHeader)
        {
            return Post(url, formData, acceptHeader, "", "");
        }

        public RequestResult Post(string url, object formData, string acceptHeader, string authorizeUsername, string authorizePassword)
        {
            return ProcessRequest(url, HttpVerbs.Post, formData, acceptHeader, authorizeUsername, authorizePassword);
        }

        public RequestResult Delete(string url)
        {
            return Delete(url, "");
        }

        public RequestResult Delete(string url, string acceptHeader)
        {
            return Delete(url, acceptHeader, "", "");
        }

        public RequestResult Delete(string url, string acceptHeader, string authorizeUsername, string authorizePassword)
        {
            return ProcessRequest(url, HttpVerbs.Delete, new NameValueCollection(), acceptHeader, authorizeUsername, authorizePassword);
        }

        private RequestResult ProcessRequest(string url, HttpVerbs httpVerb, object formData, string acceptHeader, string authorizeUsername, string authorizePassword)
        {

            var headers = new NameValueCollection();
            if (!String.IsNullOrEmpty(acceptHeader))
            {
                headers.Add("Accept", acceptHeader);
                // -> http://en.wikipedia.org/wiki/List_of_HTTP_header_fields
            }
            if (!String.IsNullOrEmpty(authorizeUsername))
            {
                headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizeUsername + ":" + authorizePassword)));
                // -> http://en.wikipedia.org/wiki/Basic_access_authentication
            }

            var formNameValueCollection = NameValueCollectionConversions.ConvertFromObject(formData);
            return ProcessRequest(url, httpVerb, formNameValueCollection, headers);

        }

        private RequestResult ProcessRequest(string url, HttpVerbs httpVerb, object formValues, NameValueCollection headers)
        {
            var formNameValueCollection = NameValueCollectionConversions.ConvertFromObject(formValues);

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            // Fix up URLs that incorrectly start with / or ~/
            if (url.StartsWith("~/"))
                url = url.Substring(2);
            else if (url.StartsWith("/"))
                url = url.Substring(1);

            // Parse out the querystring if provided
            var query = "";
            var querySeparatorIndex = url.IndexOf("?", StringComparison.Ordinal);
            if (querySeparatorIndex >= 0)
            {
                query = url.Substring(querySeparatorIndex + 1);
                url = url.Substring(0, querySeparatorIndex);
            }

            // Perform the request
            LastRequestData.Reset();
            var output = new StringWriter();
            var httpVerbName = httpVerb.ToString().ToLower();
            var workerRequest = new SimulatedWorkerRequest(url, query, output, Cookies, httpVerbName, formNameValueCollection, headers);
            HttpRuntime.ProcessRequest(workerRequest);

            // Capture the output
            AddAnyNewCookiesToCookieCollection();
            Session = LastRequestData.HttpSessionState;
            return new RequestResult
            {
                ResponseText = output.ToString(),
                ActionExecutedContext = LastRequestData.ActionExecutedContext,
                ResultExecutedContext = LastRequestData.ResultExecutedContext,
                Response = LastRequestData.Response,
            };
        }

        private void AddAnyNewCookiesToCookieCollection()
        {
            if (LastRequestData.Response == null)
                return;

            var lastResponseCookies = LastRequestData.Response.Cookies;


            foreach (string cookieName in lastResponseCookies)
            {
                var cookie = lastResponseCookies[cookieName];
                if (Cookies[cookieName] != null)
                    Cookies.Remove(cookieName);
                if ((cookie.Expires == default(DateTime)) || (cookie.Expires > DateTime.Now))
                    Cookies.Add(cookie);
            }
        }
    }
}
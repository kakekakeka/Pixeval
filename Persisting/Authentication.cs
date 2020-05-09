﻿// Pixeval - A Strong, Fast and Flexible Pixiv Client
// Copyright (C) 2019-2020 Dylech30th
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using EmbedIO;
using Pixeval.Data.Web;
using Pixeval.Data.Web.Delegation;
using Pixeval.Data.Web.Protocol;
using Pixeval.Data.Web.Request;
using Pixeval.Objects;
using Pixeval.Objects.Exceptions;
using Pixeval.Objects.Primitive;
using Pixeval.Persisting.WebApi;
using Refit;

namespace Pixeval.Persisting
{
    /// <summary>
    ///     A helper class to process the Pixiv authentication
    /// </summary>
    public class Authentication
    {
        private const string ClientHash = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";

        private static string UtcTimeNow => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00");

        /// <summary>
        ///     App-API authentication process using account and password
        /// </summary>
        /// <param name="name">account</param>
        /// <param name="pwd">password</param>
        /// <returns></returns>
        public static async Task AppApiAuthenticate(string name, string pwd)
        {
            var time = UtcTimeNow;
            var hash = (time + ClientHash).Hash<MD5CryptoServiceProvider>();

            try
            {
                var token = await RestService.For<ITokenProtocol>(HttpClientFactory.PixivApi(ProtocolBase.OAuthBaseUrl, true).Apply(h => h.Timeout = TimeSpan.FromSeconds(10)))
                    .GetTokenByPassword(new PasswordTokenRequest {Name = name, Password = pwd}, time, hash);
                Session.Global = Session.Parse(pwd, token);
            }
            catch (TaskCanceledException)
            {
                throw new AuthenticateFailedException(StringResources.AppApiAuthenticateTimeout);
            }
        }

        /// <summary>
        ///     App-API authentication process using specified refresh token
        /// </summary>
        /// <param name="refreshToken">refresh token</param>
        /// <returns></returns>
        public static async Task AppApiAuthenticate(string refreshToken)
        {
            try
            {
                var token = await RestService.For<ITokenProtocol>(HttpClientFactory.PixivApi(ProtocolBase.OAuthBaseUrl, true).Apply(h => h.Timeout = TimeSpan.FromSeconds(10)))
                    .RefreshToken(new RefreshTokenRequest {RefreshToken = refreshToken});
                Session.Global = Session.Parse(Session.Global.Password, token);
            }
            catch (TaskCanceledException)
            {
                throw new AuthenticateFailedException(StringResources.AppApiAuthenticateTimeout);
            }
        }

        /// <summary>
        ///     Authentication process to pixiv web api, which is driven by
        ///     <a href="https://github.com/cefsharp/CefSharp">CefSharp</a>
        ///     This method is for login usage only, USE AT YOUR OWN RISK
        /// </summary>
        /// <param name="name">user name</param>
        /// <param name="pwd">user password</param>
        /// <returns></returns>
        public static async Task WebApiAuthenticate(string name, string pwd)
        {
            // create x.509 certificate object for intercepting https traffic, USE AT YOUR OWN RISK
            var certificate = await CertificateManager.GetFakeServerCertificate();
            // create https reverse proxy server for intercepting and forwarding https traffic,
            // default port is 1234
            using var proxyServer = HttpsProxyServer.Create("127.0.0.1", 1234, (await new PixivApiDnsResolver().Lookup("pixiv.net"))[0].ToString(), certificate);
            // create pac file server for providing the proxy-auto-configuration file,
            // which is driven by EmbedIO, this is because CefSharp do not accept file uri,
            // default port is 4321
            using var pacServer = PacFileServer.Create("127.0.0.1", 4321);
            pacServer.Start();
            const string loginUrl = "https://accounts.pixiv.net/login";
            var chrome = new ChromiumWebBrowser(loginUrl) {RequestHandler = new BypassProxyRequestHandler()};
            chrome.FrameLoadEnd += (sender, args) =>
            {
                // when the login page is loaded, we will execute the following js snippet
                // which is going to fill and submit the form
                if (args.Url == loginUrl)
                    // ReSharper disable once AccessToDisposedClosure
                    chrome.ExecuteScriptAsync(
                        $@"
                                var container_login = document.getElementById('container-login');
                                var fields = container_login.getElementsByClassName('input-field');
                                var account = fields[0].getElementsByTagName('input')[0]
                                var password = fields[1].getElementsByTagName('input')[0]
                                account.value = '{name}';
                                password.value = '{pwd}';
                                document.getElementById('container-login').getElementsByClassName('signup-form__submit')[0].click()  
                        ");
            };

            // polling to check we have got the correct Cookie, which names PHPSESSID and
            // has a form like "numbers_hash"
            static bool PixivCookieRecorded(Cookie cookie)
            {
                return Regex.IsMatch(cookie.Domain, ".*\\.pixiv\\.net")
                       && cookie.Name == "PHPSESSID"
                       && Regex.IsMatch(cookie.Value, "\\d+_.*");
            }

            // create an asynchronous polling task while the authenticate process is running,
            // it will check the Cookie
            await chrome.AwaitAsync(async c =>
            {
                var visitor = new TaskCookieVisitor();
                Cef.GetGlobalCookieManager().VisitAllCookies(visitor);
                return (await visitor.Task).Any(PixivCookieRecorded);
            }, 500, TimeSpan.FromSeconds(30));

            // check if we have got the Cookie when the time limit is exceeded, return successfully
            // if it does, otherwise throw an exception
            var completionVisitor = new TaskCookieVisitor();
            Cef.GetGlobalCookieManager().VisitAllCookies(completionVisitor);

            // finalizing objects and save the cookie to user identity
            if ((await completionVisitor.Task).FirstOrDefault(PixivCookieRecorded) is { } cookie)
            {
                chrome.Dispose();

                if (Session.Global == null) Session.Global = new Session();
                Session.Global.PhpSessionId = cookie.Value;
                Session.Global.CookieCreation = cookie.Creation.ToLocalTime();
                return;
            }

            chrome.Dispose();
            throw new AuthenticateFailedException(StringResources.WebApiAuthenticateTimeout);
        }
    }
}
namespace ServiceStack_Auth0_Sample.App_Start
{
     using ServiceStack;
    using ServiceStack.Auth;
    using ServiceStack.Configuration;
    using ServiceStack.Text;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    
    public class Auth0Provider : OAuthProvider
    {
        public const string Name = "auth0";

        public Auth0Provider(IAppSettings appSettings, string realm)
            : base(appSettings, realm, Name)
        {
            this.AuthRealm = realm;
            this.AppId = appSettings.GetString("oauth.auth0.AppId");
            
            if (string.IsNullOrWhiteSpace(this.AppId)) 
            { 
                throw new ArgumentNullException("oauth.auth0.AppId"); 
            }

            this.AppSecret = appSettings.GetString("oauth.auth0.AppSecret");
            
            if (string.IsNullOrWhiteSpace(this.AppSecret)) 
            { 
                throw new ArgumentNullException("oauth.auth0.AppSecret"); 
            }

            this.Connection = appSettings.GetString("oauth.auth0.DefaultConnection");

            // Construct URLs based on realm
            this.PreAuthUrl = realm + "/authorize";
            this.AccessTokenUrl = realm + "/oauth/token";
            this.UserInfoUrl = realm + "/userinfo";
        }

        public string AppId { get; set; }

        public string AppSecret { get; set; }

        public string Connection { get; set; }

        public string PreAuthUrl { get; set; }

        public string UserInfoUrl { get; set; }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);
            var queryString = authService.Request.QueryString;

            var error = queryString["error"];

            if (!string.IsNullOrEmpty(error))
            {
                var error_description = queryString["error_description"];
                return authService.Redirect(session.ReferrerUrl
                                                    .AddHashParam("error", error)
                                                    .AddHashParam("error_description", error_description));
            }

            var code = queryString["code"];
            var isPreAuthCallback = !string.IsNullOrWhiteSpace(code);

            if (!isPreAuthCallback)
            {
                var connection = queryString["connection"] ?? this.Connection;
                var preAuthUrl = 
                    this.PreAuthUrl + 
                    string.Format(
                        "?client_id={0}&redirect_uri={1}&response_type=code&connection={2}",
                        this.AppId, 
                        HttpUtility.UrlEncode(CallbackUrl), 
                        connection);

                authService.SaveSession(session, this.SessionExpiry);

                return authService.Redirect(preAuthUrl);
            }

            try
            {
                var entity = new StringBuilder()
                    .Append(string.Format("client_id={0}&", this.AppId))
                    .Append(string.Format("client_secret={0}&", this.AppSecret))
                    .Append(string.Format("code={0}&", code))
                    .Append(string.Format("grant_type={0}&", "authorization_code"))
                    .Append(string.Format("redirect_uri={0}&", HttpUtility.UrlEncode(CallbackUrl)))
                    .Append(string.Format("type={0}", "web_server"))
                    .ToString();

                var tokenRequest = WebRequest.Create(this.AccessTokenUrl);
                tokenRequest.ContentType = "application/x-www-form-urlencoded";
                tokenRequest.ContentLength = entity.Length;
                tokenRequest.Method = "POST";

                using (Stream requestStream = tokenRequest.GetRequestStream())
                {
                    var writer = new StreamWriter(requestStream);
                    writer.Write(entity);
                    writer.Flush();
                }

                var tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();
                if (tokenResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(tokenResponse.GetResponseStream()))
                    {
                        var obj = JsonObject.Parse(reader.ReadToEnd());
                        if (obj != null)
                        {
                            tokens.AccessTokenSecret = obj.Get("access_token");
                            session.IsAuthenticated = true;
                            authService.SaveSession(session, this.SessionExpiry);
                            this.OnAuthenticated(authService, session, tokens, obj);

                            // Haz access!
                            return authService.Redirect(session.ReferrerUrl.AddHashParam("s", "1"));
                        }
                    }
                }
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "AccessTokenFailed"));
                }
            }

            // Shouldn't get here
            return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "Unknown"));
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
        {
            try
            {
                var tokenRequest = WebRequest.Create(this.UserInfoUrl + "?access_token=" + tokens.AccessTokenSecret);
                var tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();

                if (tokenResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(tokenResponse.GetResponseStream()))
                    {
                        var obj = JsonObject.Parse(reader.ReadToEnd());

                        // Map all standard attributes if present
                        userSession.Id = obj.Get("user_id");
                        userSession.UserName = obj.Get("nickname");
                        userSession.DisplayName = obj.Get("name");
                        userSession.FirstName = obj.Get("given_name");
                        userSession.LastName = obj.Get("family_name");
                        userSession.Email = obj.Get("email");
                        userSession.Gender = obj.Get("gender");
                        
                        // Map any "groups" to "roles"
                        if (obj.Keys.Contains("groups"))
                        {
                            var groups = obj.Get<string[]>("groups");
                            userSession.Roles = new List<string>();
                            userSession.Roles.AddRange(groups);
                        }

                        // Load all properties from Auth0 User Profile into the Dictionary
                        var auth0Session = userSession as Auth0UserSession;
                        if (auth0Session != null)
                        {
                            // Skip complex proprties: 'identitites' and 'emails'
                            string[] skipProperties = { "identities", "emails" };
                            
                            obj.Keys.Where(k => !skipProperties.Contains(k)).ToList()
                                    .ForEach(k => auth0Session.ExtraData[k] = obj.Get(k));
                        }

                        this.LoadUserOAuthProvider(userSession, tokens);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve auth0 user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }
        }
    }
}
namespace ServiceStack_Auth0_Sample.App_Start
{
	using ServiceStack;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A simple custom User Session that stores all simple properties.
    /// </summary>
    public class Auth0UserSession : AuthUserSession
    {
        public Auth0UserSession()
        {
            this.ExtraData = new Dictionary<string, string>();
        }

        public Dictionary<string, string> ExtraData { get; set; }
    }
}
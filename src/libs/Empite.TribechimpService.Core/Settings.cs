namespace Empite.TribechimpService.Core
{
    public class Settings
    {
        public string AppId { get; set; }
        public string SecretKey { get; set; }
        public string IdentityUrl { get; set; }
        public string IdentityAppId { get; set; }
        public string IdentityApiKey { get; set; }
        public ApiSettings ApiSettings { get; set; }
        public ZohoAccount ZohoAccount { get; set; }
        public HangFireConnectionSettings HangFireConnectionSettings { get; set; }
    }

    public class ApiSettings
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public string Contact { get; set; }
        public string Toc { get; set; }
    }

    public class ZohoAccount
    {
        public string RefreshToken { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthTokenUrl { get; set; }
        public string Redirecturi { get; set; }
        public string Granttype { get; set; }
        public int OAuthTokenExpireInSec { get; set; }
        public string ApiBasePath { get; set; }
        public int PaymentTerm { get; set; }
    }
    /// <summary>
    /// Class HangFireConnectionSettings.
    /// </summary>
    public class HangFireConnectionSettings
    {
        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>The server.</value>
        public string Server { get; set; }
        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>The database.</value>
        public string Database { get; set; }
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public string User { get; set; }
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        #region Overrides of Object

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"server={Server};port={Port};database={Database};uid={User};password={Password};SslMode=none;Allow User Variables=True; IgnoreCommandTransaction=true;";
        }

        #endregion
    }
}

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
        public MySqlConnectionString CoreConnection { get; set; }
        public ZohoAccount ZohoAccount { get; set; }
    }

    public class MySqlConnectionString
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public override string ToString()
        {
            return $"server={this.Server};port={Port};database={Database};uid={User};password={Password};SslMode=none;allow user variables=true";
        }
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

}

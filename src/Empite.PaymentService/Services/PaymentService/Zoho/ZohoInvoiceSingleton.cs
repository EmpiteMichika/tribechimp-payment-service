using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Empite.Core.Extensions;
using Empite.PaymentService.Interface.Service.Zoho;
using Empite.PaymentService.Models.Configs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.PaymentService.Services.PaymentService.Zoho
{
    public class ZohoInvoiceSingletonTokenService : IZohoInvoiceSingleton
    {
        private DateTime _tokenTime;
        private string OAuthToken = null;
        private readonly Settings _settings;
        
        public ZohoInvoiceSingletonTokenService(IOptions<Settings> options)
        {
            _settings = options.Value;
            _tokenTime = DateTime.UtcNow;
        }

        public async Task<string> GetOAuthToken()
        {
            if (ZohoInvoiceSingletonStatics._isInRetrivingOAuthToken)
            {
                while ((string.IsNullOrWhiteSpace(OAuthToken) || (_tokenTime - DateTime.UtcNow).Seconds < 0))
                {
                    await Task.Delay(10);
                }
                return OAuthToken;
            }

            ZohoInvoiceSingletonStatics._isInRetrivingOAuthToken = true;

            try
            {
                if (string.IsNullOrWhiteSpace(OAuthToken) || (_tokenTime - DateTime.UtcNow).Seconds < 0)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        string url = _settings.ZohoAccount.AuthTokenUrl;
                        Dictionary<string, string> parameters = new Dictionary<string, string>
                    {
                        { "refresh_token",_settings.ZohoAccount.RefreshToken },
                        { "client_id",_settings.ZohoAccount.ClientId },
                        {"client_secret",_settings.ZohoAccount.ClientSecret },
                        {"redirect_uri",_settings.ZohoAccount.Redirecturi },
                        {"grant_type",_settings.ZohoAccount.Granttype }
                    };
                        FormUrlEncodedContent content = new FormUrlEncodedContent(parameters);
                        HttpResponseMessage response = await httpClient.PostAsync(url, content);
                        if (response.IsSuccessStatusCode)
                        {
                            var byteArray = await response.Content.ReadAsByteArrayAsync();
                            var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                            AuthTokenApiResponse tokenObj =
                                JsonConvert.DeserializeObject<AuthTokenApiResponse>(responseString);
                            if (!string.IsNullOrWhiteSpace(tokenObj.access_token))
                            {
                                int expTime = tokenObj.expires_in_sec != null ? tokenObj.expires_in_sec.AsInt() : _settings.ZohoAccount.OAuthTokenExpireInSec;
                                expTime = (expTime - (expTime / 100) * 70);
                                _tokenTime = DateTime.UtcNow.AddSeconds(expTime);
                                OAuthToken = tokenObj.access_token;

                            }
                            else
                            {
                                throw new Exception($"Zoho Response has empty access token");
                            }

                        }
                        else
                        {
                            throw new Exception($"Zoho OAuthToken retrive Faild Reason => {response.ReasonPhrase}");
                        }
                    }
                }

                ZohoInvoiceSingletonStatics._isInRetrivingOAuthToken = false;
            }
            catch (Exception ex)
            {
                ZohoInvoiceSingletonStatics._isInRetrivingOAuthToken = false;
                throw ex;
            }
            return OAuthToken;
        }
        #region response classes
        internal class AuthTokenApiResponse
        {
            public string access_token { get; set; }
            public int? expires_in_sec { get; set; }
            public string api_domain { get; set; }
            public string token_type { get; set; }
            public int? expires_in { get; set; }
        }

        #endregion
    }

    public static class ZohoInvoiceSingletonStatics
    {
        public static bool _isInRetrivingOAuthToken { get; set; } = false;
    }
}

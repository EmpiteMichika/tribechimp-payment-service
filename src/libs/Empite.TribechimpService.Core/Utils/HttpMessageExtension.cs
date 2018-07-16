// ***********************************************************************
// Assembly         : RecruiterinsiderPortal.Core
// Author           : Gayan Ranasinghe
// Created          : 10-03-2017
//
// Last Modified By : Gayan Ranasinghe
// Last Modified On : 10-17-2017
// ***********************************************************************
// <copyright file="HttpMessageExtension.cs" company="Empite Solutions">
//     Copyright (c) Empite Solutions. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Empite.TribechimpService.Core.Utils
{
    /// <summary>
    /// Class HttpMessageExtension.
    /// </summary>
    public static class HttpMessageExtension
    {
        /// <summary>
        /// Sets the content of the post.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="url">The URL.</param>
        /// <param name="content">The content.</param>
        /// <returns>HttpRequestMessage.</returns>
        public static HttpRequestMessage SetPostContent(this HttpRequestMessage request, string url, JsonDataContent content)
        {
            request.Method = HttpMethod.Post;
            request.Content = content;
            request.RequestUri = new Uri(url, UriKind.Relative);
            return request;
        }

        /// <summary>
        /// set header as an asynchronous operation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="appId">The application identifier.</param>
        /// <returns>Task&lt;HttpRequestMessage&gt;.</returns>
        public static async Task<HttpRequestMessage> SetHeaderAsync(this HttpRequestMessage request, string apiKey, string appId)
        {
            var method = request.Method.Method;
            var timestamp = TimeStamp();
            var nonce = Guid.NewGuid().ToString("N");
            var hash = string.Empty;
            var url = $"/{request.RequestUri.OriginalString}";
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsByteArrayAsync();
                var md5 = MD5.Create();
                var bytes = md5.ComputeHash(content);
                hash = Convert.ToBase64String(bytes);
            }
            var data = $"{appId}{method}{url}{timestamp}{nonce}{hash}";
            var signature = Encoding.UTF8.GetBytes(data);
            var key = Convert.FromBase64String(apiKey);
            using (var hmac = new HMACSHA256(key))
            {
                var signatureBytes = hmac.ComputeHash(signature);
                var base64String = Convert.ToBase64String(signatureBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("hmac", $"{appId}:{base64String}:{nonce}:{timestamp}");
            }

            return request;
        }


        /// <summary>
        /// Times the stamp.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string TimeStamp()
        {
            var start = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            var span = DateTime.UtcNow - start;
            var stamp = Convert.ToUInt64(span.TotalSeconds).ToString();
            return stamp;
        }
    }
}

// ***********************************************************************
// Assembly         : RecruiterinsiderPortal.Core
// Author           : Gayan Ranasinghe
// Created          : 10-03-2017
//
// Last Modified By : Gayan Ranasinghe
// Last Modified On : 01-23-2018
// ***********************************************************************
// <copyright file="JsonDataContent.cs" company="Empite Solutions">
//     Copyright (c) Empite Solutions. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Empite.TribechimpService.Core.Utils
{
    /// <summary>
    /// Class JsonDataContent.
    /// </summary>
    /// <seealso cref="System.Net.Http.StringContent" />
    public class JsonDataContent : StringContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDataContent" /> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public JsonDataContent(object obj) : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        { }
    }
}

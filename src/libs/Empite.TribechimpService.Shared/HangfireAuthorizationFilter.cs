using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.Dashboard;

namespace Empite.TribechimpService.Shared
{
    public class HangfireAuthorizationFilter: IDashboardAuthorizationFilter
    {
        /// <summary>
        /// Authorizes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}

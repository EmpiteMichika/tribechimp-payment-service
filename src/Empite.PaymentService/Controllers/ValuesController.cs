using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Empite.Core.Infrastructure.Constant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RawRabbit;

namespace Empite.TribechimpService.PaymentService.Controllers
{
    [Route(ApplicationConstant.API_ROUTE)]
    [Authorize(AuthenticationSchemes = ApplicationConstant.HMAC_AUTH_SCHEMA)]
    [ApiController]
    public class ValuesController : Controller
    {
        private readonly IBusClient _client;

        public ValuesController(IBusClient client)
        {
            _client = client;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return Ok(new {Value = "value"});
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [HttpGet]
        [Route("rabbit")]
        public async Task<IActionResult> TestRAbbitMq()
        {
            var messsage = "This is from core service";
            await _client.PublishAsync(new JiraRequest { Messsage = messsage });
            return Ok();
        }
    }
}

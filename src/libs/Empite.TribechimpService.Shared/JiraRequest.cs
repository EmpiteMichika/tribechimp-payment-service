using System;
using System.Collections.Generic;
using System.Text;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Enrichers.Attributes;

namespace Empite.TribechimpService.Shared
{
    [Exchange(Type = ExchangeType.Topic, Name = "jira.exchange")]
    [Queue(Name = "jira.queue", Durable = false)]
    [Routing(RoutingKey = "jira.service")]
    public class JiraRequest
    {
        public string Messsage { get; set; }
    }
}

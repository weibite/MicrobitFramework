using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microbit.RabbitMQ;

namespace Microbit.Console
{
    [RabbitMQEntity(Exchange = "Exchange.Microbit.UserInfo", Queue = "Queue.Microbit.UserInfo", IsProperties = true)]
    [Serializable]
    public class UserInfo
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string RealName { get; set; }
    }
}

using Microbit.RabbitMQ;

namespace Microbit.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 1; i < 5; i++)
            {
                UserInfo userinfo = new UserInfo { UserName = "陈亚" + i.ToString(), Password = "123456" };
                RabbitMQWrapper.Publish(userinfo);
            }

            RabbitMQWrapper.Subscribe<UserInfo>(x =>
            {
                System.Console.WriteLine(x.UserName);
            });

            System.Console.ReadLine();
        }
    }
}

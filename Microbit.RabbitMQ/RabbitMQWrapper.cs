using System;
using System.Linq;
using System.Text;
using System.Configuration;
using RabbitMQ.Client;
using Newtonsoft.Json;
using Microbit.Utils;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;
using System.Threading;

namespace Microbit.RabbitMQ
{
    /// <summary>
    /// RabbitMQ封装类
    /// </summary>
    public static class RabbitMQWrapper
    {
        private static readonly IConnection connection = null;

        static RabbitMQWrapper()
        {
            string config = ConfigurationManager.AppSettings["microbit.rabbitmq"];
            ConnectionFactory factory = new ConnectionFactory();
            factory.AutomaticRecoveryEnabled = true;//设置自动恢复连接
            factory.NetworkRecoveryInterval = new TimeSpan(1000);
            factory.Uri = string.Format("amqp://{0}", config);
            try
            {
                connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        public static void Publish<T>(T entity) where T : class
        {
            using (var channel = connection.CreateModel())
            {
                //获取消息实体所属的交换机和队列
                string exchange = "Microbit.Exchange.*";
                string queue = "Microbit.Queue.*";
                var attr = GetAttributes<T>();
                if (attr != null)
                {
                    exchange = attr.Exchange;
                    queue = attr.Queue;
                }
                //声明交换机
                channel.ExchangeDeclare(exchange, "direct");
                //声明队列，消息持久化，防止丢失
                channel.QueueDeclare(queue, true, false, false, null);
                //绑定交换机和队列
                channel.QueueBind(queue, exchange, queue);
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.DeliveryMode = 2;
                //消息转换为二进制
                string message = JsonConvert.SerializeObject(entity);
                var msgBody = Encoding.UTF8.GetBytes(message);
                //发布消息
                channel.BasicPublish(exchange, queue, properties, msgBody);
            }
        }

        /// <summary>
        /// 同步订阅模式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public static void Subscribe<T>(Action<T> action) where T : class
        {
            string queue;//队列名
            //创建channel
            var channel = CreateChannel<T>(out queue);
            var consumer = new EventingBasicConsumer(channel);
            //订阅事件
            consumer.Received += (sender, args) =>
            {
                try
                {
                    byte[] body = args.Body;
                    string message = Encoding.UTF8.GetString(body);
                    var obj = JsonConvert.DeserializeObject<T>(message);
                    action(obj);//执行客户端消费事件
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                    //消费失败，重新放入队列头
                    channel.BasicReject(args.DeliveryTag, true);
                }
                finally
                {
                    channel.BasicAck(args.DeliveryTag, false);
                }
            };
            string consumerTag = channel.BasicConsume(queue, false, consumer);
        }

        /// <summary>
        /// 异步订阅模式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public static void SubscribeAsync<T>(Action<T> action) where T : class
        {
            string queue;//队列名
            //创建channel
            var channel = CreateChannel<T>(out queue);
            //创建事件驱动的消费者类型，而不是用while死循环来消费消息
            var consumer = new EventingBasicConsumer(channel);
            //订阅事件
            consumer.Received += (sender, args) =>
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        byte[] body = args.Body;
                        string message = Encoding.UTF8.GetString(body);
                        var obj = JsonConvert.DeserializeObject<T>(message);
                        action(obj);//执行客户端消费事件
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.ToString());
                    }             
                });
                channel.BasicAck(args.DeliveryTag, false);
            };
            string consumerTag = channel.BasicConsume(queue, false, consumer);
        }

        /// <summary>
        /// 创建Channel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <returns></returns>
        private static IModel CreateChannel<T>(out string queue) where T : class
        {
            var channel = connection.CreateModel();
            //获取消息实体所属的交换机和队列
            string exchange = "Microbit.Exchange.*";
            queue = "Microbit.Queue.*";
            var attr = GetAttributes<T>();
            if (attr != null)
            {
                exchange = attr.Exchange;
                queue = attr.Queue;
            }
            //声明队列，消息持久化，防止丢失
            channel.QueueDeclare(queue, true, false, false, null);
            //同时只消费一个消息
            channel.BasicQos(0, 1, false);
            return channel;
        }

        /// <summary>
        /// 获取类的RabbitMQEntity特性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static RabbitMQEntityAttribute GetAttributes<T>() where T : class
        {
            var type = typeof(T);
            RabbitMQEntityAttribute attribute = type.GetCustomAttributes(typeof(RabbitMQEntityAttribute), false).FirstOrDefault() as RabbitMQEntityAttribute;
            return attribute;
        }
    }
}

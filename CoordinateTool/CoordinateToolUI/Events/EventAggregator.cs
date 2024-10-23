using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Prism.Ioc;

namespace CoordinateToolUI.Events
{


    public static class PublishEvent
    {
        public static readonly IEventAggregator EventAggregator;
        static PublishEvent()
        {
            EventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
        }

        public static void BoxMessage(MessageType message)
        {
            EventAggregator.GetEvent<MessageEvent>().Publish(message);
        }

    }

    public class MessageType
    {
        public MessageType(string t = "", string m = "")
        {
            Title = t;
            Message = m;
        }

        public string Title;
        public string Message;

    }

    public class MessageEvent : PubSubEvent<MessageType>
    {
    }





}

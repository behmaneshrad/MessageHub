using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailReceiver.Domain.Interfaces
{
	public interface IMessageBroker
	{
		Task PublishAsync<T>(T message, string topic = null) where T : class;
	}
}
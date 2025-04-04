using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Common.Models
{
	namespace MessageBroker.Common.Models
	{
		public class OutboxMessage
		{
			public string Id { get; set; }
			public string Topic { get; set; }
			public string MessageType { get; set; }
			public string MessageJson { get; set; }
			public DateTime CreatedDate { get; set; }
			public DateTime? ProcessedDate { get; set; }
			public string Status { get; set; } // "Pending", "Processed", "Failed"
			public string ErrorMessage { get; set; }
		}
	}
}

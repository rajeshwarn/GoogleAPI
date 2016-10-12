using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace GoogleAPI
{
	public class GoogleProject
	{
		public GoogleProject()
		{
			Files = new List<GoogleScript>();
		}

		[JsonProperty("files")]
		public List<GoogleScript> Files { get; }

		public class GoogleScript
		{
			[JsonProperty("id")]
			public string Id { get; set; }

			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("type")]
			public string Type { get; set; }

			[JsonProperty("source")]
			public string Source { get; set; }
		}
	}
}

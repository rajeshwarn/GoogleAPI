using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAPI
{
	public class GoogleProject
	{
		public GoogleProject()
		{
			files = new List<GoogleScript>();
		}
		public List<GoogleScript> files { get; }

		public class GoogleScript
		{
			public string id { get; set; }
			public string name { get; set; }
			public string type { get; set; }
			public string source { get; set; }
		}
	}
}

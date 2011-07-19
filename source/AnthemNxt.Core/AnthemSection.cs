using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace AnthemNxt.Core
{
	public class AnthemSection : ConfigurationSection
	{
		/// <summary>
		/// When true, AnthemNxt.Manager will include all page level scripts in the callback response. Use this if you add or show 3rd party controls during 
		/// the callback that add or change client scripts in the page.
		/// </summary>
		[ConfigurationProperty("includePageScripts", DefaultValue = false, IsRequired = false)]
		public bool IncludePageScripts
		{
			get { return (bool)this["includePageScripts"]; }
			set { this["includePageScripts"] = value; }
		}

		[ConfigurationProperty("responseEncoding", DefaultValue = null, IsRequired = false)]
		public string ResponseEncoding
		{
			get { return (string)this["responseEncoding"]; }
			set { this["responseEncoding"] = value; }
		}

		[ConfigurationProperty("responseContentType", DefaultValue = "application/json", IsRequired = false)]
		public string ResponseContentType
		{
			get { return (string)this["responseContentType"]; }
			set { this["responseContentType"] = value; }
		}
	}
}

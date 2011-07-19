using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;

namespace AnthemNxt.Core
{
	public static class ScriptManager
	{
		/// <summary>
		/// Add the script to a list of scripts to be evaluated on the client during the
		/// callback response processing.
		/// </summary>
		/// <remarks>To not include &lt;script&gt;&lt;/script&gt; tags.</remarks>
		/// <example>
		/// 	<code lang="CS" title="[New Example]" description="This example adds an alert message that will be displayed on the client during callback response processing.">
		/// AnthemNxt.Manager.AddScriptForClientSideEval("alert('Hello');");
		///     </code>
		/// </example>
		/// <param name="script">The script to evaluate.</param>
		public static void AddScriptForClientSideEval(string script)
		{
			// TODO: Why can this not just be done with the usual ScriptManager replacements in this class?
			Manager.Current.clientSideEvalScripts.Add(script);
		}

		public static void RegisterClientScriptBlock(Type type, string key, string script)
		{
			RegisterClientScriptBlock(type, key, script, false);
		}

		public static void RegisterClientScriptBlock(Type type, string key, string script, bool addScriptTags)
		{
			Page page = HttpContext.Current.Handler as Page;
			if(page != null) page.ClientScript.RegisterClientScriptBlock(type, key, script, addScriptTags);
			RegisterPageScriptBlock(key, script);
		}

		public static void RegisterClientScriptInclude(string key, string url)
		{
			RegisterClientScriptInclude(typeof(Page), key, url);
		}

		public static void RegisterClientScriptInclude(Type type, string key, string url)
		{
			Page page = HttpContext.Current.Handler as Page;
			if(page != null) page.ClientScript.RegisterClientScriptInclude(type, key, url);
			RegisterPageScriptBlock(key, "src=" + url);
		}

		public static void RegisterClientScriptResource(Type type, string resourceName)
		{
			Page page = HttpContext.Current.Handler as Page;
			if(page != null) page.ClientScript.RegisterClientScriptResource(type, resourceName);
			RegisterPageScriptBlock(resourceName, "src=" + page.ClientScript.GetWebResourceUrl(type, resourceName));
		}

		public static void RegisterStartupScript(Type type, string key, string script)
		{
			RegisterStartupScript(type, key, script, false);
		}

		public static void RegisterStartupScript(Type type, string key, string script, bool addScriptTags)
		{
			Page page = HttpContext.Current.Handler as Page;
			if(page != null) page.ClientScript.RegisterStartupScript(type, key, script, addScriptTags);
			RegisterPageScriptBlock(key, script);
		}

		private static void RegisterPageScriptBlock(string key, string script)
		{
			Manager.Current.pageScripts[key] = script;
		}
	}
}

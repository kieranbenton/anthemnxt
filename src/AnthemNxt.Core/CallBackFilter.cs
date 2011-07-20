using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Configuration;
using AnthemNxt.Core.Internals;

namespace AnthemNxt.Core
{
	internal class CallbackFilter : Stream
	{
		private static AnthemSection config = null;

		private static readonly Regex scriptEmbeddedRegex = new Regex(@"<script.*?>(?<script>.*?)</script>", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex scriptTagsRegex = new Regex(@"<script(?<attributes>.*?)(>|/>)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex scriptTypeRegex = new Regex(@"type\s*=\s*['""]text/javascript['""]", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex scriptSrcRegex = new Regex(@"src\s*=\s*['""](?<src>.+?)['""]", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private Manager manager;
		private Stream stream;
		private MemoryStream buffer;

		static CallbackFilter()
		{
			config = (AnthemSection)ConfigurationManager.GetSection("anthem");
			if(config == null) config = new AnthemSection();
		}

		internal CallbackFilter(Manager manager, Stream stream)
		{
			this.manager = manager;
			this.stream = stream;
			this.buffer = new MemoryStream();
		}

		public override bool CanRead { get { return false; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } }
		public override long Length { get { return 0; } }
		public override long Position { get { return 0; } set { } }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) { return 0; }
		public override void SetLength(long value) { }
		public override int Read(byte[] buffer, int offset, int count) { return 0; }
		public override void Write(byte[] buffer, int offset, int count) { this.buffer.Write(buffer, offset, count); }

		public override void Close()
		{
			string viewState = null;
			string viewStateEncrypted = null;
			string eventValidation = null;
			Dictionary<string, string> controls = null;
			List<string> scripts = null;

			if(manager.updatePage)
			{
				// Return the full text of the page
				string html = HttpContext.Current.Response.ContentEncoding.GetString(this.buffer.GetBuffer());

				// Extract the viewstate & eventvalidation values from the response
				viewState = GetHiddenInputValue(html, Markers.ViewstateBegin);
				viewStateEncrypted = GetHiddenInputValue(html, Markers.ViewstateEncryptedBegin);
				eventValidation = GetHiddenInputValue(html, Markers.EventValidationBegin);

				// Get just the html of the controls that have been updated as part of this callback
				controls = GetControls(html);

				// Return a list of all the new scripts present in the page
				scripts = GetScripts(html);

				foreach(object o in manager.targets.Values)
				{
					var c = o as Control;
					if(c != null && !c.Visible && c.ID != null && controls.ContainsKey(c.ID)) 
						controls[c.ID] = "";
				}
			}

			var sb = new StringBuilder();
			try
			{
				JsonWriter.WriteValueAndError(sb, 
					manager.responseValue, manager.responseError, 
					viewState, viewStateEncrypted, eventValidation, 
					controls, 
					scripts, manager.clientSideEvalScripts);
			}
			catch(Exception ex)
			{
				// If an exception was thrown while formatting the result value, we need to discard whatever was written and start over with nothing but the error message
				sb.Length = 0;
				JsonWriter.WriteValueAndError(sb, null, ex.Message, null, null, null, null, null, null);
			}

			// If an IOFrame was used to make this callback, then wrap the response in a <textarea> element so the iframe will not mess with the text of the JSON object
			// If the response text contains any </textarea> tags they will truncate the response on the client. To avoid this, </textarea> tags are converted to </anthemarea> tags here and converted back to </textarea> on the client
			string response = sb.ToString();
			if(string.Compare(HttpContext.Current.Request[RequestID.IOFrame], "true", true) == 0) response = "<textarea id=\"response\">" + Regex.Replace(response, "</textarea>", "</anthemarea>", RegexOptions.IgnoreCase) + "</textarea>";

			byte[] buffer = HttpContext.Current.Response.ContentEncoding.GetBytes(response);
			stream.Write(buffer, 0, buffer.Length);
		}

		private Dictionary<string, string> GetControls(string html)
		{
			var controls = new Dictionary<string, string>();

			// Find the first begin marker.
			int i = html.IndexOf(Markers.ControlBegin);

			// Keep looping while we've got markers.
			while(i != -1)
			{
				i += Markers.ControlBegin.Length;

				// Find the end of the begin marker.
				int j = html.IndexOf("-->", i);
				if(j == -1)
					break;
				else
				{
					// The string between i and j should be the ClientID.
					string id = html.Substring(i, j - i);

					// Point past the end of the begin marker.
					i = j + 3;
					string endMarker = Markers.ControlEnd + id + "-->";

					// Find the end marker for the current control.
					j = html.IndexOf(endMarker, i);
					if(j == -1)
						break;
					else
					{
						// The string between i and j is now the HTML.
						string control = html.Substring(i, j - i);
						controls[id] = control;

						// Point past the end of the end marker.
						i = j + endMarker.Length;
					}
				}

				// Find the next begin marker.
				i = html.IndexOf(Markers.ControlBegin, i);
			}
			return controls;
		}

		private List<string> GetScripts(string html)
		{
			var scripts = new List<string>();

			// Add all of the scripts that were manually registered.
			for(int index = 0; index < manager.pageScripts.Count; index++)
			{
				string script = manager.pageScripts[index];

				// Strip off any <script> tags
				if(scriptEmbeddedRegex.IsMatch(script))
				{
					foreach(Match scriptMatch in scriptEmbeddedRegex.Matches(script))
					{
						string innerScript = scriptMatch.Groups["script"].ToString();
						if(innerScript != "") scripts.Add(innerScript);
					}
				}
				else
					scripts.Add(script);
			}

			// Load the script libraries in case they are used by the embedded scripts
			if(config.IncludePageScripts)
			{
				foreach(Match attributesMatch in scriptTagsRegex.Matches(html))
				{
					string attributes = attributesMatch.Groups["attributes"].ToString().Trim();
					if(scriptTypeRegex.Match(attributes).Success)
					{
						foreach(Match srcMatch in scriptSrcRegex.Matches(attributes))
						{
							string src = srcMatch.Groups["src"].ToString();
							if(src != "") scripts.Add("src=" + src);
						}
					}
				}
			}

			// Now load the embedded scripts
			if(manager.updateValidationScripts || config.IncludePageScripts)
			{
				// These scripts will reset page validation. Page_Validators is an array of validators to be invoked. WebForm_OnSubmit (ASP.NET 2.0) is
				// called by the form's onsubmit event handler by postback (non-callback) controls that cause
				// validation. Resetting these will ensure that no javascript errors occur if validators have been removed from the page. If there are 
				// still validators on the page, then the actual array and function will override these values.
				scripts.Add(@"
//<![CDATA[
var Page_Validators = new Array(); function WebForm_OnSubmit() { return true; }
//]]>");

				// This loop will look for any scripts that were injected into the page. If they are found, then they are added to the page.
				foreach(Match scriptMatch in scriptEmbeddedRegex.Matches(html))
				{
					string script = scriptMatch.Groups["script"].ToString();
					if(script != "")
					{
						// This sequence of regular expressions match all of the client side validation scripts that may be added by the validation controls for both ASP.NET 1.1 and 2.0.
						if(config.IncludePageScripts && script.IndexOf("AnthemNxt.Manager.GetScripts: false") == -1)
							scripts.Add(script);
						else if(Regex.IsMatch(script, "var Page_ValidationSummaries", RegexOptions.IgnoreCase))
							scripts.Add(script);
						else if(Regex.IsMatch(script, "var Page_Validators", RegexOptions.IgnoreCase))
							scripts.Add(script);
						else if(Regex.IsMatch(script, "var Page_ValidationActive", RegexOptions.IgnoreCase))
							scripts.Add(script);
						else if(Regex.IsMatch(script, "function WebForm_OnSubmit", RegexOptions.IgnoreCase))
							scripts.Add(script);
						else if(Regex.IsMatch(script, "\\.evaluationfunction =", RegexOptions.IgnoreCase))
							scripts.Add(script);

						// If validators with client side validation were added to the page during the callback and if there are postback controls  on the page that cause validation, then we need to add an onsubmit event handler to the form that enforces the client side validation. This is how postback (non-callback) controls cause validation to occur.
						// This next script is executed during callback response processing on the client. It will attach the ASP.NET validation function to the onsubmit event of the form.
						if(Regex.IsMatch(script, "function WebForm_OnSubmit", RegexOptions.IgnoreCase))
							scripts.Add(@"
//<![CDATA[
var form = Anthem_GetForm(); if (typeof(form) != ""undefined"" && form != null) Anthem_AddEvent(form, ""onsubmit"", ""return WebForm_OnSubmit();"");
//]]>");
					}
				}
			}

			return scripts;
		}

		private static string GetHiddenInputValue(string html, string marker)
		{
			string value = null;
			int i = html.IndexOf(marker);
			if(i != -1)
			{
				value = html.Substring(i + marker.Length);
				value = value.Substring(0, value.IndexOf('\"'));
			}
			return value;
		}
	}
}

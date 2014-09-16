using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using ASP = System.Web.UI.WebControls;
using System.Web.Configuration;
using System.Collections.Generic;
using AnthemNxt.Core.Internals;
using System.Diagnostics;
using System.Linq;

[assembly: WebResource("AnthemNxt.Core.Anthem.js", "text/javascript")]

namespace AnthemNxt.Core
{
	/// <summary>
	/// The Manager class is responsible for managing all of the interaction between ASP.NET
	/// and the Anthem controls.
	/// TODO: (works either for a normal page request or a callback) 
	/// TODO: Except throws an exception if page is unmanaged? Change this.
	/// </summary>
	public class Manager
	{
		private static AnthemSection config = null;

		internal Hashtable targets = new Hashtable();
		internal bool updatePage;
		internal bool updateValidationScripts;

		internal object responseValue = null;
		internal string responseError = null;
		internal NameValueCollection pageScripts = new NameValueCollection();
		internal List<string> clientSideEvalScripts = new List<string>();

		static Manager()
		{
			// Load the anthem configuration section if one is present
			config = (AnthemSection)ConfigurationManager.GetSection("anthem");
			if(config == null) config = new AnthemSection();
		}

		#region Operations

		public static void Register(Page page)
		{
			Register(page, page);
		}

		public static void Register(Control control)
		{
			Register(control.Page, control);
		}

		public static void HandleError()
		{
			Manager.Current.CompleteRequest();
		}

		public static Manager Current
		{
			get
			{
				Manager manager = HttpContext.Current.Items[RequestID.Manager] as Manager;
				if(manager == null) throw new ApplicationException("This page was never registered with AnthemNxt.Manager!");
				return manager;
			}
		}

		#endregion

		#region Registering elements and initialisation

		private static void Register(Page page, Control control)
		{
			AddManager(page);
			if(page != control) Current.targets[control.ClientID] = control;
		}

		private static void AddManager(Page page)
		{
			Manager manager = HttpContext.Current.Items[RequestID.Manager] as Manager;
			if(manager == null)
			{ 
				manager = new Manager();

				// Hookup the two rendering event handlers we need access to
				page.PreRender += new EventHandler(manager.OnPreRender);
				page.Unload += new EventHandler(manager.OnUnload);

				manager.targets[RequestID.Manager] = manager;
				HttpContext.Current.Items[RequestID.Manager] = manager;
			}

			// References the form on the page with the javascript
			var form = page.GetForm();
			if(form != null)
			{
				page.ClientScript.RegisterClientScriptBlock(typeof(Manager), "pageScript", @"
<script type=""text/javascript"">
//<![CDATA[
var Anthem_FormID = """ + form.ClientID + @""";
//]]>
</script>");
			}

			// Includes a reference to the anthem.js script in the page
			page.ClientScript.RegisterClientScriptResource(typeof(Manager), "AnthemNxt.Core.Anthem.js");
		}

		#endregion

		#region Page Events

		private void OnPreRender(object source, EventArgs e)
		{
			HttpContext context = HttpContext.Current;
			HttpRequest req = context.Request;
			HttpResponse res = context.Response;
			Page page = source as Page;

			if(IsRedirectedToLoginPage || !Manager.IsCallBack) return;

			object targetObject = null;
			string methodName = null;
			bool invokeMethod = true;

			// What kind of callback is this? Method based or event based?
			if(req.Form[RequestID.PageMethod] != null)
			{
				// Targetted a Method on this page
				targetObject = page;
				methodName = req.Form[RequestID.PageMethod];
			}
			else if(req.Form[RequestID.MasterPageMethod] != null)
			{
				// Targetted a Method on the page's master page
				if(page != null)
				{
					// TODO: Since master pages can nest, we might need to do a search for the method up to the root master page.
					targetObject = page.Master;
					methodName = req.Form[RequestID.MasterPageMethod];
				}
			}
			else if(req.Form[RequestID.ControlID] != null && req.Form[RequestID.ControlMethod] != null)
			{
				// Targetted a Method on a control
				targetObject = targets[req.Form[RequestID.ControlID]];
				methodName = req.Form[RequestID.ControlMethod];
			}
			else
			{
				// Otherwise was an event based callback
				invokeMethod = false;
			}

			if(invokeMethod)
			{
				if(targetObject == null)
					responseError = "CONTROLNOTFOUND";
				else
				{
					if(!string.IsNullOrEmpty(methodName))
					{
						MethodInfo methodInfo = FindTargetMethod(targetObject, methodName);
						if(methodInfo == null)
							responseError = "METHODNOTFOUND";
						else
						{
							try
							{
								var parameters = ConvertParameters(methodInfo, req);
								responseValue = InvokeMethod(targetObject, methodInfo, parameters);
							}
							catch(MethodAccessException ex)
							{
								responseError = methodInfo.IsPublic ? ex.Message : string.Format("AnthemNxt.Manager does not have permission to invoke method \"{0}\" in the current trust level. Please try making the method Public.", methodName);
							}
							catch(Exception ex)
							{
								responseError = ex.Message;
							}
						}
					}
				}
			}

			// Pipe the response through our custom filter that will build the partial JSON response
			if(!string.IsNullOrEmpty(config.ResponseEncoding)) res.ContentEncoding = Encoding.GetEncoding(config.ResponseEncoding);
			if(!string.IsNullOrEmpty(config.ResponseContentType)) res.ContentType = config.ResponseContentType;
			res.Cache.SetCacheability(HttpCacheability.NoCache);
			
			// Hook up the callback filter that will construct the JSON to be sent back to the client
			res.Filter = new CallbackFilter(this, res.Filter);

			updatePage = string.Compare(req[RequestID.UpdatePage], "true", true) == 0;
			updateValidationScripts = false;
			if(updatePage)
			{
				// If there are any validators on the page, then include the validation scripts in the callback response in case any validators were added to the page during the callback processing.
				updateValidationScripts = AreValidationScriptsRequired;
				if(updateValidationScripts)
				{
					pageScripts.Add(RequestID.Manager + "WebForm.js", "src=" + page.ClientScript.GetWebResourceUrl(typeof(Page), "WebForms.js"));
					pageScripts.Add(RequestID.Manager + "WebUIValidation.js", "src=" + page.ClientScript.GetWebResourceUrl(typeof(ASP.BaseValidator), "WebUIValidation.js"));
				}
			}
		}

		/// <summary>
		/// Used to catch Response.Redirect() during a callback. If it is a redirect the response is converted back into a normal response and the appropriate javascript is returned to redirect the client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnUnload(object sender, EventArgs e)
		{
			CompleteRequest();
		}

		private void CompleteRequest()
		{
			var res = HttpContext.Current.Response;

			// Handles inserting javascript to do a redirect
			if(Manager.IsCallBack && res.StatusCode == 302)
			{
				string href = res.RedirectLocation.Replace("\\", "\\\\").Replace("'", "\\'");
				res.RedirectLocation = "";
				res.Clear();
				res.StatusCode = 200;

				var sb = new StringBuilder();
				ScriptManager.AddScriptForClientSideEval("try{window.location='" + href + "';}catch(e){}");
				JsonWriter.WriteValueAndError(sb, null, null, null, null, null, null, null, clientSideEvalScripts);
				res.Write(sb.ToString());
				res.End();
			}
		}

		#endregion

		#region Ajax Method Invoking

		private static object[] ConvertParameters(MethodInfo methodInfo, HttpRequest req)
		{
			object[] parameters = new object[methodInfo.GetParameters().Length];
			int i = 0;
			foreach(ParameterInfo paramInfo in methodInfo.GetParameters())
			{
				object param = null;
				string paramValue = req.Form[RequestID.CallBackArgument + i];

				if(paramValue != null)
				{
					if(paramInfo.ParameterType.IsArray)
					{
						Type type = paramInfo.ParameterType.GetElementType();
						string[] values = req.Form.GetValues(RequestID.CallBackArgument + i);
						Array array = Array.CreateInstance(type, values.Length);

						for(int index = 0; index < values.Length; index++)
						{
							array.SetValue(Convert.ChangeType(values[index], type), index);
						}
						param = array;
					}
					else
						param = Convert.ChangeType(paramValue, paramInfo.ParameterType);
				}

				parameters[i] = param;
				++i;
			}
			return parameters;
		}

		private static MethodInfo FindTargetMethod(object target, string methodName)
		{
			Type type = target.GetType();
			MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(methodInfo != null)
			{
				object[] methodAttributes = methodInfo.GetCustomAttributes(typeof(AnthemNxt.Core.MethodAttribute), true);
				if(methodAttributes.Length > 0) return methodInfo;
			}
			return null;
		}

		private static object InvokeMethod(object target, MethodInfo methodInfo, object[] parameters)
		{
			object val = null;
			try
			{
				val = methodInfo.Invoke(target, parameters);
			}
			catch(TargetInvocationException ex)
			{
				// TargetInvocationExceptions should have the actual exception the method threw in its InnerException property.
				if(ex.InnerException != null) throw ex.InnerException;
				throw ex;
			}
			return val;
		}

		/// <summary>
		/// This is an empty method used as the target for the Anthem_FireEvent function. That function sets the
		/// __EVENTTARGET to the desired ID which causes the appropriate event to fire on the server so nothing
		/// needs to be done here.
		/// </summary>
		[AnthemNxt.Core.Method]
		public void FireEvent() { }

		#endregion

		#region Marking Controls for Callback Update + Extracting Them Again (with scripts)

		/// <summary>
		/// This method needs to be called by custom controls that want their innerHTML to be automatically updated on the client pages during
		/// call backs. Call this at the top of the Render override. The parentTagName argument should be "div" or "span" depending on the
		/// type of control. It's this parent element that actually gets its innerHTML updated after call backs.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="parentTagName"></param>
		/// <param name="control"></param>
		public static void WriteBeginControlMarker(HtmlTextWriter writer, string parentTagName, Control control)
		{
			writer.Write("<{0} id=\"{1}\">", parentTagName, "Anthem_" + control.ClientID + "__");
			
			IUpdatableControl updatableControl = control as IUpdatableControl;
			if(updatableControl != null && updatableControl.UpdateAfterCallBack && IsCallBack)
				writer.Write(Markers.ControlBegin + GetUniqueIDWithDollars(control) + "-->");
		}

		/// <summary>
		/// This method needs to be called by custom controls that want their innerHTML to be automatically updated on the client pages during
		/// call backs. Call this at the bottom of the Render override.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="parentTagName"></param>
		/// <param name="control"></param>
		public static void WriteEndControlMarker(HtmlTextWriter writer, string parentTagName, Control control)
		{
			IUpdatableControl updatableControl = control as IUpdatableControl;
			if(updatableControl != null && updatableControl.UpdateAfterCallBack && IsCallBack)
				writer.Write(Markers.ControlEnd + GetUniqueIDWithDollars(control) + "-->");

			writer.Write("</{0}>", parentTagName);
		}

		private static string GetUniqueIDWithDollars(Control control)
		{
			string uniqueIdWithDollars = control.UniqueID;
			if(uniqueIdWithDollars == null) return null;
			if(uniqueIdWithDollars.IndexOf(':') >= 0) return uniqueIdWithDollars.Replace(':', '$');
			return uniqueIdWithDollars;
		}

		#endregion

		/// <summary>
		/// Returns a value indicating if the control is visible on the client.
		/// </summary>
		public static bool GetControlVisible(Control control, StateBag viewstate, bool designMode)
		{
			if(viewstate[RequestID.Visible] == null || (bool)viewstate[RequestID.Visible])
			{
				if(control.Parent != null && !designMode) return control.Parent.Visible;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Sets the visibility of the control on the client.
		/// </summary>
		public static void SetControlVisible(StateBag viewState, bool value)
		{
			viewState[RequestID.Visible] = value;
		}

		#region Properties

		/// <summary>
		/// Returns <strong>true</strong> if the current POST is a callback.
		/// </summary>
		public static bool IsCallBack
		{
			get
			{
				HttpContext context = HttpContext.Current;
				if(context != null)
				{
					string callback = context.Request.Params["anthem_callback"];
					if(callback != null)
					{
						// If Anthem_CallBack appears multiple times, Params will return all values joined with a comma. For example "true,true". We are only interested in the first value.
						if(callback.IndexOf(",") != -1) callback = callback.Split(',')[0];
						return string.Compare(callback, "true", true) == 0;
					}
				}
				return false;
			}
		}

		private bool IsRedirectedToLoginPage
		{
			get
			{
				// TODO: There is no handler in JS for this response - required because the auth module will escape the '&Anthem_CallBack=true'
				HttpContext context = HttpContext.Current;
				HttpRequest req = context.Request;
				string returnURL = req.QueryString["ReturnURL"];
				if(returnURL != null && returnURL.Length > 0)
				{
					returnURL = context.Server.UrlDecode(returnURL);
					if(returnURL.EndsWith("?anthem_callback=true") || returnURL.EndsWith("&anthem_callback=true"))
					{
						HttpResponse resp = context.Response;
						// WriteResult(resp, null, "LOGIN");
						// TODO: Something else
						resp.End();
						return true;
					}
				}
				return false;
			}
		}

		private static bool AreValidationScriptsRequired
		{
			get
			{
				var page = HttpContext.Current.CurrentHandler as Page;
				if(page != null && page.Validators.Count > 0 && page.Request.Browser.W3CDomVersion.Major >= 1)
				{
					foreach(IValidator validator in page.Validators)
					{
						ASP.BaseValidator bval = validator as ASP.BaseValidator;
						if(bval != null && bval.EnableClientScript)
						{
							if(page.Request.Browser.EcmaScriptVersion.CompareTo(new Version(1, 2)) >= 0)
								return true;
						}
					}
				}
				return false;
			}
		}

		#endregion
	}
}
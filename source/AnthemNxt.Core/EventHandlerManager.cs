using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASP = System.Web.UI.WebControls;
using System.Web.UI;

namespace AnthemNxt.Core
{
	public static class EventHandlerManager
	{
		#region Script Attributes / Callback Event References

		/// <summary>
		/// Adds the script to the control's attribute collection.
		/// </summary>
		/// <remarks>
		/// If the attribute already exists, the script is prepended to the existing value.
		/// </remarks>
		/// <param name="control">The control to modify.</param>
		/// <param name="attributeName">The attribute to modify.</param>
		/// <param name="script">The script to add to the attribute.</param>
		public static void AddScriptAttribute(ASP.WebControl control, string attributeName, string script)
		{
			AddScriptAttribute(control, null, attributeName, script);
		}

		/// <summary>
		/// Adds the script to the item's attribute collection.
		/// </summary>
		/// <param name="control">The control to modify.</param>
		/// <param name="item">The <see cref="System.Web.UI.WebControls.ListItem"/> to modify.</param>
		/// <param name="attributeName">The attribute to modify.</param>
		/// <param name="script">The script to add.</param>
		public static void AddScriptAttribute(ASP.WebControl control, ASP.ListItem item, string attributeName, string script)
		{
			bool enableCallBack = !(control is ICallbackControl) || ((ICallbackControl)control).EnableCallBack;

			if(enableCallBack)
			{
				string newValue = script;
				string oldValue = (item == null) ? control.Attributes[attributeName] : item.Attributes[attributeName];

				// Append the new script to the old (if one existed)
				if(oldValue != null && oldValue != newValue) newValue = oldValue.Trim().TrimEnd(';') + ";" + script;

				if(item == null)
					control.Attributes[attributeName] = newValue;
				else
					item.Attributes[attributeName] = newValue;
			}
		}

		/// <summary>
		/// Obtains a reference to a clinet-side javascript function that causes, when invoked, the client to callback to the server.
		/// </summary>
		public static string GetCallbackEventReference(ICallbackControl control, bool causesValidation, string validationGroup)
		{
			return GetCallbackEventReference(control, string.Empty, causesValidation, validationGroup, string.Empty);
		}

		/// <summary>
		/// Obtains a reference to a clinet-side javascript function that causes, when invoked, the client to callback to the server.
		/// </summary>
		public static string GetCallbackEventReference(ICallbackControl control, string argument, bool causesValidation, string validationGroup)
		{
			return GetCallbackEventReference(control, argument, causesValidation, validationGroup, string.Empty);
		}

		/// <summary>
		/// Obtains a reference to a clinet-side javascript function that causes, when invoked, the client to callback to the server.
		/// </summary>
		public static string GetCallbackEventReference(ICallbackControl control, bool causesValidation, string validationGroup, string imageDuringCallback)
		{
			return GetCallbackEventReference(control, string.Empty, causesValidation, validationGroup, imageDuringCallback);
		}

		/// <summary>
		/// Obtains a reference to a clinet-side javascript function that causes, when invoked, the client to callback to the server.
		/// </summary>
		public static string GetCallbackEventReference(ICallbackControl control, string argument, bool causesValidation, string validationGroup, string imageDuringCallback)
		{
			return string.Format(
				"javascript:Anthem_Fire(this,event,'{0}','{1}',{2},'{3}','{4}','{5}',{6},{7},{8},{9},true,true);",
				((Control)control).UniqueID,
				argument,
				causesValidation ? "true" : "false",
				validationGroup,
				imageDuringCallback,
				control.TextDuringCallBack,
				control.EnabledDuringCallBack ? "true" : "false",
				string.IsNullOrEmpty(control.PreCallBackFunction) ? "null" : control.PreCallBackFunction,
				string.IsNullOrEmpty(control.PostCallBackFunction) ? "null" : control.PostCallBackFunction,
				string.IsNullOrEmpty(control.CallBackCancelledFunction) ? "null" : control.CallBackCancelledFunction
			);
		}

		#endregion

		#region Containers + Adding Child Callbacks Automatically

		/// <summary>
		/// Add generic callbacks events to all the child controls in a container. This is used by template controls (eg. DataGrid).
		/// </summary>
		/// <param name="control">The container control.</param>
		/// <param name="enabledDuringCallBack"><strong>true</strong> if the control should be enabled on the client during a callback.</param>
		/// <param name="textDuringCallBack">The text to display during a callback.</param>
		/// <param name="preCallBackFunction">The javascript function to execute before starting the callback.</param>
		/// <param name="postCallBackFunction">The javascript function to execute after the callback response is received.</param>
		/// <param name="callBackCancelledFunction">The javascript function to execute if the callback is cancelled by the pre-callback function.</param>
		public static void AddChildCallBacks(Control control, bool enabledDuringCallBack, string textDuringCallBack, string preCallBackFunction, string postCallBackFunction, string callBackCancelledFunction)
		{
			foreach(Control child in GetAllChildControls(control))
			{
				if(child is ASP.GridView)
				{
					AddChildCallBacks(child, enabledDuringCallBack, textDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction);
				}
				else if(child is ASP.DetailsView)
				{
					AddChildCallBacks(child, enabledDuringCallBack, textDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction);
				}
				else if(child is ASP.FormView)
				{
					AddChildCallBacks(child, enabledDuringCallBack, textDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction);
				}
				else if(child is ASP.IButtonControl && child is ASP.WebControl)
				{
					if(child.Parent is ASP.DataControlFieldCell && ((ASP.DataControlFieldCell)child.Parent).ContainingField is ASP.CommandField)
					{
						AddEventHandler(
							control, (ASP.WebControl)child, "onclick", ((ASP.IButtonControl)child).CommandName, ((ASP.IButtonControl)child).CommandArgument,
							((ASP.IButtonControl)child).CausesValidation, ((ASP.IButtonControl)child).ValidationGroup,
							textDuringCallBack, enabledDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction
						);
					}
					else
					{
						AddEventHandler(
							child, (ASP.WebControl)child, "onclick", ((ASP.IButtonControl)child).CommandName, ((ASP.IButtonControl)child).CommandArgument,
							((ASP.IButtonControl)child).CausesValidation, ((ASP.IButtonControl)child).ValidationGroup,
							textDuringCallBack, enabledDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction
						);
					}
				}
				else if(child is ASP.CheckBox)
				{
					if(((ASP.CheckBox)child).AutoPostBack)
					{
						AddEventHandler(
							child, (ASP.WebControl)child, "onclick", "", "",
							((ASP.CheckBox)child).CausesValidation, ((ASP.CheckBox)child).ValidationGroup,
							textDuringCallBack, enabledDuringCallBack,
							preCallBackFunction, postCallBackFunction, callBackCancelledFunction
						);
						((ASP.CheckBox)child).AutoPostBack = false;
					}
				}
				else if(child is ASP.CheckBoxList)
				{
					if(((ASP.CheckBoxList)child).AutoPostBack)
					{
						AddScriptAttribute(
							(ASP.WebControl)child,
							"onclick",
							string.Format("AnthemListControl_OnClick(event,{0},'{1}','{2}',{3},{4},{5},{6},true,true)",
								((ASP.CheckBoxList)child).CausesValidation ? "true" : "false", ((ASP.CheckBoxList)child).ValidationGroup,
								textDuringCallBack, enabledDuringCallBack ? "true" : "false",
								string.IsNullOrEmpty(preCallBackFunction) ? "null" : preCallBackFunction,
								string.IsNullOrEmpty(postCallBackFunction) ? "null" : postCallBackFunction,
								string.IsNullOrEmpty(callBackCancelledFunction) ? "null" : callBackCancelledFunction
							)
						);
						ASP.CheckBox controlToRepeat = (ASP.CheckBox)((ASP.CheckBoxList)child).Controls[0];
						controlToRepeat.AutoPostBack = false;
					}
				}
				else if(child is ASP.DataGrid)
				{
					AddChildCallBacks(child, enabledDuringCallBack, textDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction);
				}
				else if(child is ASP.DropDownList)
				{
					if(((ASP.DropDownList)child).AutoPostBack)
					{
						AddEventHandler(
							child, (ASP.WebControl)child, "onchange", "", "",
							((ASP.DropDownList)child).CausesValidation, ((ASP.DropDownList)child).ValidationGroup,
							textDuringCallBack, enabledDuringCallBack,
							preCallBackFunction, postCallBackFunction, callBackCancelledFunction
						);
						((ASP.DropDownList)child).AutoPostBack = false;
					}
				}
				else if(child is ASP.ListBox)
				{
					if(((ASP.ListBox)child).AutoPostBack)
					{
						AddEventHandler(
							child, (ASP.WebControl)child, "onchange", "", "",
							((ASP.ListBox)child).CausesValidation, ((ASP.ListBox)child).ValidationGroup,
							textDuringCallBack, enabledDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction
						);
						((ASP.ListBox)child).AutoPostBack = false;
					}
				}
				else if(child is ASP.Panel)
				{
					AddChildCallBacks(child, enabledDuringCallBack, textDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction);
				}
				else if(child is ASP.RadioButtonList)
				{
					if(((ASP.RadioButtonList)child).AutoPostBack)
					{
						AddScriptAttribute(
							(ASP.WebControl)child,
							"onclick",
							string.Format("AnthemListControl_OnClick(event,{0},'{1}','{2}',{3},{4},{5},{6},true,true)",
								((ASP.RadioButtonList)child).CausesValidation ? "true" : "false", ((ASP.RadioButtonList)child).ValidationGroup,
								textDuringCallBack, enabledDuringCallBack ? "true" : "false",
								string.IsNullOrEmpty(preCallBackFunction) ? "null" : preCallBackFunction,
								string.IsNullOrEmpty(postCallBackFunction) ? "null" : postCallBackFunction,
								string.IsNullOrEmpty(callBackCancelledFunction) ? "null" : callBackCancelledFunction
							)
						);
						((ASP.RadioButtonList)child).AutoPostBack = false;
					}
				}
				else if(child is ASP.Repeater)
				{
					AddChildCallBacks(child, enabledDuringCallBack, textDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction);
				}
				else if(child is ASP.TextBox)
				{
					if(((ASP.TextBox)child).AutoPostBack)
					{
						AddEventHandler(
							child, (ASP.WebControl)child, "onchange", "", "",
							((ASP.TextBox)child).CausesValidation, ((ASP.TextBox)child).ValidationGroup,
							textDuringCallBack, enabledDuringCallBack, preCallBackFunction, postCallBackFunction, callBackCancelledFunction
						);
						((ASP.TextBox)child).AutoPostBack = false;
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// Add a generic callback to the target control.
		/// </summary>
		/// <remarks>The target control is most often the same as the control that is raising the event, but the GridView (for example) is the target for all of it's generated child controls.</remarks>
		private static void AddEventHandler(Control parent, ASP.WebControl control, string eventName, string commandName, string commandArgument, bool causesValidation, string validationGroup, string textDuringCallBack, bool enabledDuringCallBack, string preCallBackFunction, string postCallBackFunction, string callBackCancelledFunction)
		{
			if(!string.IsNullOrEmpty(commandName) || !string.IsNullOrEmpty(commandArgument))
			{
				parent.Page.ClientScript.RegisterForEventValidation(parent.UniqueID, string.Format("{0}${1}", commandName, commandArgument));
			}

			AddScriptAttribute(control, eventName,
				string.Format(
					"javascript:Anthem_Fire(this,event,'{0}','{1}',{2},'{3}','','{4}',{5},{6},{7},{8},true,true);return false;",
					parent.UniqueID,
					string.IsNullOrEmpty(commandName) && string.IsNullOrEmpty(commandArgument) ? "" : commandName + "$" + commandArgument,
					causesValidation ? "true" : "false", validationGroup,
					textDuringCallBack, enabledDuringCallBack ? "true" : "false",
					string.IsNullOrEmpty(preCallBackFunction) ? "null" : preCallBackFunction,
					string.IsNullOrEmpty(postCallBackFunction) ? "null" : postCallBackFunction,
					string.IsNullOrEmpty(callBackCancelledFunction) ? "null" : callBackCancelledFunction
				)
			);
		}

		private static List<Control> GetAllChildControls(Control control)
		{
			var controls = new List<Control>();
			foreach(Control child in control.Controls)
			{
				if(!(child is IUpdatableControl) && !(child is LiteralControl))
				{
					controls.Add(child);
					if(!(child is ASP.DataGrid || child is ASP.GridView || child is ASP.DetailsView || child is ASP.FormView || child is ASP.Panel || child is ASP.Repeater))
					{
						controls.AddRange(GetAllChildControls(child));
					}
				}
			}
			return controls;
		}
	}
}

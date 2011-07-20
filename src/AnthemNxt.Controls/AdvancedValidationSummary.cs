using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.ComponentModel;
using System.Linq;
using AnthemNxt.Core.Internals;
using AnthemNxt.Core;

namespace AnthemNxt.Controls
{
	public class AdvancedValidationSummary : WebControl, IUpdatableControl
	{
		public AdvancedValidationSummary()
			: base(HtmlTextWriterTag.Div)
		{
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Make Anthem aware of this control
			Manager.Register(this);
		}

		protected override void Render(HtmlTextWriter writer)
		{
			if(!DesignMode) Manager.WriteBeginControlMarker(writer, "div", this);

			if(Visible)
			{
				var errorMessages = new List<string>();

				// Populate and ALWAYS display in design mode even if disabled
				if(DesignMode)
				{
					errorMessages.AddRange(new string[] { "Example error 1", "Example error 2" });
				}
				else
				{
					if(!Enabled) return;
					errorMessages = GetErrorMessages().ToList();
				}

				if(Page != null) Page.VerifyRenderingInServerForm(this);

				if(errorMessages.Count > 0)
				{
					string str, str2, str3, str4, str5;

					switch(DisplayMode)
					{
						case ValidationSummaryDisplayMode.List:
							str = "b"; str2 = ""; str3 = ""; str4 = "b"; str5 = "";
							break;

						case ValidationSummaryDisplayMode.SingleParagraph:
							str = " "; str2 = ""; str3 = ""; str4 = " "; str5 = "b";
							break;

						case ValidationSummaryDisplayMode.BulletList:
							str = ""; str2 = "<ul>"; str3 = "<li>"; str4 = "</li>"; str5 = "</ul>";
							break;

						default: throw new NotSupportedException("Invalid display mode");
					}

					// Write the header out
					if(HeaderText.Length > 0)
					{
						writer.Write(HeaderText);
						WriteBreakIfPresent(writer, str);
					}

					writer.Write(str2);

					foreach(var msg in errorMessages)
					{
						writer.Write(str3);
						writer.Write(msg);
						WriteBreakIfPresent(writer, str4);
					}
					WriteBreakIfPresent(writer, str5);
				}
			}

			if(!DesignMode) Manager.WriteEndControlMarker(writer, "div", this);
		}

		public override void RenderControl(HtmlTextWriter writer)
		{
			Visible = true;

			base.RenderControl(writer);
		}

		private void WriteBreakIfPresent(HtmlTextWriter writer, string text)
		{
			if(text == "b")
				writer.WriteBreak();
			else
				writer.Write(text);
		}

		#region Which validators should be displayed?

		protected IEnumerable<string> GetErrorMessages()
		{
			// Select which (failed) validators should be included
			IEnumerable<BaseValidator> validators = null;
			switch(SummaryMode)
			{
				case ValidationSummaryMode.ShowAllGroups: validators = AllFailedValidators; break;
				case ValidationSummaryMode.ShowMatchingGroupOnly: validators = GroupFailedValidators; break;
				case ValidationSummaryMode.ShowOrphanedOnly: validators = OrphanFailedValidators; break;
				default: throw new NotSupportedException("Invalid summary mode");
			}

			return validators
				.Where(val => !string.IsNullOrEmpty(val.ErrorMessage))
				.Select(val => val.ErrorMessage);
		}

		protected IEnumerable<BaseValidator> AllFailedValidators
		{
			get
			{
				var failedvalidators = new List<BaseValidator>();
				foreach(IValidator val in Page.Validators)
				{
					if(!(val is BaseValidator)) continue;
					if(!val.IsValid) failedvalidators.Add((BaseValidator)val);
				}
				return failedvalidators;
			}
		}

		protected IEnumerable<BaseValidator> GroupFailedValidators
		{
			get
			{
				return AllFailedValidators.Where(val => val.ValidationGroup.ToLowerInvariant() == this.ValidationGroup.ToLowerInvariant());
			}
		}

		protected IEnumerable<BaseValidator> OrphanFailedValidators
		{
			get
			{
				// Get all summary controls that derive from the ASP.NET baseclass or this advanced one, and get a list of the groups they
				// cover (excluding this instance)
				var coveredgroups = new List<string>();
				coveredgroups.AddRange(	Page.Controls
											.FindControls(typeof(System.Web.UI.WebControls.ValidationSummary))
											.Where(sum => sum != this)
											.Cast<System.Web.UI.WebControls.ValidationSummary>()
											.Select(sum => sum.ValidationGroup.ToLowerInvariant().Trim()));
				coveredgroups.AddRange(Page.Controls
											.FindControls(typeof(AdvancedValidationSummary))
											.Where(sum => sum != this)
											.Cast<AdvancedValidationSummary>()
											.Select(sum => sum.ValidationGroup.ToLowerInvariant().Trim()));


				// Filter the list of ALL failed validators to ones that have group names that are not in this list
				var orphanvalidators = AllFailedValidators
					.Where(val => !coveredgroups.Any(group => group == val.ValidationGroup.ToLowerInvariant().Trim()));

				return orphanvalidators;
			}
		}

		#endregion

		#region Properties

		// TODO: Move to control state if this causes a problem - probably won't because these should be hard coded

		public ValidationSummaryMode SummaryMode
		{
			get 
			{
				if(ViewState["SummaryMode"] == null) return ValidationSummaryMode.ShowMatchingGroupOnly;
				return (ValidationSummaryMode)ViewState["SummaryMode"];
			}
			set { ViewState["SummaryMode"] = value; }
		}

		public ValidationSummaryDisplayMode DisplayMode
		{
			get
			{
				if(ViewState["DisplayMode"] == null) return ValidationSummaryDisplayMode.BulletList;
				return (ValidationSummaryDisplayMode)ViewState["DisplayMode"];
			}
			set { ViewState["DisplayMode"] = value; }
		}

		[DefaultValue("")]
		public string HeaderText
		{
			get { return (string)ViewState["HeaderText"] ?? ""; }
			set { ViewState["HeaderText"] = value; }
		}

		public virtual string ValidationGroup
		{
			get { return (string)ViewState["ValidationGroup"] ?? ""; }
			set { ViewState["ValidationGroup"] = value; }
		}

		[Description("True if this control should be updated after each callback."), Category("Anthem"), DefaultValue(false)]
		public virtual bool AutoUpdateAfterCallBack
		{
			get { return true; }
			set { throw new NotSupportedException("This must always be true"); }
		}

		[DefaultValue(false), Browsable(false)]
		public virtual bool UpdateAfterCallBack
		{
			get { return true; }
			set { throw new NotSupportedException("This must always be true"); }
		}

		public override bool Visible
		{
			get { return Manager.GetControlVisible(this, ViewState, base.DesignMode); }
			set { Manager.SetControlVisible(ViewState, value); }
		}

		#endregion
	}
}

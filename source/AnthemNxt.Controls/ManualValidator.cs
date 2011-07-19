using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace AnthemNxt.Controls
{
	public class ManualValidator : BaseValidator, IValidator
	{
		protected override bool EvaluateIsValid()
		{
			return string.IsNullOrEmpty(ErrorMessage);
		}

		public new string ErrorMessage
		{
			get
			{
				var o = this.ViewState["ErrorMessage"];
				if(o != null) return (string)o;
				return "";
			}
			set
			{
				this.ViewState["ErrorMessage"] = value;

				// Trigger a revalidation as this control is based on the 'ErrorMessage'
				Validate();
			}
		}

		protected override bool ControlPropertiesValid()
		{
			// Allow this control to not have a control to validate set
			return true;
		}
	}
}

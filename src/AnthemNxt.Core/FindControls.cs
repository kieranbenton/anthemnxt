using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace AnthemNxt.Core
{
	public static class FindControlsExtensions
	{
		public static HtmlForm GetForm(this Control c)
		{
			if(c is HtmlForm) return (HtmlForm)c;
			foreach(Control child in c.Controls)
			{
				HtmlForm form = child as HtmlForm;
				if(form != null && form.Visible) return form;

				if(child.HasControls())
				{
					HtmlForm htmlForm = GetForm(child);
					if(htmlForm != null) return htmlForm;
				}
			}
			return null;
		}

		public static Control FindControlRecursive(this ControlCollection controls, string id)
		{
			foreach(Control c in controls)
			{
				var t = c.FindControlRecursive(id);
				if(t != null) return t;
			}
			return null;
		}

		public static Control FindControlRecursive(this Control root, string id)
		{
			if(root.ID == id) return root;
			return root.Controls.FindControlRecursive(id);
		}

		public static List<Control> FindControls(this ControlCollection controls, Type controltype)
		{
			var list = new List<Control>();

			foreach(Control c in controls)
			{
				var t = c.FindControls(controltype);
				list.AddRange(t);
			}

			return list;
		}

		public static List<Control> FindControls(this Control root, Type controltype)
		{
			var list = new List<Control>();

			if(controltype.IsAssignableFrom(root.GetType())) list.Add(root);
			list.AddRange(root.Controls.FindControls(controltype));

			return list;
		}

        public static T FindParentControl<T>(this Control root) where T : Control
        {
            if(root is T)
                return (T)root;

            if(root.Parent == null)
                return null;

            return root.Parent.FindParentControl<T>();
        }
	}
}

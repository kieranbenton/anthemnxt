using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using AnthemNxt.Core;

namespace AnthemNxt.Tests
{
	public partial class _Default : Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void cmdTest1_Click(object sender, EventArgs e)
		{
			cmdTestA1.Text += "!";
			cmdTestA1.UpdateAfterCallBack = true;

			//Manager.WriteBeginControlMarker
		}

		protected void cmdTest2_Click(object sender, EventArgs e)
		{
			Trace.Write("Throwing exception in page");
			throw new InvalidOperationException("does this get caught");
		}

		protected void cmdTest3_Click(object sender, EventArgs e)
		{
			Response.Redirect("~/targetpage.aspx");
		}
	}
}

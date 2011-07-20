using System;
using System.Collections.Generic;
using System.Text;

namespace AnthemNxt.Core.Internals
{
	internal static class Markers
	{
		public const string ControlBegin = "<!--START:";
		public const string ControlEnd = "<!--END:";
		public const string ViewstateBegin = "<input type=\"hidden\" name=\"__VIEWSTATE\" id=\"__VIEWSTATE\" value=\"";
		public const string ViewstateEncryptedBegin = "<input type=\"hidden\" name=\"__VIEWSTATEENCRYPTED\" id=\"__VIEWSTATEENCRYPTED\" value=\"";
		public const string EventValidationBegin = "<input type=\"hidden\" name=\"__EVENTVALIDATION\" id=\"__EVENTVALIDATION\" value=\"";
	}
}

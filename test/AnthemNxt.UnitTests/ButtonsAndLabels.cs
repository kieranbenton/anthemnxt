using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using WatiN.Core;

namespace AnthemNxt.UnitTests
{
    public class ButtonsAndLabels
    {
        [Fact]
        public void ClickMe()
        {
            IE ie = new IE("http://localhost/AnthemNxt.Tests/ButtonsAndLabels.aspx");
            Assert.Null(ie.Span(Find.ById("ctl00_ContentPlaceHolder_label")).Text);
            ie.Button(Find.ByName("ctl00$ContentPlaceHolder$button")).Click();
            Assert.NotEqual("", ie.Span(Find.ById("ctl00_ContentPlaceHolder_label")).Text);
            // TODO: Check no postback occurred - how can we do this?
            ie.Close();
        }
    }
}

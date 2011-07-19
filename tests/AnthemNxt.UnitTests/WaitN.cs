using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;
using Xunit;

namespace AnthemNxt.WaitN
{
    public class WaitN
    {
        [Fact]
        public void Test()
        {
            // Open a new Internet Explorer window and
            // goto the google website.
            IE ie = new IE("http://www.google.com");

            // Find the search text field and type Watin in it.
            ie.TextField(Find.ByName("q")).TypeText("WatiN");

            // Click the Google search button.
            ie.Button(Find.ByValue("Google Search")).Click();

            // Uncomment the following line if you want to close
            // Internet Explorer and the console window immediately.
            //ie.Close();
        }
    }
}

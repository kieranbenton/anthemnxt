<%@ Page Language="C#" MasterPageFile="~/Sample.master" %>
<%@ Register TagPrefix="cc1" Namespace="AnthemNxt.Examples" Assembly="__code" %>

<%@ Import Namespace="System.Data" %>
<asp:Content ID="Content2" runat="server" ContentPlaceHolderID="ContentPlaceHolder">
    <h1>
        Invoke Custom Control</h1>
    <p>
        This is an example of a simple custom control that has a Click event. The source
        code for the control is in App_Code/CustomControl2.cs.</p>
    <cc1:CustomControl2 ID="CustomControl2" runat="server" Text="Click Me!" />
</asp:Content>

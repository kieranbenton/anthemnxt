using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ASP = System.Web.UI.WebControls;
using AnthemNxt.Core;

namespace AnthemNxt.Controls
{
	[ToolboxData("<{0}:ImageMap runat=server></{0}:AnthemImageMap>")]
	public class ImageMap : ASP.ImageMap, IPostBackEventHandler, ICallbackControl, IUpdatableControl
	{
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Register the control with Anthem
			AnthemNxt.Core.Manager.Register(this);
		}

		/// <summary>
		/// Catch the callback (ASP.NET thinks this is a PostBack) and redirect to the ImageMap.OnClick handler. There is only the one event raisable so no
		/// need to check the event argument.
		/// </summary>
		/// <param name="eventargument"></param>
		void IPostBackEventHandler.RaisePostBackEvent(string eventargument)
		{
			this.OnClick(new ImageMapEventArgs(eventargument));
		}

		/// <summary>
		/// Overrides the pretty useless ASP.NET 2.0 rendering of an ImageMap and replaces with a Anthem.NET capable (replaces the POST type maps) control that
		/// is also fully XHTML compatible.
		/// </summary>
		/// <param name="writer"></param>
		protected override void Render(HtmlTextWriter writer)
		{
			// Anthem preamble
			if(!DesignMode) AnthemNxt.Core.Manager.WriteBeginControlMarker(writer, "span", this);

			if(Visible)
			{
				// Manually render the underlying image due to not being able to skip the base.Render imagemap implementation
				RenderUnderlyingImage(writer);

				// Only output if there is at least one hotspot
				if(Enabled && !base.IsEnabled) writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
				if(HotSpots.Count > 0)
				{
					// Imagemap preamble, creating a new "sub" ID for the map which is referred to by the image
					string id = "ImageMap" + this.ClientID;
					writer.AddAttribute(HtmlTextWriterAttribute.Id, id);
					writer.AddAttribute(HtmlTextWriterAttribute.Name, id);
					writer.RenderBeginTag(HtmlTextWriterTag.Map);

					int i = 0;
					foreach(HotSpot spot in HotSpots)
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Shape, GetHotSpotMarkupName(spot), false);
						writer.AddAttribute(HtmlTextWriterAttribute.Coords, spot.GetCoordinates());

						// What kind of hotspot is this? POST of course is now routed through Anthem.
						switch(spot.HotSpotMode)
						{
							case HotSpotMode.NotSet: throw new NotSupportedException("HotSpotMode.NotSet not supported");

							case HotSpotMode.PostBack:
								if(Page != null) Page.VerifyRenderingInServerForm(this);

								// Registers the click to fire back via AJAX
								writer.AddAttribute(HtmlTextWriterAttribute.Onclick, 
									EventHandlerManager.GetCallbackEventReference(
										this,				// Reference to this ICallbackControl
										spot.PostBackValue, // The event argument
										false,				// Causes validation
										string.Empty,		// Validation group
										string.Empty		// Image during callback
									));

								// Added otherwise most browsers won't render the map with the correct mouse cursor
								writer.AddAttribute(HtmlTextWriterAttribute.Href, "javascript:void(0);");
								break;

							case HotSpotMode.Navigate:
								writer.AddAttribute(HtmlTextWriterAttribute.Href, ResolveClientUrl(spot.NavigateUrl));
								if(spot.Target.Length > 0) writer.AddAttribute(HtmlTextWriterAttribute.Target, spot.Target);
								break;

							case HotSpotMode.Inactive:
								writer.AddAttribute("nohref", "true");
								break;
						}

						// Add general hotspot attributes
						writer.AddAttribute(HtmlTextWriterAttribute.Title, spot.AlternateText);
						writer.AddAttribute(HtmlTextWriterAttribute.Alt, spot.AlternateText);
						if(AccessKey.Length > 0) writer.AddAttribute(HtmlTextWriterAttribute.Accesskey, AccessKey);
						if(TabIndex != 0) writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, spot.TabIndex.ToString());

						// Hotspot postamble
						writer.RenderBeginTag(HtmlTextWriterTag.Area);
						writer.RenderEndTag();
						i++;
					}

					// Imagemap postamble
					writer.RenderEndTag();
				}
			}

			// Anthem postamble
			if(!DesignMode) AnthemNxt.Core.Manager.WriteEndControlMarker(writer, "span", this);
		}

		protected void RenderUnderlyingImage(HtmlTextWriter writer)
		{
			// Name the ID of the image after what was originally requested and tie it to the "sub" ID of the map
			writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
			writer.AddAttribute(HtmlTextWriterAttribute.Usemap, "#ImageMap" + this.ClientID);

			// Write out image URL, description and alternative text (fixes XHTML rendered from ASP.NET 2.0)
			if(ImageUrl.Length > 0) writer.AddAttribute(HtmlTextWriterAttribute.Src, ResolveClientUrl(ImageUrl));
			if(DescriptionUrl.Length > 0) writer.AddAttribute(HtmlTextWriterAttribute.Longdesc, ResolveClientUrl(DescriptionUrl));
			GenerateEmptyAlternateText = true;
			if(AlternateText.Length > 0 || GenerateEmptyAlternateText) writer.AddAttribute(HtmlTextWriterAttribute.Alt, AlternateText);

			// Renders the alignment
			string alignment = "";
			switch(ImageAlign)
			{
				case ImageAlign.Left: alignment = "left"; break;
				case ImageAlign.Right: alignment = "right"; break;
				case ImageAlign.Baseline: alignment = "baseline"; break;
				case ImageAlign.Top: alignment = "top"; break;
				case ImageAlign.Middle: alignment = "middle"; break;
				case ImageAlign.Bottom: alignment = "bottom"; break;
				case ImageAlign.AbsBottom: alignment = "absbottom"; break;
				case ImageAlign.AbsMiddle: alignment = "absmiddle"; break;
				case ImageAlign.NotSet: alignment = ""; break;
				default: alignment = "texttop"; break;
			}
			if(alignment != "") writer.AddAttribute(HtmlTextWriterAttribute.Align, alignment);

			// Eliminates the usual (and annoying) image border
			if(BorderWidth.IsEmpty) writer.AddStyleAttribute(HtmlTextWriterStyle.BorderWidth, "0px");

			// Image pre & postamble
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();
		}

		private static string GetHotSpotMarkupName(HotSpot spot)
		{
			if(spot is PolygonHotSpot)
				return "poly";
			else if(spot is RectangleHotSpot)
				return "rect";
			else if(spot is CircleHotSpot)
				return "circle";
			else
				throw new NotSupportedException("Other hot spot types not supported");
				
		}

		#region ICallBackControl Members

		[DefaultValue("")]
		public virtual string CallBackCancelledFunction
		{
			get
			{
				if(null == ViewState["CallBackCancelledFunction"])
					return string.Empty;
				else
					return (string)ViewState["CallBackCancelledFunction"];
			}
			set { ViewState["CallBackCancelledFunction"] = value; }
		}

		[DefaultValue(true)]
		public virtual bool EnableCallBack
		{
			get
			{
				if(ViewState["EnableCallBack"] == null)
					return true;
				else
					return (bool)ViewState["EnableCallBack"];
			}
			set
			{
				ViewState["EnableCallBack"] = value;
			}
		}

		[DefaultValue(true)]
		public virtual bool EnabledDuringCallBack
		{
			get
			{
				if(null == ViewState["EnabledDuringCallBack"])
					return true;
				else
					return (bool)ViewState["EnabledDuringCallBack"];
			}
			set { ViewState["EnabledDuringCallBack"] = value; }
		}

		[DefaultValue("")]
		public virtual string PostCallBackFunction
		{
			get
			{
				if(null == ViewState["PostCallBackFunction"])
					return string.Empty;
				else
					return (string)ViewState["PostCallBackFunction"];
			}
			set { ViewState["PostCallBackFunction"] = value; }
		}

		[DefaultValue("")]
		public virtual string PreCallBackFunction
		{
			get
			{
				if(null == ViewState["PreCallBackFunction"])
					return string.Empty;
				else
					return (string)ViewState["PreCallBackFunction"];
			}
			set { ViewState["PreCallBackFunction"] = value; }
		}

		[DefaultValue("")]
		public virtual string TextDuringCallBack
		{
			get
			{
				if(null == ViewState["TextDuringCallBack"])
					return string.Empty;
				else
					return (string)ViewState["TextDuringCallBack"];
			}
			set { ViewState["TextDuringCallBack"] = value; }
		}

		#endregion

		#region IUpdatableControl Members

		private bool _updateAfterCallBack = false;
		private static readonly object EventPreUpdateKey = new object();

		[DefaultValue(false)]
		public virtual bool AutoUpdateAfterCallBack
		{
			get
			{
				if(ViewState["AutoUpdateAfterCallBack"] == null)
					return false;
				else
					return (bool)ViewState["AutoUpdateAfterCallBack"];
			}
			set
			{
				if(value) UpdateAfterCallBack = true;
				ViewState["AutoUpdateAfterCallBack"] = value;
			}
		}

		[Browsable(false)]
		public virtual bool UpdateAfterCallBack
		{
			get { return _updateAfterCallBack; }
			set { _updateAfterCallBack = value; }
		}

		[Category("Misc"), Description("Fires before the control is rendered with updated values.")]
		public event EventHandler PreUpdate
		{
			add { Events.AddHandler(EventPreUpdateKey, value); }
			remove { Events.RemoveHandler(EventPreUpdateKey, value); }
		}

		public virtual void OnPreUpdate()
		{
			EventHandler EditHandler = (EventHandler)Events[EventPreUpdateKey];
			if(EditHandler != null)
				EditHandler(this, EventArgs.Empty);
		}

		#endregion
	}
}

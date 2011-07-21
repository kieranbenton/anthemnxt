// Anthem.js

var anthemnxt = {};

function Anthem_Encode(s){
	if (typeof encodeURIComponent == "function") {
		// Use JavaScript built-in function
		// IE 5.5+ and Netscape 6+ and Mozilla
		return encodeURIComponent(s);
	} else {
		// Need to mimic the JavaScript version
		// Netscape 4 and IE 4 and IE 5.0
		return encodeURIComponentNew(s);
	}
}

// Primarily used by AnthemNxt.Manager to add an onsubmit event handler
// when validators are added to a page during a callback.
function Anthem_AddEvent(control, eventType, functionPrefix) {
    var ev;
    eval("ev = control." + eventType + ";");
    if (typeof(ev) == "function") {
        ev = ev.toString();
        ev = ev.substring(ev.indexOf("{") + 1, ev.lastIndexOf("}"));
    }
    else {
        ev = "";
    }
    var func;
    if (navigator.appName.toLowerCase().indexOf('explorer') > -1) {
        func = new Function(functionPrefix + " " + ev);
    }
    else {
        func = new Function("event", functionPrefix + " " + ev);
    }
    eval("control." + eventType + " = func;");
}

// Returns the form that is posted back using AJAX
function Anthem_GetForm() {
    return $('#' + Anthem_FormID)[0];
}

// Returns the URL for callbacks
function Anthem_GetCallBackUrl() {
    var form = Anthem_GetForm();
    var action = form.action + (form.action.indexOf('?') == -1 ? "?" : "&") + "anthem_callback=true";
    return action;
}

function Anthem_Fire(control, e, eventTarget, eventArgument, 
    causesValidation, validationGroup, imageUrlDuringCallBack, textDuringCallBack, enabledDuringCallBack,
	preCallBackFunction, postCallBackFunction, callBackCancelledFunction, includeControls, updatePage
) {
    // Cancel the callback if the control is disabled. Although most controls will not raise their callback event if they are disabled, the LinkButton will
    if (control.disabled) return;

    var preProcessOut = new Anthem_PreProcessCallBackOut();
    var preProcessResult = Anthem_PreProcessCallBack(control,
	    e, eventTarget,
	    causesValidation, validationGroup,
	    imageUrlDuringCallBack, textDuringCallBack, enabledDuringCallBack,
	    preCallBackFunction, callBackCancelledFunction,
	    preProcessOut
	);

    if (preProcessResult) {
        var eventType = e.type;

        Anthem_FireEvent(
            control, eventTarget, eventArgument,
		    function (result) {
		        Anthem_PostProcessCallBack(
                    result, control,
                    eventType, eventTarget,
                    null, null,
                    imageUrlDuringCallBack, textDuringCallBack, postCallBackFunction,
                    preProcessOut
                );
		    },
		    null,
		    includeControls,
		    updatePage
	    );
    }
}

function Anthem_FireEvent(source, eventTarget, eventArgument, clientCallBack, clientCallBackArg, includeControls, updatePage) {
    var form = Anthem_GetForm();
    Anthem_SetHiddenInputValue(form, "__EVENTTARGET", eventTarget);
    Anthem_SetHiddenInputValue(form, "__EVENTARGUMENT", eventArgument);
    Anthem_CallBack(source, null, null, null, null, clientCallBack, clientCallBackArg, includeControls, updatePage);
}

function Anthem_CallBack(source, target, id, method, args, clientCallBack, clientCallBackArg, includeControls, updatePage) {
    // Preserve __EVENTTARGET & __EVENTARGUMENT so that callbacks can be made from inside 'precallback'
    var etarget = $('#__EVENTTARGET').val(); var eargs = $('#__EVENTARGUMENT').val();
    $(anthemnxt).trigger('precallback', [source]);
    $('#__EVENTTARGET').val(etarget); $('#__EVENTARGUMENT').val(eargs);

    // Add "call" methods if they are provided
    var data = "";
    if (target == "Page") {
        data += "&anthem_pagemethod=" + method;
    } else if (target == "MasterPage") {
        data += "&anthem_masterpagemethod=" + method;
    } else if (target == "Control") {
        data += "&anthem_controlid=" + id.split(":").join("_"); data += "&anthem_controlmethod=" + method;
    }

    // Add "call" arguments if there are any
	if (args) {
		for (var argsIndex = 0; argsIndex < args.length; ++argsIndex) {
			if (args[argsIndex] instanceof Array) {
				for (var i = 0; i < args[argsIndex].length; ++i) {
					data += "&anthem_callbackargument" + argsIndex + "=" + Anthem_Encode(args[argsIndex][i]);
				}
			} else {
				data += "&anthem_callbackargument" + argsIndex + "=" + Anthem_Encode(args[argsIndex]);
			}
		}
	}
	
    // Add parameter to indicate the server side code needs to provide a page fragment update
	if (updatePage) data += "&anthem_updatepage=true";
	
	// Build 'data' containing all the control's values
	if (includeControls) {
		var form = Anthem_GetForm();
		if (form != null) {
			for (var elementIndex = 0; elementIndex < form.length; ++elementIndex) {
				var element = form.elements[elementIndex];
				if (element.name) {
					var elementValue = null;
					if (element.nodeName.toUpperCase() == "INPUT") {
						var inputType = element.getAttribute("type").toUpperCase();
						if (inputType == "TEXT" || inputType == "PASSWORD" || inputType == "HIDDEN") {
							elementValue = element.value;
						} else if (inputType == "CHECKBOX" || inputType == "RADIO") {
							if (element.checked) {
								elementValue = element.value;
							}
						} else if (inputType == "FILE") {
						}
					} else if (element.nodeName.toUpperCase() == "SELECT") {
						if (element.multiple) {
							elementValue = [];
							for (var i = 0; i < element.length; ++i) {
								if (element.options[i].selected) {
									elementValue.push(element.options[i].value);
								}
							}
						} else if (element.length == 0) {
						    elementValue = null;
						} else {
							elementValue = element.value;
						}
					} else if (element.nodeName.toUpperCase() == "TEXTAREA") {
						elementValue = element.value;
					}
					if (elementValue instanceof Array) {
						for (var i = 0; i < elementValue.length; ++i) {
							data += "&" + element.name + "=" + Anthem_Encode(elementValue[i]);
						}
					} else if (elementValue != null) {
						data += "&" + element.name + "=" + Anthem_Encode(elementValue);
					}
				}
			}
		}
	}
	if (data.length > 0) data = data.substring(1);

    var result = $.ajax({ 
        async: clientCallBack ? true : false,
        cache: false,
        type: 'POST',
        url: Anthem_GetCallBackUrl(),
        data: data,
        complete: function (x) {
            result = Anthem_GetResult(x);
            if (result.error) Anthem_Error(result);
            if (updatePage) Anthem_UpdatePage(result);
            Anthem_EvalClientSideScript(result);
            clientCallBack(result, clientCallBackArg);
            $(anthemnxt).trigger('postcallback', [source]);
        }
    });

    if (!clientCallback) {
        result = Anthem_GetResult(x);
        if (result.error) Anthem_Error(result);
        if (updatePage) Anthem_UpdatePage(result);
        Anthem_EvalClientSideScript(result);
        $(anthemnxt).trigger('postcallback', [source]);
    }

	return result;
}

function Anthem_GetResult(x) {
	var result = { "value": null, "error": null };
	var responseText = x.responseText;
	try {
		result = eval("(" + responseText + ")");
	} catch (e) {
		if (responseText.length == 0) {
			result.error = "NORESPONSE";
		} else {
			result.error = "BADRESPONSE";
			result.responseText = responseText;
		}
	}
	return result;
}

function Anthem_SetHiddenInputValue(form, name, value) {
    var input = null;
    if (form[name]) {
        input = form[name];
    } else {
        input = document.createElement("input");
        input.setAttribute("name", name);
        input.setAttribute("type", "hidden");
    }
    input.setAttribute("value", value);
    var parentElement = input.parentElement ? input.parentElement : input.parentNode;
    if (parentElement == null) {
        form.appendChild(input);
        form[name] = input;
    }
}

function Anthem_RemoveHiddenInput(form, name) {
    var input = form[name];
    if (input != null && typeof(input) != "undefined") {
      var parentElement = input.parentElement ? input.parentElement : input.parentNode;
      if (parentElement != null) {
          form[name] = null;
          parentElement.removeChild(input);
      }
    }
}

function Anthem_UpdatePage(result) {
	var form = Anthem_GetForm();
	if (result.viewState) Anthem_SetHiddenInputValue(form, "__VIEWSTATE", result.viewState);
	if (result.viewStateEncrypted) Anthem_SetHiddenInputValue(form, "__VIEWSTATEENCRYPTED", result.viewStateEncrypted);
	if (result.eventValidation) Anthem_SetHiddenInputValue(form, "__EVENTVALIDATION", result.eventValidation);
	if (result.controls) {
		for (var controlID in result.controls) {
			var containerID = "Anthem_" + controlID.split("$").join("_") + "__";
			var control = document.getElementById(containerID);
			if (control) {
				control.innerHTML = result.controls[controlID];
				if (result.controls[controlID] == "") {
					control.style.display = "none";
				} else {
					control.style.display = "";
				}
			}
		}
	}
	if (result.pagescript) Anthem_LoadPageScript(result, 0);
}

// Load each script in order and wait for each one to load before proceeding
function Anthem_LoadPageScript(result, index) {
    if (index < result.pagescript.length) {
		try {
		    var isExternalScript = false;
		    var script = document.createElement('script');
		    script.type = 'text/javascript';
		    if (result.pagescript[index].indexOf('src=') == 0) {
		        isExternalScript = true;
		        script.src = result.pagescript[index].substring(4);
		    } else {
		        if (script.canHaveChildren ) {
		            script.appendChild(document.createTextNode(result.pagescript[index]));
		        } else {
		            script.text = result.pagescript[index];
		        }
		    }
		    var heads = document.getElementsByTagName('head');
		    if (heads != null && typeof(heads) != "undefined" && heads.length > 0) {
		        var head = heads[0];

		        // The order that scripts appear is important since later scripts can
		        // redefine a function. Therefore it is important to add every script
		        // to the page and in the same order that they were added on the server.
		        // On the other hand, if we just keep adding scripts the DOM will grow
		        // unnecessarily. This code scans the <head> element block and removes 
		        // previous instances of the identical script.
		        var found = false;
		        for (var child = 0; child < head.childNodes.length; child++) {
		            var control = head.childNodes[child];
		            if (typeof(control.tagName) == "string") {
		                if (control.tagName.toUpperCase() == "SCRIPT") {
		                    if (script.src.length > 0) {
		                        if (script.src == control.src) {
		                            found = true;
		                            break;
		                        }
		                    } else if (script.innerHTML.length > 0) {
		                        if (script.innerHTML == control.innerHTML) {
		                            found = true;
		                            break;
		                        }
		                    }
		                }
		            }
		        }
		        if (found) head.removeChild(control);
                
                var scriptAddedToHead = false;
                if (typeof script.readyState != "undefined" && !window.opera) {
                	script.onreadystatechange = function() {
                		if(script.readyState != "complete" && script.readyState != "loaded") {
                			return;
                		} else {
                			Anthem_LoadPageScript(result, index + 1);
                		}
                	};
                } else {
                    if (isExternalScript) // if it's an external script, only execute the next script when the previous one is loaded.
                    {
                    	script.onload = function() { Anthem_LoadPageScript(result, index + 1); };
                    }
                    else // I didn't find a way for script blocks to fire some onload event. So in this case directly call the Anthem_LoadPageScript for the next script.
                    {
                        document.getElementsByTagName('head')[0].appendChild(script);
                        scriptAddedToHead = true;
                        Anthem_LoadPageScript(result, index + 1);
                    }
                }
                
                // Now we append the new script and move on to the next script.
		        // Note that this is a recursive function. It stops when the
		        // index grows larger than the number of scripts.
		        if (!scriptAddedToHead) document.getElementsByTagName('head')[0].appendChild(script);
	        }
		} catch (e) {
		    throw e;
		}
	}
} 

function Anthem_EvalClientSideScript(result) {
	if (result.script) {
		for (var i = 0; i < result.script.length; ++i) {
			try {
				eval(result.script[i]);
			} catch (e) {
				alert("Error evaluating client-side script!\n\nScript: " + result.script[i] + "\n\nException: " + e);
			}
		}
	}
}

function Anthem_Clear__EVENTTARGET() {
    var form = Anthem_GetForm(); // see http://sourceforge.net/tracker/index.php?func=detail&aid=1429412&group_id=151897&atid=782464
	Anthem_SetHiddenInputValue(form, "__EVENTTARGET", "");
}

function Anthem_InvokePageMethod(methodName, args, clientCallBack, clientCallBackArg) {
	Anthem_Clear__EVENTTARGET();
    return Anthem_CallBack(methodName, "Page", null, methodName, args, clientCallBack, clientCallBackArg, true, true);
}

function Anthem_InvokeMasterPageMethod(methodName, args, clientCallBack, clientCallBackArg) {
	Anthem_Clear__EVENTTARGET();
	return Anthem_CallBack(methodName, "MasterPage", null, methodName, args, clientCallBack, clientCallBackArg, true, true);
}

function Anthem_InvokeControlMethod(id, methodName, args, clientCallBack, clientCallBackArg) {
	Anthem_Clear__EVENTTARGET();
	return Anthem_CallBack(methodName, "Control", id, methodName, args, clientCallBack, clientCallBackArg, true, true);
}

function Anthem_PreProcessCallBackOut() {
    this.ParentElement = null;
    this.OriginalText = '';
}

function Anthem_PreProcessCallBack(
    control,
    e,
    eventTarget,
    causesValidation, 
    validationGroup, 
    imageUrlDuringCallBack, 
    textDuringCallBack, 
    enabledDuringCallBack,
    preCallBackFunction,
    callBackCancelledFunction,
    preProcessOut
) {
	var valid = true;
	if (causesValidation && typeof(Page_ClientValidate) == "function") {
		valid = Page_ClientValidate(validationGroup);
	}
	if (typeof(WebForm_OnSubmit) == "function") {
	    valid = WebForm_OnSubmit();
	}
	if (valid) {
        var preCallBackResult = true;
        if (typeof(preCallBackFunction) == "function") {
	        preCallBackResult = preCallBackFunction(control, e);
        }
        if (typeof(preCallBackResult) == "undefined" || preCallBackResult) {
		    var inputType = control.getAttribute("type");
		    inputType = (inputType == null) ? '' : inputType.toUpperCase();
		    if (inputType == "IMAGE" && e != null) {
                var form = Anthem_GetForm();
                if (e.offsetX) { // IE
                    Anthem_SetHiddenInputValue(form, eventTarget + ".x", e.offsetX);
                    Anthem_SetHiddenInputValue(form, eventTarget + ".y", e.offsetY);
                } else { // FireFox + ???
                    var offset = GetControlLocation(control);
                    Anthem_SetHiddenInputValue(form, eventTarget + ".x", e.clientX - offset.x + 1 + window.pageXOffset);
                    Anthem_SetHiddenInputValue(form, eventTarget + ".y", e.clientY - offset.y + 1 + window.pageYOffset);
                }
		    }
		    if (imageUrlDuringCallBack || textDuringCallBack) {
		        var nodeName = control.nodeName.toUpperCase();
		        if (nodeName == "INPUT") {
		            if (inputType == "CHECKBOX" || inputType == "RADIO" || inputType == "TEXT") {
		                preProcessOut.OriginalText = GetLabelText(control.id);
		                SetLabelText(control.id, textDuringCallBack);
		            } else if (inputType == "IMAGE") {
		                if (imageUrlDuringCallBack) {
		                    preProcessOut.OriginalText = control.src;
		                    control.src = imageUrlDuringCallBack;
		                } else {
		                    preProcessOut.ParentElement = control.parentElement ? control.parentElement : control.parentNode;
		                    if (preProcessOut.ParentElement) {
		                        preProcessOut.OriginalText = preProcessOut.ParentElement.innerHTML;
		                        preProcessOut.ParentElement.innerHTML = textDuringCallBack;
		                    }
		                }
		            } else if (inputType == "SUBMIT" || inputType == "BUTTON") {
		                preProcessOut.OriginalText = control.value;
		                control.value = textDuringCallBack;
		            }
		        } else if (nodeName == "SELECT" || nodeName == "SPAN") {
		            preProcessOut.OriginalText = GetLabelText(control.id);
		            SetLabelText(control.id, textDuringCallBack);
		        } else {
		            preProcessOut.OriginalText = control.innerHTML;
			        control.innerHTML = textDuringCallBack;
			    }
		    }
		    // Disable the control during callback if required
		    control.disabled = (typeof(enabledDuringCallBack) == "undefined") ? false : !enabledDuringCallBack;
		    return true;
		} else {
		    // Callback cancelled
            if (typeof(callBackCancelledFunction) == "function") {
	            callBackCancelledFunction(control, e);
	        }
	        return false;
		}
    } else {
        // Validation failed
        return false;
    }
}

function Anthem_PostProcessCallBack(
    result, 
    control,
    e,
    eventTarget, 
    clientCallBack, 
    clientCallBackArg, 
    imageUrlDuringCallBack, 
    textDuringCallBack, 
    postCallBackFunction, 
    preProcessOut
) {
    if (typeof(postCallBackFunction) == "function") {
        postCallBackFunction(control, e);
    }
    // Re-enable the control if it was disabled during callback
	control.disabled = false;
    var inputType = control.getAttribute("type");
    inputType = (inputType == null) ? '' : inputType.toUpperCase();
	if (inputType == "IMAGE") {
	    var form = Anthem_GetForm();
        Anthem_RemoveHiddenInput(form, eventTarget + ".x");
        Anthem_RemoveHiddenInput(form, eventTarget + ".y");
	}
	if (imageUrlDuringCallBack || textDuringCallBack) {
	    var nodeName = control.nodeName.toUpperCase();
	    if (nodeName == "INPUT") {
	        if (inputType == "CHECKBOX" || inputType == "RADIO" || inputType == "TEXT") {
	            SetLabelText(control.id, preProcessOut.OriginalText);
	        } else if (inputType == "IMAGE") {
	            if (imageUrlDuringCallBack) {
	                control.src = preProcessOut.OriginalText;
	            } else {
	                preProcessOut.ParentElement.innerHTML = preProcessOut.OriginalText;
	            }
	        } else if (inputType == "SUBMIT" || inputType == "BUTTON") {
	            control.value = preProcessOut.OriginalText;
	        }
	    } else if (nodeName == "SELECT" || nodeName == "SPAN") {
	        SetLabelText(control.id, preProcessOut.OriginalText);
	    } else {
	        control.innerHTML = preProcessOut.OriginalText;
	    }
	}
	if (typeof(clientCallBack) == "function") {
	    clientCallBack(result, clientCallBackArg);
	}
}

function AnthemListControl_OnClick(
    e,
	causesValidation,
	validationGroup,
	textDuringCallBack,
	enabledDuringCallBack,
	preCallBackFunction,
	postCallBackFunction,
	callBackCancelledFunction,
	includeControlValuesWithCallBack,
	updatePageAfterCallBack
) {
	var target = e.target || e.srcElement;
	if (target.nodeName.toUpperCase() == "LABEL" && target.htmlFor != '')
	    return;
	var eventTarget = target.id.split("_").join("$");
	Anthem_Fire(
	    target, 
	    e,
	    eventTarget, 
	    '', 
	    causesValidation, 
	    validationGroup, 
	    '',
	    textDuringCallBack, 
	    enabledDuringCallBack, 
	    preCallBackFunction, 
	    postCallBackFunction, 
	    callBackCancelledFunction, 
	    true, 
	    true
	);
}

// Returns the top, left control location in FireFox
function GetControlLocation(control) {
    var offsetX = 0;
    var offsetY = 0;
    var parent;
    
    for (parent = control; parent; parent = parent.offsetParent) {
        if (parent.offsetLeft) offsetX += parent.offsetLeft;
        if (parent.offsetTop) offsetY += parent.offsetTop;
    }
    
    return { x: offsetX, y: offsetY };
}

function GetLabelText(id) {
    var labels = document.getElementsByTagName('label');
    for (var i = 0; i < labels.length; i++) {
        if (labels[i].htmlFor == id) {
            return labels[i].innerHTML;
        }
    }
    return null;
}

function SetLabelText(id, text) {
    var labels = document.getElementsByTagName('label');
    for (var i = 0; i < labels.length; i++) {
        if (labels[i].htmlFor == id) {
            labels[i].innerHTML = text;
            return;
        }
    }
}

// Used by encodeURIComponentNew to mimic function encodeURIComponent in 
// IE 5.5+, Netscape 6+, and Mozilla
function utf8(wide) {
  var c, s;
  var enc = "";
  var i = 0;
  while(i<wide.length) {
    c= wide.charCodeAt(i++);
    // handle UTF-16 surrogates
    if (c>=0xDC00 && c<0xE000) continue;
    if (c>=0xD800 && c<0xDC00) {
      if (i>=wide.length) continue;
      s= wide.charCodeAt(i++);
      if (s<0xDC00 || c>=0xDE00) continue;
      c= ((c-0xD800)<<10)+(s-0xDC00)+0x10000;
    }
    // output value
    if (c<0x80) enc += String.fromCharCode(c);
    else if (c<0x800) enc += String.fromCharCode(0xC0+(c>>6),0x80+(c&0x3F));
    else if (c<0x10000) enc += String.fromCharCode(0xE0+(c>>12),0x80+(c>>6&0x3F),0x80+(c&0x3F));
    else enc += String.fromCharCode(0xF0+(c>>18),0x80+(c>>12&0x3F),0x80+(c>>6&0x3F),0x80+(c&0x3F));
  }
  return enc;
}

var hexchars = "0123456789ABCDEF";

function toHex(n) {
  return hexchars.charAt(n>>4)+hexchars.charAt(n & 0xF);
}

var okURIchars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

// Mimics function encodeURIComponent in IE 5.5+, Netscape 6+, and Mozilla
function encodeURIComponentNew(s) {
  var s = utf8(s);
  var c;
  var enc = "";
  for (var i= 0; i<s.length; i++) {
    if (okURIchars.indexOf(s.charAt(i))==-1)
      enc += "%"+toHex(s.charCodeAt(i));
    else
      enc += s.charAt(i);
  }
  return enc;
}

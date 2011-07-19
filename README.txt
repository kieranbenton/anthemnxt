ANTHEMNXT RELEASE NOTES
=======================

** 1.5.2 ** 

Last release that is fully compatible with Anthem.NET. From here on in we will be not necessarily providing 100% compatibility with Anthem aware controls in the hope of modernising the JS used (as well as adding JQuery as a dependency). If you just want a version of Anthem.NET with some of the bugs fixed then use this download

** 1.6.0 **

- Modularised server side code into AnthemNxt.Core (containing the global handlers/managers and the JS) and AnthemNxt.Controls containing just the subclassed AJAX version of the standard controls.
- Added in Anthem.NET test suite as AnthemNxt.Tests.
- Broken out Anthem.Manager into CallbackFilter, EventHandlerManager, Manager, ScriptManager and associated interfaces.
- Changed namsepaces to follow new project name and new modular nature.
- Changed default value of Panel.AddCallBacks to FALSE.
- Signed the assemblies for use in trusted environments.
- Removed clearing of __EVENTTARGET & __EVENTARGUMENT after a callback - prevents being able to do another callback inside of one of the pre/post handlers.
- Converted anthem_precallback and anthem_postcallback to be JQuery "events" on $(document). Allows >1 subscriber, reduces code complexity. Also adding 'target' as a parameter to anthem_precallback so that we can which object caused the callback. We lose the ability to cancel the callback from those global events.
var SystemThreadingTimerLib = {
    $vars: {
        currentCallbackId : 0,
		callback: {}
    },

	SetCallback: function (onCallback)
	{
		vars.callback = onCallback;
	},
	
	UpdateTimer: function(interval)
	{
		var id = ++vars.currentCallbackId;
		setTimeout(function()
		{
			if (id === vars.currentCallbackId)
				{{{ makeDynCall("v", "vars.callback") }}}();
		},
		interval);
	}
};

autoAddDeps(SystemThreadingTimerLib, '$vars');
mergeInto(LibraryManager.library, SystemThreadingTimerLib);
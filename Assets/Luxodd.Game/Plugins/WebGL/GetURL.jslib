mergeInto(LibraryManager.library, {
 
    GetURLFromQueryStr: function () {
        // Luxodd loads the WebGL document from a blob: URL. The blob URL has
        // no token query string, so read the real launch URL from the parent
        // page (or referrer when cross-origin access is unavailable).
        var launchUrl = window.location.href;
        try {
            if (window.parent && window.parent !== window && window.parent.location.href) {
                launchUrl = window.parent.location.href;
            }
        } catch (error) {
            if (document.referrer) {
                launchUrl = document.referrer;
            }
        }

        var bufferSize = lengthBytesUTF8(launchUrl) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(launchUrl, buffer, bufferSize);
        return buffer;
    },
	
	ReceiveJWTFromParent: function () {
        window.addEventListener('message', function (event) {
            //
			console.log("receive message event: " + event.data);
			
            var jwt = event.data.jwt; // get token
            if (jwt) {
                var bufferSize = lengthBytesUTF8(jwt) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(jwt, buffer, bufferSize);
                Module.ccall('OnReceiveJWT', null, ['number'], [buffer]); // call unity method
            }
        });
    },
	
	NotifyParentGameReady: function () {
		console.log("Send gameReady message");
        window.parent.postMessage({ type: 'gameReady' }, '*');
    }
});

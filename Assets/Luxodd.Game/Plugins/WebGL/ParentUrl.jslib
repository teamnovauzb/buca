mergeInto(LibraryManager.library, {
    GetParentHost: function () {
        var host = "";

        try {
            if (window.parent && window.parent !== window) {
                host = window.parent.location.host;
            }
        } catch (e) {
            if (document.referrer) {
                var ref = new URL(document.referrer);
                host = ref.host;
            }
        }

        if (!host && window.location.origin && window.location.origin !== "null") {
            host = new URL(window.location.origin).host;
        }

        if (!host && window.location.protocol === "blob:") {
            try {
                host = new URL(window.location.href.substring(5)).host;
            } catch (e) {}
        }

        // --- НОВЫЙ способ вернуть строку в Unity ---
        var length = lengthBytesUTF8(host) + 1;   // +1 для терминального 0
        var buffer = _malloc(length);
        stringToUTF8(host, buffer, length);
        return buffer;
    },

    GetWebSocketProtocol: function () {
        var pageProtocol = window.location.protocol;

        try {
            if (window.parent && window.parent !== window) {
                pageProtocol = window.parent.location.protocol;
            }
        } catch (e) {
            if (document.referrer) {
                pageProtocol = new URL(document.referrer).protocol;
            }
        }

        // A blob:https:// URL reports "blob:" as its protocol. Parse the
        // embedded origin so HTTPS arcade pages always use secure wss://.
        if (pageProtocol === "blob:") {
            try {
                pageProtocol = new URL(window.location.href.substring(5)).protocol;
            } catch (e) {}
        }

        var proto = (pageProtocol === "https:") ? "wss:" : "ws:";

        var length = lengthBytesUTF8(proto) + 1;
        var buffer = _malloc(length);
        stringToUTF8(proto, buffer, length);
        return buffer;
    }
});

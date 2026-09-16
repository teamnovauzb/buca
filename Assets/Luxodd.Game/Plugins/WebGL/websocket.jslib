mergeInto(LibraryManager.library, {
    ConnectWebSocket: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        var socket = new WebSocket(url);

        function sendToUnity(method, arg) {
            var attempts = 0;

            function deliver() {
                var instance = window.unityInstance;
                if (instance && typeof instance.SendMessage === "function") {
                    if (typeof arg === "undefined") {
                        instance.SendMessage('WebSocketLibraryWrapper', method);
                    } else {
                        instance.SendMessage('WebSocketLibraryWrapper', method, arg);
                    }
                    return;
                }

                attempts++;
                if (attempts <= 240) {
                    setTimeout(deliver, 25);
                } else {
                    console.error("Unity did not become ready for WebSocket callback", method);
                }
            }

            deliver();
        }

        window.UnityWebSocket = socket;

        socket.onopen = function () {
            if (window.UnityWebSocket !== socket) {
                console.warn("Ignoring open event from a stale WebSocket");
                socket.close();
                return;
            }

            var pending = window.UnityWebSocketPendingMessages || [];
            window.UnityWebSocketPendingMessages = [];
            for (var i = 0; i < pending.length; i++) {
                try {
                    socket.send(pending[i]);
                } catch (error) {
                    console.error("Failed to flush queued WebSocket message:", error);
                    window.UnityWebSocketPendingMessages.push(pending[i]);
                    break;
                }
            }

            console.log("WebSocket connected to " + url);
            sendToUnity('OnWebSocketOpen');
        };

        socket.onmessage = function (event) {
            if (window.UnityWebSocket !== socket) {
                return;
            }
            console.log("Message received: " + event.data);
            sendToUnity('OnWebSocketMessage', event.data);
        };

        socket.onclose = function (event) {
            if (window.UnityWebSocket !== socket) {
                console.warn("Ignoring close event from a stale WebSocket");
                return;
            }
            console.log("WebSocket connection closed, code and reason:", event.code, event.reason);
            sendToUnity('OnWebSocketClose', event.code);
            window.UnityWebSocket = null;
        };

        socket.onerror = function (error) {
            if (window.UnityWebSocket !== socket) {
                return;
            }
            console.error("WebSocket connection error: ", error);
            sendToUnity('OnWebSocketError', error.type || "error");
        };
    },

    SendWebSocketMessage: function (messagePtr) {
        var message = UTF8ToString(messagePtr);
        var socket = window.UnityWebSocket;

        if (!socket) {
            console.error("No active WebSocket connection; message was not sent");
            return;
        }

        if (socket.readyState === WebSocket.CONNECTING) {
            window.UnityWebSocketPendingMessages = window.UnityWebSocketPendingMessages || [];
            if (window.UnityWebSocketPendingMessages.length < 256) {
                window.UnityWebSocketPendingMessages.push(message);
                console.warn("WebSocket is connecting; message queued");
            } else {
                console.error("WebSocket pending-message queue is full");
            }
            return;
        }

        if (socket.readyState !== WebSocket.OPEN) {
            console.error("WebSocket is not open; message was not sent (state " + socket.readyState + ")");
            return;
        }

        try {
            console.log("Sending message: " + message);
            socket.send(message);
        } catch (error) {
            console.error("Failed to send WebSocket message:", error);
        }
    },

    CloseWebSocket: function () {
        if (window.UnityWebSocket) {
            console.log("Closing WebSocket connection...");
            window.UnityWebSocketPendingMessages = [];
            window.UnityWebSocket.close();
        } else {
            console.error("No active WebSocket connection to close");
        }
    },

    NavigateToHome: function () {
        var currentUrl = window.location.origin;
        var homeUrl = currentUrl + "/home";
        console.log("Navigating to: " + homeUrl);
        window.location.href = homeUrl;
    },

    SendSessionEndMessage: function () {
        console.log("Sending session_end postMessage to parent window");
        window.parent.postMessage({
            type: "session_end"
        }, "*");
    },
	
	SendSessionOptionsMessageWithAction: function (actionPtr) {
        var action = UTF8ToString(actionPtr);
        if (!action) {
            console.warn("SendSessionOptionsMessageWithAction: empty action");
            return;
        }
        console.log("Sending session_options with action:", action);
        window.parent.postMessage({ type: "session_options", action: action }, "*");
    }
});

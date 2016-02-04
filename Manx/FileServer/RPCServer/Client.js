function wsClient(url) {
    var me = this;
    var eventHandlers = {};
    var handlers = new Array(100);
    var pending = new Array();
    var socket = null;
    var open = false;
    var firstEmpty = 0;

    var eventHandler = function (key, data) {
        var h = eventHandlers[key];
        if (h) h(data);
    }

    var init = function () {
        socket = new WebSocket(url);
        socket.onopen = function () {
            open = true;
            var req = null;
            while (req = pending.pop()) me.exec.apply(me, req);
        };
        socket.onclose = function (event) {
            open = false;
            //retry connection if disconnect
            if (event.wasClean) return;
            window.setTimeout(function () { init() }, 1000);
        };

        socket.onmessage = function (event) {
            var parts = event.data.split(' ', 3);
            var key = parts[0];
            if(!key)
                var handler = key ? handlers[key] : eventHandler;
            if (!handler) return;// alert('handler not found');

            //clear callback after responce
            handlers[key] = undefined;
            if (firstEmpty > key) firstEmpty = key;
            var data = parts[2];
            handler(parts[1], data?JSON.parse(data):null);
        };

        socket.onerror = function () {
            //alert("error");
        };
    };
    me.exec = function (method, data, callback) {
        //If connection is not open - remember requests
        if (!open) return pending.push(arguments);
        data = JSON.stringify(data);
        if (data.length > 0) data = data.substring(1, data.length - 1);
        //no callback - no key
        if (!callback) return socket.send(['', method, data].join(' '));

        //Remeber callback and key is firstEmpty
        handlers[firstEmpty] = callback;
        socket.send([firstEmpty, method, data].join(' '));
        while (handlers[++firstEmpty]);
    };
    me.on = function (key,fn) {
        eventHandlers[key] = fn;
    };
    init();
}
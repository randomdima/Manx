function wsClient(url) {
    var me = this;
    var handlers = new Array(100);
    var pending = new Array();
    var socket = null;
    var open = false;
    var firstEmpty = 0;

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
            var key=parts[0];
            var handler = handlers[key];
            if (!handler) return;// alert('handler not found');

            //clear callback after responce
            handlers[key] = undefined;
            if (firstEmpty > key) firstEmpty = key;
            handler(parts[1], JSON.parse(parts[2]));
        };

        socket.onerror = function () {
            //alert("error");
        };
    };
    me.exec = function (method, data, callback) {
        //If connection is not open - remember requests
        if (!open) return pending.push(arguments);
        //Remeber callback and key is firstEmpty
        handlers[firstEmpty] = callback;
        socket.send([firstEmpty,method,JSON.stringify(data)].join(' '));
        while (handlers[++firstEmpty]);
    };
    init();
}
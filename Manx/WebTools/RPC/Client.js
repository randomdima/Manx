
window.RPCMessageType = {
    Invoke:2,
    Bind:1,
    Event:0
}
function RPCClient(url) {
    var me = this;
    var socket = null;
    var handlers = {};
    var types = new Array(100);
    var converter = new BinaryConverter();
    var objects = {};

    me.start = function () {
        socket = new WebSocket(url);
        socket.binaryType = 'arraybuffer';
        socket.onclose = function (event) {
            //retry connection if disconnect
            if (event.wasClean) return;
            window.setTimeout(function () { me.start() }, 1000);
        };
        socket.onmessage = function (event) {
            var data = converter.Convert(event.data);
           
            //switch (data.type) {
            //    case RPCMessageType.Event:
            //        return onEvent(data);
            //}
        };
        socket.onerror = function () {
            onEvent({name:'Error',args:['Onknown error']});
        };
    };
    me.send = function (data) {
        socket.send(JSON.stringify(data));
    }
    me.onError = function (fn) {
        handlers.Error = fn;
    }
    me.onStart = function (fn) {
        handlers.Start = fn;
    }
}


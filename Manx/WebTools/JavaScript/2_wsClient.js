function RPCClient(url) {
    var me = this;
    var socket = null;

    var types = new Array(100);
    var converter = new BinaryTypes.Converter();
    converter.onAction = function (data) { socket.send(data); };
    me.start = function (onStart) {
        socket = new WebSocket(url);
        socket.binaryType = 'arraybuffer';
        socket.onclose = function (event) {
            //retry connection if disconnect
            if (event.wasClean) return;
            window.setTimeout(function () { me.start() }, 1000);
        };
        socket.onmessage = function (event) {
            var message = converter.Convert(event.data);
            message.Root.MoveTo(3, 4);
            if (message instanceof RPCRootMessage)
                onStart.call(me, message.Root);

            if (message instanceof RPCCallBackMessage)
                message.fn.apply(null,message.arg);

            if(message instanceof RPCInvokeMessage)
                alert('Invoke '+message.member)
        };
        socket.onerror = function () {
            onEvent({name:'Error',args:['Onknown error']});
        };
    };
}


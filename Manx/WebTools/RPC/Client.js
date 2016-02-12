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
    var objects = {};

    me.start = function () {
        socket = new WebSocket(url);
        socket.onclose = function (event) {
            //retry connection if disconnect
            if (event.wasClean) return;
            window.setTimeout(function () { me.start() }, 1000);
        };
        socket.onmessage = function (event) {
            var data = refObject(eval('('+event.data+')'));           
            switch (data.type) {
                case RPCMessageType.Event:
                    return onEvent(data);
            }
        };
        socket.onerror = function () {
            onEvent({name:'Error',args:['Onknown error']});
        };
    };
    var refObject = function (obj) {
        if (typeof (obj) != 'object') return obj;
        if (obj.$id != null) {
            var exist = objects[obj.$id];
            if (exist) return exist;

            var t = obj.$type;
            delete obj.$type;
            if (!types[t.id]) types[t.id] = CreateDynamicClass(t, me);            
            objects[obj.$id] = obj = new types[t.id](obj);
        }

        for (var q in obj) 
            obj[q] = refObject(obj[q]);
        
        return obj;
    }
    var onEvent = function (data) {
        if (!data.obj)
            handlers[data.member].apply(this, data.args);
        else data.obj.fireEvent(data.member, data.args, true);
    }
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


function CreateDynamicClass(type, socket) {
    var t = function (data) {
        for (var q in data)
            this[q] = data[q];
        return this;
    }
    t.prototype.toJSON = function () { return this.$id; }
    //t.prototype._type = type.name;
    t.prototype.fireEvent = function (name, args, local) {
        if(local)
            this.handlers[name].apply(this, args);
        else socket.send({
                    type: RPCMessageType.Event,
                    obj: this,
                    member: name,
                    args:args
                });
    }

    var addMethod = function (name) {
        t.prototype[name] = function () {
            socket.send({
                type: RPCMessageType.Invoke,
                obj: this,
                member: name,
                args: Array.prototype.slice.call(arguments)
            });
        }
    }
    var addEvent = function(name) {
        if (name.indexOf('on') == 0 || name.indexOf('On') == 0) name = name.substring(2);
        t.prototype['on'+name] = function (callback) {
            if (!this.handlers) this.handlers = {};
            this.handlers[name] = callback;
            socket.send({
                type:RPCMessageType.Bind,
                obj: this,
                member: name
            });
        }
    }

    for (var m in type.methods) addMethod(type.methods[m]);
    for (var e in type.events) addEvent(type.events[e]);

    return t;
}
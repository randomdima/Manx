window.BinaryTypes = {
    String: function (Binary) {
        this.IsSealed = true;
        this.IsValueType = true;
        this.Read = function () {
            var len = Binary.UInt16.Read();
            var str = String.fromCharCode.apply(null, new Int8Array(Binary.buffer, Binary.offset, len));
            Binary.offset += len;
            return str;
        }
        this.Write = function () {
        }
        this.GetSize = function () {
        }
    },
    Boolean: function (Binary) {
        this.IsSealed = true;
        this.IsValueType = true;
        this.Read = function () {
            return Binary.UInt8.Read() == 1;
        }
        this.Write = function () {

        }
        this.GetSize = function () {
            return 1;
        }
    },
    Number: function (Binary,type) {
        this.IsSealed = true;
        this.IsValueType = true;
        this.reader = 'get' + type;
        var size;
        type = 'get' + type;
        switch (type.substring(type.length - 2)) {
            case 't8': size = 1; break;
            case '16': size = 2; break;
            case '32': size = 4; break;
            case '64': size = 8; break;
        }
        this.Read = function () {
            var val = Binary.view[type](Binary.offset, true);
            Binary.offset += size;
            return val;
        }
        this.Write = function () {

        }
        this.GetSize = function () {
            return size;
        }
    },
    Array: function (Binary,type) {
        this.IsSealed = true;
        this.IsValueType = true;
        var cnv = Binary.GetConverter(type);
        this.Read = function () {
            var len = Binary.UInt16.Read();
            var res = new Array(len);
            for (var q = 0; q < len; q++) res[q] = cnv.Read(type);
            return res;
        }
        this.Write = function () {

        }
        this.GetSize = function () {
        }
    },
    Dictionary: function (Binary,type) {
        this.IsSealed = true;
        this.IsValueType = true;
        var cnv = Binary.GetConverter(type);
        this.Read = function () {
            var len = Binary.UInt16.Read();
            var res = {};
            while (len-- > 0)
                res[Binary.String.Read()] = cnv.Read(type);
            return res;
        }
        this.Write = function () {

        }
        this.GetSize = function () {
        }
    },
    Object: function (Binary,cfg) {
        for (var q in cfg) this[q] = cfg[q];
        var cls = eval('(function ' + this.Name + '(){})');
        window[this.Name] = cls;
        //  var cls = function () { };
        for (var q in this.Methods)
            cls.prototype[q] = function () { };
        for (var q in this.Events) {
            if (q.substring(0, 2).toLocaleLowerCase() == 'on') q = q.substring(2);
            cls.prototype['on' + q] = function () { };
        }

        var types = [];
        var assign = [];
        for (var q in this.Properties) {
            types.push('var T_' + q + '=this.Properties["'+q+'"];');
            types.push('var C_' + q + '=Binary.GetConverter(T_' + q + ');');
            assign.push('obj.'+q+'=C_'+q+'.Read(T_'+q+');\r\n');
        }
        this.Read = eval(types.join('\r\n') + '(function(){var obj=new cls();\r\n ' + assign.join('\r\n') + '\r\n return obj;})');

        this.Write = function () {

        }
        this.GetSize = function () {
        }
    },
    Function: function (Binary,type) {
        this.IsSealed = true;
        this.IsValueType = false;
        this.Read = function () {
            var props = Object.keys(type.Properties);
            var setter = [];
            for (var q in props)
                setter.push(props[q] + ':' + props[q]);
            var wrap = function (obj) {
                Binary.CallFunction(fn, obj);
            };
            var fn = eval('(function(' + props.join(',') + '){return wrap({' + setter.join(',') + '});})');
            return fn;
        }
    },
    Type: function (Binary) {
        this.IsSealed = false;
        this.IsValueType = false;
        var ObjCnv = new BinaryTypes.Object(Binary,
            {
                Name: 'RuntimeType',
                Properties: {
                    Name: Binary.String,
                    IsSealed: Binary.Boolean,
                    Methods: Binary.TypeDictionary,
                    Properties: Binary.TypeDictionary,
                    Events: Binary.TypeDictionary
                }
            });
        this.Read = function () {
            switch (Binary.UInt8.Read()) {
                case 0:
                    return new BinaryTypes.Object(Binary,ObjCnv.Read());
                case 1:
                    return new BinaryTypes.Array(Binary,Binary.Read(Binary.Type));
                case 2:
                    return new Binary.Dictionary(Binary, Binary.Read(Binary.Type));
                case 3:
                    return new BinaryTypes.Function(Binary, Binary.Read(Binary.Type));
            }
        }

    },
    Converter: function (cfg) {
        var Binary = this;
        var RefLength = 1;
        var RefStorage = new Array(65535);
        this.AddReference = function (value, key) { return RefStorage[value._key = (key || RefLength++)] = value; }
        this.GetReference = function (key) { return key ? RefStorage[key] : null; }
        this.GetKey = function (value) { return value == null ? 0 : value._key; }
        this.CallFunction = function (fn, arg) {
            cfg.CallFunction(this.Write([fn,arg]));
        }
        this.Convert = function (buffer) {
            Binary.offset = 0;
            Binary.view = new DataView(Binary.buffer = buffer);
            return Binary.Read(Binary.Object);
        }
        this.GetConverter = function (type) {
            return (type && type.IsValueType) ? type : Binary;
        }
        this.Read = function (type) {
            var key = Binary.UInt16.Read();
            var ref=Binary.GetReference(key);
            if (ref !== undefined) return ref;
            if(!type.IsSealed) type=Binary.Read(Binary.Type);
            return Binary.AddReference(type.Read(type),key);
        }
        this.Write = function (value) {
            var key = this.GetKey(value);
            Binary.UInt16.Write(key);
        }
        this.Null = { _key: 0 };
        this.AddReference({});
        this.AddReference(this.Boolean = new BinaryTypes.Boolean(this));
        this.AddReference(this.UInt8 = new BinaryTypes.Number(this, 'Uint8'));
        this.AddReference(this.UInt16 = new BinaryTypes.Number(this, 'Uint16'));
        this.AddReference(this.Int32 = new BinaryTypes.Number(this, 'Int32'));
        this.AddReference(this.String = new BinaryTypes.String(this));
        this.AddReference(this.Object = this);
        this.AddReference(this.Void = {});
        this.AddReference(this.TypeDictionary = new BinaryTypes.Dictionary(this,Binary.Object));
        this.AddReference(this.Type = new BinaryTypes.Type(this));
    }
};
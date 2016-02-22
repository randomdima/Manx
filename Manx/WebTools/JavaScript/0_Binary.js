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
        this.Write = function (value) {
            var len=(value?value.length:0);
            Binary.UInt16.Write(len);
            if (len == 0) return;
            var bufView = new Uint8Array(Binary.buffer,Binary.offset,len);
            for (var q=0;q<len;q++) 
                bufView[q] = value.charCodeAt(q);
            Binary.offset += len;
        }
        this.GetSize = function (value) {
            return  Binary.UInt16.Size+ (value ? value.length : 0);
        }
        this.Validate=function(value,name)
        {
            if (value == null) return null;
            if (typeof value != 'string') return value.toString();
            return value;             
        }
    },
    Boolean: function (Binary) {
        this.IsSealed = true;
        this.IsValueType = true;
        this.Read = function () {
            return Binary.UInt8.Read() == 1;
        }
        this.Write = function (value) {
            Binary.UInt8.Write(value);
        }
        this.GetSize = function () {
            return 1;
        }
        this.Validate = function (value, name) {
            return !!value;
        }
    },
    Number: function (Binary,type) {
        this.IsSealed = true;
        this.IsValueType = true;
        //this.reader = 'get' + type;
        this.Size;
        switch (type.substring(type.length - 2)) {
            case 't8': this.Size = 1; break;
            case '16': this.Size = 2; break;
            case '32': this.Size = 4; break;
            case '64': this.Size = 8; break;
        }
        var reader = 'get' + type;
        var writer = 'set' + type;
        this.Read = function () {
            var val = Binary.view[reader](Binary.offset, true);
            Binary.offset += this.Size;
            return val;
        }
        this.Write = function (value) {
            Binary.view[writer](Binary.offset,value, true);
            Binary.offset += this.Size;
        }
        this.GetSize = function () {
            return this.Size;
        }
        this.Validate = function (value, name) {
            value = parseInt(value);
            if (value == NaN)
                throw "Numeric Parameter "+name+" Has Incorrect Value ["+value+"]";
            return parseInt(value)||0;
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
        this.Write = function (value) {
            var len = value ? value.length : 0;
            Binary.UInt16.Write(len);
            for (var q = 0; q < len; q++)
                cnv.Write(value[q]);
        }
        this.GetSize = function (value) {
            var size = Binary.UInt16.Size;
            var len = value ? value.length : 0;
            for (var q = 0; q < len; q++)
                size += cnv.GetSize(value[q]);
            return size;
        }
        this.Validate = function (value, name) {
            if (value == null) return null;
            if (!Array.isArray(value))
                    throw "Array Parameter " + name + " Has Incorrect Value [" + value + "]";
            return value;
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
        this.Write = function (value) {
            var len = value ? Object.keys(value).length : 0;
            Binary.UInt16.Write(len);
            for (var q in value) {
                Binary.String.Write(q);
                cnv.Write(value[q]);
            }
        }
        this.GetSize = function () {
            var size = Binary.UInt16.Size;
            var len = value ? Object.keys(value).length : 0;
            for (var q in value) 
                size += Binary.String.GetSize(q) + cnv.GetSize(value[q]);            
            return size;
        }
        this.Validate = function (value, name) {
            if (value == null) return null;
            if (typeof value != 'object')
                throw "Dictionary Parameter " + name + " Has Incorrect Value [" + value + "]";
            return value;
        }
    },
    Object: function (Binary,cfg) {
        for (var q in cfg) this[q] = cfg[q];
        var cls = eval('(function ' + this.Name + '(cfg){this.constructor(cfg);})');
        this.cls = cls;
        window[this.Name] = cls;
        cls.prototype.type = this;
        cls.prototype.constructor = function (cfg) {
            if (!cfg) return;
            if (cfg._forced)
            {
                for (var q in cfg)
                    this[q] = cfg[q];
                return;
            }
            for (var q in cfg) {
                var P = this.type.Properties[q];
                if (!P) continue;
                this[q] = P.Validate(cfg[q],q);
            }
        };
     
        for (var q in this.Methods)  cls.prototype[q] = this.Methods[q];
        var types = [];
        var reader = [];
        var writer = [];
        var sizer = [];
        for (var q in this.Properties) {
            types.push('var T_' + q + '=this.Properties["'+q+'"];');
            types.push('var C_' + q + '=Binary.GetConverter(T_' + q + ');');
            reader.push(q + ':C_' + q + '.Read(T_' + q + ')');
            writer.push('C_' + q + '.Write(value.' + q + ');');
            sizer.push('C_' + q + '.GetSize(value.' + q + ')');
        }
        eval(types.join(''));
        this.Read = eval('(function(){return new cls({_forced:1,' + reader.join() + '});})');
        this.Write = eval('(function(value){' + writer.join('') + '})');
        this.GetSize = eval('(function(value){ return ' + sizer.join('+') + ';})');
        this.Validate = function (value, name) {
            if (value == null) return null;
            if (value.type !=this)
               throw this.Name+" Parameter " + name + " Has Incorrect Value [" + value + "]";
            return value;
        }
    },
    Function: function (Binary,types) {
        this.IsSealed = true;
        this.IsValueType = false;
        var cnv = new Array(types.length-1);
        for (var q = 1; q < types.length; q++)
            cnv[q-1] = Binary.GetConverter(types[q]);

        this.Read = function () {            
            var fn = function () {
                var size = Binary.UInt16.Size * 4;
                for (var q = 0; q < cnv.length; q++)
                    size += cnv[q].GetSize(arguments[q]);
                Binary.offset = 0;
                Binary.view = new DataView(Binary.buffer = new ArrayBuffer(size));
                Binary.UInt16.Write(321); // new req id?
                Binary.UInt16.Write(Binary.FunctionCall._key);
                Binary.UInt16.Write(fn._key);
                Binary.UInt16.Write(this._key);
                for (var q = 0; q < cnv.length; q++)
                    size += cnv[q].Write(arguments[q]);
                Binary.onAction(Binary.buffer);
            };
            fn.params = cnv;
            return fn;
        }
        this.Write = function () {
            console.log('No func write');
        }
        this.GetSize = function () {
            console.log('No func get size');
        }
        this.Validate = function (value, name) {
            if (value == null) return null;
            if (typeof value != 'function') throw "Function Parameter " + name + " Has Incorrect Value [" + value + "]";
            if(!value._key) Binary.AddReference(value);
            return value;
        }
    },
    Type: function (Binary) {
        this.IsSealed = false;
        this.IsValueType = false;
        this.Read = function () {
            switch (Binary.UInt8.Read()) {
                case 0:
                    return new BinaryTypes.Object(Binary, Binary.TypeInfo.Read());
                case 1:
                    return new BinaryTypes.Array(Binary,Binary.Read(Binary.Type));
                case 2:
                    return new Binary.Dictionary(Binary, Binary.Read(Binary.Type));
                case 3:
                    return new BinaryTypes.Function(Binary, Binary.TypeArray.Read());
            }
        }
        this.Write = function () {
            console.log('No type write');
        }
        this.GetSize = function () {
            console.log('No type get size');
        }        
        this.Validate = Binary.Validate;
    },
    Converter: function (cfg) {
        this.onAction = function () { };
        for (var q in cfg) this[q] = cfg[q];
        var Binary = this;
        var RefLength = 1;
        var RefStorage = new Array(65535);
        this.AddReference = function (value, key) {
            if (!key) key = RefLength++;
            else if (key >= RefLength) RefLength = key+1;
            return RefStorage[value._key = key] = value;
        }
        this.GetReference = function (key) { return key ? RefStorage[key] : null; }
        this.GetKey = function (value) { return value == null ? 0 : value._key; }
        this.Convert = function (value) {
            Binary.offset = 0;
            if (value instanceof ArrayBuffer) {
                Binary.view = new DataView(Binary.buffer = value);
                return Binary.Read(Binary.Object);
            }
            else {
                Binary.view = new DataView(Binary.buffer = new ArrayBuffer(Binary.GetSize(value)));
                Binary.Write(value);
                return Binary.buffer;
            }
        }
        this.GetConverter = function (type) {
            return (type && type.IsValueType) ? type : Binary;
        }
        this.Read = function (type) {
            var key = Binary.UInt16.Read();
            var ref=Binary.GetReference(key);
            if (ref !== undefined) return ref;
            if (!type.IsSealed) type = Binary.Read(Binary.Type);
            Binary.AddReference({ Validate: function (x) { return x;}}, key);
            return Binary.AddReference(type.Read(type),key);
        }
        this.WriteRef = function (value) {
            var key = this.GetKey(value);
            if (key != null) {
                Binary.UInt16.Write(key);
                return true;
            }
            Binary.UInt16.Write(this.AddReference(value)._key);
            return false;
        }
        this.Write = function (value) {
            if (this.WriteRef(value)) return;
            if(!value.type.IsSealed)
                this.Write(value.type);
            value.type.Write(value);
        }
        this.GetSize = function (value) {
            var key = this.GetKey(value);
            if (key != null) 
                return Binary.UInt16.Size;
            var size = Binary.UInt16.Size + value.type.GetSize(value);
            if (value.type.IsSealed)
               return size;
            return size + this.GetSize(value.type);
        }
        this.Validate = function (value, name) {
            return value;
            //if (value == null) return null;
            //if (!Binary.GetKey(value))
            //    throw "Reference Parameter " + name + " Has Incorrect Value [" + value + "]";
            //return value;
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
        this.AddReference(this.TypeDictionary = new BinaryTypes.Dictionary(this, Binary.Object));
        this.AddReference(this.TypeArray = new BinaryTypes.Array(this,Binary.Object));
        this.AddReference(this.MethodInfo = new BinaryTypes.Object(this,
            {
                Name: 'MethodInfo',
                Properties: {
                    Arguments: TypeArray,
                    Deletage: Binary.Object
                }
            }));
        this.AddReference(this.MethodDictionary = new BinaryTypes.Dictionary(this, Binary.MethodInfo));
        this.AddReference(this.TypeInfo = new BinaryTypes.Object(this,
            {
                Name: 'RuntimeType',
                Properties: {
                    Name: Binary.String,
                    IsSealed: Binary.Boolean,
                    Methods: Binary.MethodDictionary,
                    Properties: Binary.TypeDictionary
                }
            }));
        this.AddReference(this.Type = new BinaryTypes.Type(this));
        this.AddReference(this.FunctionCall = {});
    }
};
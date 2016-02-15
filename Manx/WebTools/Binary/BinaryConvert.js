var BinaryTypes = {
    Type: { key: -1051447098, IsSealed: true },
    Object: { key: -737641570 },
    Int: { key: -118636331, IsSealed: true },
    String: { key: 1236129805, IsSealed: true }
}
BinaryTypes.MemberInfo = { key: 659150170, IsSealed: true, Properties: { Name: BinaryTypes.String, Type: BinaryTypes.Int } };
BinaryTypes.MemberArray = { key: -1, IsSealed: true, elementType: BinaryTypes.MemberInfo };
BinaryTypes.TypeInfo = { key: -134522806, IsSealed: true, Properties: { Methods: BinaryTypes.MemberArray, Properties: BinaryTypes.MemberArray } };

function BinaryConverter() {
    var me = this;
    me.offset=0;
    me.buffer=null;
    me.view=null;
    me.Providers = [ObjectProvider,ArrayProvider];
    me.Converters = {
        Int: new IntConverter(),
        Byte:new ByteConverter(),
        String: new StringConverter()
    };
    me.ReferenceStorage = {
        0: null
    };
    for (var e in BinaryTypes) {
        e = BinaryTypes[e];
        if (!(e.key in me.ReferenceStorage))
            me.ReferenceStorage[e.key] = e;
    }
}
BinaryConverter.prototype.Convert = function (buffer) {
    this.offset=0;
    this.buffer=buffer;
    this.view=new DataView(buffer);
    var cnv = this.GetConverter(BinaryTypes.Object);
    return cnv.Read(this);
}
BinaryConverter.prototype.GetConverter = function (type) {
    var cnv = this.Converters[type.key];
    if (cnv) return cnv;
    var q = this.Providers.length;
    while (q--) 
        if (cnv = this.Providers[q](type))
            return this.Converters[type.key] = cnv;
}
BinaryConverter.prototype.Read = function (type) {
    return this.GetConverter(type||BinaryTypes.Object).Read(this);
}


function ArrayProvider(type) {
    if (type.elementType)
        return new ArrayConverter(type);
}
function ArrayConverter(type) {
    this.elementType = type.elementType;
}
ArrayConverter.prototype.Read = function (raw) {
    var len = raw.Converters.Byte.Read(raw);
    var res = new Array(len);
    var cnv = raw.GetConverter(this.elementType);
    for (var q = 0; q < len; q++) type.methods[q] = cnv.Read(raw);
    return res;
}
ArrayConverter.prototype.Write = function (raw) {
}
ArrayConverter.prototype.GetSize = function (raw) {
}

function ObjectProvider(type) {
    return new ObjectConverter(type);
}
function ObjectConverter(type) { this.type = type; }
ObjectConverter.prototype.Read = function (raw) {
    var key = raw.Converters.Int.Read(raw);
    var ref = raw.ReferenceStorage[key];
    if (ref !== undefined) return ref;

    var type = this.type.IsSealed?this.type:raw.Read(BinaryTypes.TypeInfo);
    var obj = {key:key};
    raw.ReferenceStorage[key] = obj;
    for (var p in type.Properties) {
      //  p = type.Properties[p];
        obj[p] = raw.Read(type.Properties[p]);
    }    
    return obj;
}
ObjectConverter.prototype.Write = function (raw) {
}
ObjectConverter.prototype.GetSize = function (raw) {
}


function StringConverter() {}
StringConverter.prototype.Read = function (raw) {
    var len = raw.buffer[raw.offset++];
    var str = String.fromCharCode.apply(null, raw.buffer.slice(raw.offset, raw.offset + len));
    raw.offset += len;
    return str;
}
StringConverter.prototype.Write = function (raw) {
}
StringConverter.prototype.GetSize = function (raw) {
}



function IntConverter() {}
IntConverter.prototype.Read = function (raw) {
    var val = raw.view.getInt32(raw.offset);
    raw.offset += 4;
    return val;
}
IntConverter.prototype.Write = function (raw) {
}
IntConverter.prototype.GetSize = function (raw) {
}

function ByteConverter() { }
ByteConverter.prototype.Read = function (raw) {
    return raw.view.getInt8(raw.offset++);
}
ByteConverter.prototype.Write = function (raw) {
}
ByteConverter.prototype.GetSize = function (raw) {
}
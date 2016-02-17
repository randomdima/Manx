/// <reference path="1_BinaryConverter.js" />
function BinaryString() {
    this.IsSealed = true;
    this.Read = function (raw) {
        var len = Binary.UInt16.Read(raw);
        var str = String.fromCharCode.apply(null,new Int8Array(raw.buffer,raw.offset,len));
        raw.offset += len;
        return str;
    }
    this.Write = function (raw) {
    }
    this.GetSize = function (raw) {
    }
}

function BinaryBoolean() {
    this.IsSealed = true;
    this.Read = function (raw) {
        var val = raw.view.getUint8(raw.offset++, true);
        return val==1;
    }
    this.Write = function (raw) {

    }
    this.GetSize = function (raw) {
        return  1;
    }
}

function BinaryNumber(type) {
    this.IsSealed = true;
    this.reader = 'get' + type;
    var size;
    type = 'get' + type;
    switch (type.substring(type.length - 2)) {
        case 't8': size = 1; break;
        case '16': size = 2; break;
        case '32': size = 4; break;
        case '64': size = 8; break;
    }
    this.Read = function (raw) {
        var val = raw.view[type](raw.offset, true);
        raw.offset += size;
        return val;
    }
    this.Write = function (raw) {

    }
    this.GetSize = function (raw) {
        return size;
    }
}

function BinaryArray(type) {
    this.IsSealed = true;
    this.Read = function (raw) {
        var len = Binary.UInt16.Read(raw);
        var res = new Array(len);
        for (var q = 0; q < len; q++) res[q] = type.Read(raw);
        return res;
    }
    this.Write = function (raw) {

    }
    this.GetSize = function (raw) {
    }
}

function BinaryDictionary(type) {
    this.IsSealed = true;
    this.Read = function (raw) {
        var len = Binary.UInt16.Read(raw);
        var res = {};
        while (len-- > 0) 
            res[Binary.String.Read(raw)]=type.Read(raw);        
        return res;
    }
    this.Write = function (raw) {

    }
    this.GetSize = function (raw) {
    }
}

function BinaryObject(cfg) {
    if (cfg==null) {
        this.Read = function (raw) { return raw.Read(); }
        return;
    }
    this.IsSealed = cfg.IsSealed;
    var cls = eval('(function ' + cfg.Name + '(){})');
    window[cfg.Name] = cls;
  //  var cls = function () { };
    for (var q in cfg.Methods)
        cls.prototype[q] = function () { };
    for (var q in cfg.Events) {
        if (q.substring(0, 2).toLocaleLowerCase() == 'on') q = q.substring(2);
        cls.prototype['on' + q] = function () { };
    }
    this.Read = function (raw) {
        var obj = new cls();
        for (var p in cfg.Properties)
            obj[p] = cfg.Properties[p].Read(raw);
        return obj;
    }
    this.Write = function (raw) {

    }
    this.GetSize = function (raw) {
    }
}
/// <reference path="0_BinaryTypes.js" />
function BinaryType() {
    var ObjCnv = new BinaryObject({
        Name:'RuntimeType',
        Properties: { 
            Name:Binary.String,
            IsSealed:Binary.Boolean,
            Methods:Binary.TypeDictionary,
            Properties: Binary.TypeDictionary,
            Events:Binary.TypeDictionary
        }
    });
    this.Read = function (raw) {
        switch (Binary.UInt8.Read(raw)) {
            case 0:
                return new BinaryObject(ObjCnv.Read(raw));
            case 1:
                return new BinaryArray(raw.GetConverter(raw.Read()));
            case 2:
                return new BinaryDictionary(raw.GetConverter(raw.Read()));
        }
    }

}


window.Binary = {
    Null: {},
    KnownType: {},
    Boolean:new BinaryBoolean(),
    UInt8: new BinaryNumber('Uint8'),
    UInt16: new BinaryNumber('Uint16'),
    Int32: new BinaryNumber('Int32'),
    String: new BinaryString(),
    Object: new BinaryObject(),
    Void: {}
}
Binary.TypeDictionary = new BinaryDictionary(Binary.Object);
Binary.Type = new BinaryType();

function BinaryConverter() {
    var ReferenceStorageLength = 0;
    var ReferenceStorage = new Array(65535);

    this.AddReference = function (value) {
        ReferenceStorage[value._key = ReferenceStorageLength++] = value;
    }
    for (var e in Binary) this.AddReference(Binary[e]);
    ReferenceStorage[0] = null;

    this.Convert = function (buffer) {
        this.offset = 0;
        this.view = new DataView(this.buffer = buffer);
        return this.Read();
    }
    this.GetConverter = function (type) {
        return type.IsSealed ? type : this;
    }
    this.Read = function () {
        var key = Binary.UInt16.Read(this);
        if (ReferenceStorage[key] !== undefined) return ReferenceStorage[key];
        return ReferenceStorage[key] = this.Read().Read(this);
    }
}
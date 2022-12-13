module Angara {

    export class Guid {
        private id: string;
        private static emptyGuid = new Guid("00000000-0000-0000-0000-000000000000");
        public ToString() { return this.id; }
        constructor(id: string) {
            if (id.length != 36) throw new Error("String format must be 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx' format");
            this.id = id.toLowerCase();
        }
        static Empty() {
            return Guid.emptyGuid;
        }
        static NewGuid() {
            return new Guid(
                Guid.s4() + Guid.s4() + '-' + Guid.s4() + '-4' + Guid.s4().substr(0, 3) + '-' +
                Guid.s4() + '-' + Guid.s4() + Guid.s4() + Guid.s4()
            );
        }

        private static s4() {
            return Math
                .floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
    }

    export enum InfoSetType {
        Null,
        Bool,
        Int,
        Double,
        String,
        Raw, // JavaScript specifics. InfoSet content is raw object. Raw type is useful when
        // you need to pass information directly to JSON marshaller
        Artefact,
        Map,
        DateTime,
        Guid,
        BoolArray,
        IntArray,
        DoubleArray,
        StringArray,
        DateTimeArray,
        Seq,
    }

    class Base64 {
        private static b64ToUint6(nChr: number) {
            return nChr > 64 && nChr < 91 ? nChr - 65 : nChr > 96 && nChr < 123 ? nChr - 71 : nChr > 47 && nChr < 58 ?
                nChr + 4 : nChr === 43 ? 62 : nChr === 47 ? 63 : 0;
        }

        public static Decode(base64: string) {
            var s = window.atob(base64); // More optimal is to work directly with Uint8Array without intermediate string
            var result = new Uint8Array(s.length);
            for (var i = 0; i < result.byteLength; i++)
                result[i] = s.charCodeAt(i);
            return result;
        }

        public static Encode(arr: { buffer: ArrayBuffer }) {
            var view = new DataView(arr.buffer);
            var binary = '';
            for (var i = 0; i < view.byteLength; i++)
                binary += String.fromCharCode(view.getUint8(i));
            return window.btoa(binary); // More optimal is to work directly with Uint8Array without intermediate string
        }
    }

    export var TypeIdPropertyName = "__angara_typeId";

    export class InfoSet {
        private t: InfoSetType;
        private v: any;

        constructor(t: InfoSetType, v: any) {
            this.t = t;
            this.v = v;
        }

        public get Type() { return this.t; }

        public static get EmptyMap() { return new InfoSet(InfoSetType.Map, {}); }

        public AddInfoSet(p: string, i: InfoSet) {
            if (this.t == InfoSetType.Map) {
                this.v[p] = i;
                return this;
            }
            else throw new Error("Can add properties only to Map or Artefact");
        }

        public AddInt(p: string, n: number) { return this.AddInfoSet(p, InfoSet.Int(n)); }
        public AddString(p: string, s: string) { return this.AddInfoSet(p, InfoSet.String(s)); }
        public AddBool(p: string, b: boolean) { return this.AddInfoSet(p, InfoSet.Bool(b)); }
        public AddDateTime(p: string, d: Date) { return this.AddInfoSet(p, InfoSet.DateTime(d)); }
        public AddDouble(p: string, n: number) { return this.AddInfoSet(p, InfoSet.Double(n)); }
        public AddGuid(p: string, g: Guid) { return this.AddInfoSet(p, InfoSet.Guid(g)); }
        public AddIntArray(p: string, inta: Array<number>) { return this.AddInfoSet(p, InfoSet.IntArray(inta)); }
        public AddStringArray(p: string, sa: Array<string>) { return this.AddInfoSet(p, InfoSet.StringArray(sa)); }
        public AddBoolArray(p: string, ba: Array<boolean>) { return this.AddInfoSet(p, InfoSet.BoolArray(ba)); }
        public AddDateTimeArray(p: string, da: Array<Date>) { return this.AddInfoSet(p, InfoSet.DateTimeArray(da)); }
        public AddDoubleArray(p: string, da: Array<number> | Float64Array) { return this.AddInfoSet(p, InfoSet.DoubleArray(da)); }
        public AddSeq(p: string, s: Array<InfoSet>) { return this.AddInfoSet(p, InfoSet.Seq(s)); }

        public ToNull() {
            if (this != null && this.t != InfoSetType.Null) throw new Error("Null value expected");
            return null;
        }

        public ToBool() {
            if (this.t != InfoSetType.Bool) throw new Error("Boolean value expected");
            return <boolean>this.v;
        }

        public ToInt() {
            if (this.t != InfoSetType.Int) throw new Error("Int value expected");
            return <number>this.v;
        }

        public ToDouble() {
            if (this.t != InfoSetType.Int && this.t != InfoSetType.Double) throw new Error("Double value expected");
            return <number>this.v;
        }

        public ToString() {
            if (this.t != InfoSetType.String) throw new Error("String value expected");
            return <string>this.v;
        }

        public ToRaw() {
            switch (this.t) {
                case InfoSetType.Artefact:
                    throw new Error("Cannot convert InfoSet with Artefacts to raw representation because it loses type information");
                case InfoSetType.Map:
                    var result = {};
                    var map = <{ [p: string]: InfoSet }>(this.v);
                    for (var p in map)
                        result[p] = map[p].ToRaw();
                    return result;
                case InfoSetType.Seq:
                    var arr = <Array<InfoSet>>(this.v);
                    return arr.map(v => v.ToRaw());
                case InfoSetType.Guid:
                    var guid = <Guid>(this.v);
                    return guid.ToString();
                default:
                    return this.v;
            }
        }

        public ToGuid() {
            if (this.t != InfoSetType.Guid) throw new Error("Guid value expected");
            return <Guid>this.v;
        }

        public ToMap() {
            if (this.t != InfoSetType.Map) throw new Error("Map value expected");
            return <{ [key: string]: InfoSet }>this.v;
        }

        public ToArtefact() { // typeId * contents
            if (this.t != InfoSetType.Artefact) throw new Error("Artefact value expected");
            var typeId = <string>(this.v[0]);
            var content = <InfoSet>(this.v[1]);
            return { TypeId: typeId, Content: content };
        }

        public ToDateTime() {
            if (this.t != InfoSetType.DateTime) throw new Error("DateTime value expected");
            return <Date>this.v;
        }

        public ToBoolArray() {
            if (this.t != InfoSetType.BoolArray) throw new Error("Array of booleans expected");
            return <boolean[]>this.v;
        }

        public ToIntArray() {
            if (this.t != InfoSetType.IntArray) throw new Error("Array of integers expected");
            return <number[] | Int32Array>this.v;
        }

        public ToDoubleArray() {
            if (this.t != InfoSetType.DoubleArray && this.t != InfoSetType.IntArray) throw new Error("Array of doubles expected");
            return <number[] | Float64Array>this.v;
        }

        public ToStringArray() {
            if (this.t != InfoSetType.StringArray) throw new Error("Array of strings expected");
            return <Array<string>>this.v;
        }

        public ToDateTimeArray() {
            if (this.t != InfoSetType.DateTimeArray) throw new Error("Array of DateTime expected");
            return <Array<Date>>this.v;
        }

        public ToSeq() {
            if (this.t != InfoSetType.Seq) throw new Error("Sequence expected");
            return <Array<InfoSet>>this.v;
        }

        public get IsBool() { return this.t == InfoSetType.Bool; }
        public get IsInt() { return this.t == InfoSetType.Int; }
        public get IsDouble() { return this.t == InfoSetType.Double; }
        public get IsString() { return this.t == InfoSetType.String; }
        public get IsRaw() { return this.t == InfoSetType.Raw; }
        public get IsGuid() { return this.t == InfoSetType.Guid; }
        public get IsArtefact() { return this.t == InfoSetType.Artefact; }
        public get IsMap() { return this.t == InfoSetType.Map; }
        public get IsDateTime() { return this.t == InfoSetType.DateTime; }
        public get IsBoolArray() { return this.t == InfoSetType.BoolArray; }
        public get IsIntArray() { return this.t == InfoSetType.IntArray; }
        public get IsDoubleArray() { return this.t == InfoSetType.DoubleArray; }
        public get IsStringArray() { return this.t == InfoSetType.StringArray; }
        public get IsDateTimeArray() { return this.t == InfoSetType.DateTimeArray; }
        public get IsSeq() { return this.t == InfoSetType.Seq; }
        public get IsNull() { return this.t == InfoSetType.Null; }

        public static Null() { return new InfoSet(InfoSetType.Null, null); }
        public static Bool(b: boolean) { return new InfoSet(InfoSetType.Bool, b); }
        public static Int(n: number) { return new InfoSet(InfoSetType.Int, n); }
        public static Double(n: number) { return new InfoSet(InfoSetType.Double, n); }
        public static DoubleArray(d: number[] | Float64Array) { return new InfoSet(InfoSetType.DoubleArray, d); }
        public static String(s: string) { return new InfoSet(InfoSetType.String, s); }
        public static Raw(r: any) { return new InfoSet(InfoSetType.Raw, r); }
        public static Guid(g: Guid) { return new InfoSet(InfoSetType.Guid, g); }
        public static Artefact(typeId: string, content: InfoSet) { return new InfoSet(InfoSetType.Artefact, [typeId, content]); }
        public static Map(m: { [p: string]: InfoSet }) { return new InfoSet(InfoSetType.Map, m); }
        public static DateTime(d: Date) { return new InfoSet(InfoSetType.DateTime, d); }
        public static BoolArray(b: boolean[] | Int8Array) { return new InfoSet(InfoSetType.BoolArray, b); }
        public static IntArray(n: number[] | Int32Array) { return new InfoSet(InfoSetType.IntArray, n); }
        public static StringArray(s: Array<string>) { return new InfoSet(InfoSetType.StringArray, s); }
        public static DateTimeArray(d: Array<Date>) { return new InfoSet(InfoSetType.DateTimeArray, d); }
        public static Seq(s: Array<InfoSet>) { return new InfoSet(InfoSetType.Seq, s); }

        private static EncodeNameAndType(n: string, t: string | null) {
            n = n.replace(":", "::");
            return t == null ? n : n + ":" + t;
        }

        private static DecodeNameAndType(s: string): [string, string | null] {
            var idx = s.lastIndexOf(":");
            if (idx == 0 || idx > 0 && s[idx - 1] != ':')
                return [s.substring(0, idx).replace("::", ":"), s.substring(idx + 1)];
            else
                return [s.replace("::", ":"), null];
        }

        private static Encode(i: InfoSet): [any, string | null] {
            if (i == null || i.IsNull)
                return [null, null];
            if (i.IsBool)
                return [i.v, null];
            else if (i.IsDateTime)
                return [i.v, "datetime"];
            else if (i.IsInt)
                return [i.v, "int"];
            else if (i.IsDouble)
                return [i.v, null];
            else if (i.IsString)
                return [i.v, null];
            else if (i.IsRaw)
                return [i.v, null];
            else if (i.IsGuid)
                return [(<Guid>i.v).ToString(), "guid"];
            else if (i.IsArtefact) {
                var result = <[string, InfoSet]>i.v;
                return [InfoSet.Encode(result[1])[0], result[0]];
            } else if (i.IsBoolArray) {
                var arr = new Array<number>();
                var boolarr = <boolean[]>i.v;
                for (var k = 0; k < boolarr.length; k++)
                    arr[k] = boolarr[k] ? 1 : 0;
                return [Base64.Encode(i.v instanceof Int8Array ? i.v : new Int8Array(arr)), "bool array"];
            } else if (i.IsIntArray)
                return [Base64.Encode(i.v instanceof Int32Array ? i.v : new Int32Array(<number[]>i.v)), "int array"];
            else if (i.IsDoubleArray)
                return [Base64.Encode(i.v instanceof Float64Array ? i.v : new Float64Array(<number[]>i.v)), "double array"];
            else if (i.IsStringArray) {
                return [i.v, i.v.length == 0 ? "string array" : null];
            } else if (i.IsDateTimeArray) {
                var arr = new Array<number>();
                var datearr = <Date[]>i.v;
                for (var k = 0; k < datearr.length; k++)
                    arr[k] = datearr[k].valueOf();
                return [Base64.Encode(i.v instanceof Float64Array ? i.v : new Float64Array(arr)), "datetime array"];
            } else if (i.IsSeq) {
                var seq = <InfoSet[]>i.v;
                return [seq.map(i => InfoSet.Marshal(i)), null]
            } else if (i.IsMap) {
                var res = {};
                for (var p in i.v) {
                    var encoded = InfoSet.Encode(i.v[p]);
                    var key = InfoSet.EncodeNameAndType(p, encoded[1]);
                    res[key] = encoded[0];
                }
                return [res, null]
            }
            throw new Error("Incompatible input InfoSet.");
        }

        public static Marshal(i: InfoSet) {
            var encoded = InfoSet.Encode(i);
            if (encoded[1] == null)
                return encoded[0];
            else {
                var r = {};
                r[InfoSet.EncodeNameAndType("", encoded[1])] = encoded[0];
                return r;
            }
        }

        public static Unmarshal(t: any): InfoSet {
            var infoSet = InfoSet.Decode([t, null]);
            if (infoSet.IsMap) {
                var map = infoSet.ToMap();
                var names: string[] = [];
                var k = 0;
                for (var p in map) {
                    names[k] = p;
                    k++;
                }
                if (names.length == 1 && names[0] == "") {
                    return map[""];
                }
                else {
                    return infoSet;
                }
            }
            else return infoSet;
        }

        private static DecodeMap(json: any): InfoSet {
            var map = InfoSet.EmptyMap;
            for (var p in json) {
                var nameType = InfoSet.DecodeNameAndType(p);
                map = map.AddInfoSet(nameType[0], InfoSet.Decode([json[p], nameType[1]]));
            }
            return map;
        }

        private static Decode(t: [any, string | null]): InfoSet {
            var json = t[0];
            var typeId = t[1];
            if (json == null)
                return InfoSet.Null();
            else if (typeId == "int")
                return InfoSet.Int(<number>json);
            else if (typeId == "datetime")
                return InfoSet.DateTime(<Date>json);
            else if (typeId == "guid")
                return InfoSet.Guid(new Guid(json));
            else if (typeId == "int array")
                return InfoSet.IntArray(new Int32Array(Base64.Decode(json).buffer));
            else if (typeId == "string array")
                return InfoSet.StringArray(json);
            else if (typeId == "datetime array") {
                var arrd = new Array<Date>();
                var int32arr = new Float64Array(Base64.Decode(json).buffer);
                for (var k = 0; k < int32arr.length; k++)
                    arrd[k] = new Date(int32arr[k]);
                return InfoSet.DateTimeArray(arrd);
            } else if (typeId == "bool array") {
                var arr = new Array<boolean>();
                var int8arr = new Int8Array(Base64.Decode(json).buffer);
                for (var k = 0; k < int8arr.length; k++)
                    arr[k] = int8arr[k] == 1 ? true : false;
                return InfoSet.BoolArray(arr);//(<Array<boolean>>json);
            } else if (typeId == "double array")
                return InfoSet.DoubleArray(new Float64Array(Base64.Decode(json).buffer));
            else if (typeId == "array") {
                var length = json.length;
                var a = new Array<InfoSet>(length);
                for (var i = 0; i < length; i++)
                    a[i] = InfoSet.Unmarshal(json[i]);
                return InfoSet.Seq(a);
            } else if (typeId != null) {
                var typeEnd = typeId.split(" ");
                if (typeEnd.length > 0 && typeEnd[typeEnd.length - 1] == "array") {
                    return InfoSet.Artefact(typeId, InfoSet.Decode([json, "array"]));
                }
                else
                    return InfoSet.Artefact(typeId, InfoSet.Decode([json, null]));

            } else /* typeId == null */ if (typeof json == "boolean")
                return InfoSet.Bool(<boolean>json);
            else if (typeof json == "number")
                return InfoSet.Double(<number>json);
            else if (typeof json == "string")
                return InfoSet.String(<string>json);
            else if (json instanceof Array) {
                var objs = <Array<any>>json;
                var allStrings = objs.every(s => typeof (s) === "string");
                if (allStrings && objs.length > 0)
                    return InfoSet.StringArray(<Array<string>>objs);
                else
                    return InfoSet.Seq(objs.map(i => InfoSet.Unmarshal(i)));
            } else if (typeof json == "object") {
                return InfoSet.DecodeMap(json);
            } else
                throw new Error("Unsupported object type " + typeof json);
        }

        public static Deserialize(is: Angara.InfoSet): any {
            if (is.IsNull) {
                return null;
            }
            if (is.IsBool) {
                return is.ToBool();
            }
            if (is.IsDouble) {
                return is.ToDouble();
            }
            if (is.IsInt) {
                return is.ToInt();
            }
            if (is.IsString) {
                return is.ToString();
            }
            if (is.IsDateTime) {
                return is.ToDateTime();
            }
            if (is.IsRaw) {
                return is.ToRaw();
            }
            if (is.IsGuid) {
                return is.ToGuid().ToString();
            }
            if (is.IsBoolArray) {
                return is.ToBoolArray();
            }
            if (is.IsDoubleArray) {
                return is.ToDoubleArray();
            }
            if (is.IsIntArray) {
                return is.ToIntArray();
            }
            if (is.IsStringArray) {
                return is.ToStringArray();
            }
            if (is.IsDateTimeArray) {
                return is.ToDateTimeArray();
            }
            if (is.IsMap) {
                var map = is.ToMap();
                var obj = {};
                for (var key in map) {
                    obj[key] = InfoSet.Deserialize(map[key]);
                }
                return obj;
            }
            if (is.IsArtefact) {
                var artefact = is.ToArtefact();
                var artCont = InfoSet.Deserialize(artefact.Content);
                artCont[TypeIdPropertyName] = artefact.TypeId;
                return artCont;
            }
            if (is.IsSeq) {
                var seq = is.ToSeq();
                var objArr = new Array<Object>(seq.length);
                for (var i = 0; i < seq.length; i++) {
                    objArr[i] = InfoSet.Deserialize(seq[i]);
                }
                return objArr;
            }

            throw "Unknown type of InfoSet";
        }
    }
}
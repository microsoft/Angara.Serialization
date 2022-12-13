import { Angara as a } from "./Angara.InfoSet";

describe("Angara.InfoSet", () => {

    it("generates server-compatible JSON for primitives", () => {
        var c_json = a.InfoSet.Marshal(a.InfoSet.EmptyMap
            .AddInt("i", 20)
            .AddString("s", "Hello!")
            .AddBool("b", true)
            .AddDouble("d", 10.3)
            .AddDateTime("dt", new Date("2015-10-08"))
            .AddGuid("g", new a.Guid("11111111-2222-3333-4444-555555555555")));
        var s_json = {
            "i:int": 20,
            "s": "Hello!",
            "b": true,
            "d": 10.3,
            "dt:datetime": "2015-10-08T00:00:00.000Z",
            "g:guid": "11111111-2222-3333-4444-555555555555",
        };
        expect(JSON.stringify(c_json)).toEqual(JSON.stringify(s_json));
    });



    it("restores server-compatible JSON for arrays", () => {
        var da = [1e-12, 1e+20, 3.1415, 2.87];
        var ia = [20, 30];
        var c_json = a.InfoSet.Marshal(a.InfoSet.EmptyMap
            .AddIntArray("ia", ia)
            .AddBoolArray("ba", [true, false])
            .AddDoubleArray("da", da)
            .AddDateTimeArray("datea", [new Date("2015-10-08"), new Date("2014-08-15")])
            .AddStringArray("sa", ["hello", "world", "!"]));
        var s_json = {
            "ia:int array": "FAAAAB4AAAA=",
            "ba:bool array": "AQA=",
            "da:double array": "EeotgZmXcT1AjLV4Ha8VRG8Sg8DKIQlA9ihcj8L1BkA=",
            "datea:datetime array": "AAAA9UsEdUIAAMBrb310Qg==",//???
            "sa": ["hello", "world", "!"]
        };
        var infoSet = a.InfoSet.Unmarshal(s_json);
        expect(infoSet.IsMap).toBeTruthy();
        var m = infoSet.ToMap();
        var ia2 = m["ia"].ToIntArray();
        expect(ia2.length).toBe(ia.length);
        for (var i = 0; i < ia.length; i++)
            expect(ia2[i]).toEqual(ia[i]);
        var da2 = m["da"].ToDoubleArray();
        expect(da2.length).toBe(da.length);
        for (var i = 0; i < da.length; i++)
            expect(da2[i]).toEqual(da[i]);
    });

    it("restores numeric arrays with proper length", () => {
        var json = [
            {
                ":double array": "AAAAAAAAAAAe3YkrC9+RPyfc98lY3qE/DcnvSMfLqj8oUWptj9uxPwKBwrjWT7Y/a1c8m2DCuj/TYk9M1DK/PxmejZZs0ME/dVOoZwsGxD+Jcwt+GjrGP09idt1tbMg/9VhCrNmcyj90xs02MsvMPx255PJL984/kAaTwX2Q0D8pjjLdCqTRP8fVg883ttI/T+kvN+/G0z/czQDQG9bUP/ULinSo49U/PNPOH4Dv1j+BluXujfnXP6r/mCK9Adk/0BoGIfkH2j9TmDd3LQzbP8gFvtpFDtw/AN5EKy4O3T95UCR00gveP+2g7+0eB98/////////3z8T3/0gMXvgPyrdrD4Z9eA/HHdw165t4T8S/RGE6OThP3hJfPi8WuI/Xlp1BCPP4j+2vlWUEULjPznJvbF/s+M/vnpIhGQj5D8cFjxSt5HkP9RPOIFv/uQ/2AviloRp5T8rnIw57tLlPxBz4DCkOuY/zDt/Zp6g5j84TaXm1ATnP4JpyOA/Z+c/wb4zqNfH5z82HKG0lCboPzlQz6Jvg+g/"
            },
            {
                ":int array": "AAAAAAEAAAACAAAAAwAAAAQAAAAFAAAABgAAAAcAAAAIAAAACQAAAAoAAAALAAAADAAAAA0AAAAOAAAADwAAABAAAAARAAAAEgAAABMAAAAUAAAAFQAAABYAAAAXAAAAGAAAABkAAAAaAAAAGwAAABwAAAAdAAAAHgAAAB8AAAAgAAAAIQAAACIAAAAjAAAAJAAAACUAAAAmAAAAJwAAACgAAAApAAAAKgAAACsAAAAsAAAALQAAAC4AAAAvAAAAMAAAADEAAAAyAAAA"
            }
        ];
        var infoSet = a.InfoSet.Unmarshal(json);
        var seq = infoSet.ToSeq();
        var da = seq[0].ToDoubleArray();
        expect(da.length).toBe(51);
        var ia = seq[1].ToIntArray();
        expect(ia.length).toBe(51);
    })

    it("roundtrips integer values", () => {
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Int(10)));
        expect(infoSet.IsInt).toBeTruthy();
        expect(infoSet.ToInt()).toBe(10);
    });

    it("roundtrips double values", () => {
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Double(10.3)));
        expect(infoSet.IsDouble).toBeTruthy();
        expect(infoSet.ToDouble()).toBe(10.3);
    });

    it("roundtrips string values", () => {
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.String("Hello, Angara Web!")));
        expect(infoSet.IsString).toBeTruthy();
        expect(infoSet.ToString()).toBe("Hello, Angara Web!");
    });

    it("roundtrips raw values", () => {
        var s_json = {
            "i": 20,
            "s": "Hello!"
        };
        var infoset1 = a.InfoSet.Double(20);
        var infoset2 = a.InfoSet.String("Hello!");
        var c_json = a.InfoSet.Marshal(a.InfoSet.Raw(s_json));
        var infoSet = a.InfoSet.Unmarshal(c_json);

        expect(s_json).toBe(c_json);
        expect(infoSet.IsMap).toBeTruthy();
        expect(infoSet.ToMap()).toEqual({ "i": infoset1, "s": infoset2 });
    });

    it("roundtrips artefact values", () => {
        var content = a.InfoSet.Map({ "i": a.InfoSet.Int(10) });
        var artefact = a.InfoSet.Artefact("plot", content);
        var json = a.InfoSet.Marshal(artefact);
        var artefact2 = a.InfoSet.Unmarshal(json);
        expect(artefact2.IsArtefact).toBeTruthy();
        expect(artefact2.ToArtefact()).toEqual({ TypeId: "plot", Content: content });
    });

    it("roundtrips map values", () => {
        var infoSet1 = a.InfoSet.Int(10);
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Map({ "i": infoSet1 })));
        expect(infoSet.IsMap).toBeTruthy();
        expect(infoSet.ToMap()).toEqual({ "i": infoSet1 });
    });

    it("roundtrips boolean values", () => {
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Bool(true)));
        expect(infoSet.IsBool).toBeTruthy();
        expect(infoSet.ToBool()).toBe(true);
    });

    it("roundtrips Date values", () => {
        var d = new Date(2015, 10, 8, 16, 30, 45, 0);
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.DateTime(d)));
        expect(infoSet.IsDateTime).toBeTruthy();
        expect(infoSet.ToDateTime()).toEqual(d);
    });

    it("roundtrips bool arrays", () => {
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.BoolArray([true, false, true])));
        expect(infoSet.IsBoolArray).toBeTruthy();
        var arr = infoSet.ToBoolArray();
        expect(arr[0]).toBe(true);
        expect(arr[1]).toBe(false);
        expect(arr[2]).toBe(true);
    });

    it("roundtrips int arrays", () => {
        var ia = new Array<number>(51);
        for (var i = 0; i < 51; i++)
            ia[i] = i;
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.IntArray(ia)));
        expect(infoSet.IsIntArray).toBeTruthy();
        var arr = infoSet.ToIntArray();
        for (var i = 0; i < 51; i++)
            expect(arr[i]).toBe(ia[i]);
    });

    it("roundtrips double arrays", () => {
        var arr = [3.14, 2.87, 42];
        var json = a.InfoSet.Marshal(a.InfoSet.DoubleArray(arr));
        var infoSet = a.InfoSet.Unmarshal(json);
        expect(infoSet.IsDoubleArray).toBeTruthy();
        var arr2 = infoSet.ToDoubleArray();
        expect(arr2.length).toBe(arr.length);
        for (var i = 0; i < arr2.length; i++)
            expect(arr2[i]).toEqual(arr[i]);
    });

    it("roundtrips string arrays", () => {
        var sa = ["two", "one", "go"];
        var json = a.InfoSet.Marshal(a.InfoSet.StringArray(sa));
        var infoSet = a.InfoSet.Unmarshal(json);
        expect(infoSet.IsStringArray).toBeTruthy();
        var arr = infoSet.ToStringArray();
        expect(arr[0]).toBe("two");
        expect(arr[1]).toBe("one");
        expect(arr[2]).toBe("go");
        var es = new Array<string>(); // Empty string array
        var infoSet = a.InfoSet.StringArray(es);
        var json = a.InfoSet.Marshal(infoSet);
        var infoSet2 = a.InfoSet.Unmarshal(json);
        expect(infoSet2.IsStringArray).toBeTruthy();
        expect(infoSet2.ToStringArray()).toBe(es);

    });

    it("roundtrips DateTime arrays", () => {
        var da = [new Date(2015, 10, 8, 16, 30, 45, 0), new Date()];
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.DateTimeArray(da)));
        expect(infoSet.IsDateTimeArray).toBeTruthy();
        var arr = infoSet.ToDateTimeArray();
        expect(arr[0]).toEqual(da[0]);
        expect(arr[1]).toEqual(da[1]);
    });

    it("roundtrips Seq values", () => {
        var da = [a.InfoSet.String("hello"), a.InfoSet.BoolArray([true, false]), a.InfoSet.Int(10)];
        var json = a.InfoSet.Marshal(a.InfoSet.Seq(da));
        var infoSet = a.InfoSet.Unmarshal(json);
        expect(infoSet.IsSeq).toBeTruthy();
        var arr = infoSet.ToSeq();
        expect(arr[0]).toEqual(da[0]);
        expect(arr[1]).toEqual(da[1]);
        expect(arr[2]).toEqual(da[2]);
        da = [];
        var infoSet1 = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Seq(da)));
        expect(infoSet1.IsSeq).toBeTruthy();
        expect(infoSet1.ToSeq()).toEqual([]);
    });

    it("roundtrips Guid values", () => {
        var gs = a.Guid.NewGuid();
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Guid(gs)));
        expect(infoSet.IsGuid).toBeTruthy();
        expect(infoSet.ToGuid()).toEqual(gs);
    });

    it("roundtrips Null values", () => {
        var infoSet = a.InfoSet.Unmarshal(a.InfoSet.Marshal(a.InfoSet.Null()));
        var infoSet1 = a.InfoSet.Unmarshal(a.InfoSet.Marshal(null));
        expect(infoSet.IsNull).toBeTruthy();
        expect(infoSet1.IsNull).toBeTruthy();
        expect(infoSet.ToNull()).toBe(null);
        expect(infoSet1.ToNull()).toBe(null);

    });

    it("Unmarshal data", () => {
        var data = a.InfoSet.EmptyMap
            .AddInt("i", 20)
            .AddString("s", "Hello!")
            .AddBool("b", true);
        var unmarshalData = a.InfoSet.Unmarshal({ ":someData": a.InfoSet.Marshal(data) });
        expect(unmarshalData.IsArtefact).toBeTruthy();
        expect(unmarshalData.ToArtefact()).toEqual({ TypeId: "someData", Content: data });

        var unmarshalDatawithName = a.InfoSet.Unmarshal({ "s:someData": a.InfoSet.Marshal(data) });
        expect(unmarshalDatawithName.IsMap).toBeTruthy();
        expect(unmarshalDatawithName.ToMap()["s"].ToArtefact()).toEqual({ TypeId: "someData", Content: data });

        var data1 = a.InfoSet.EmptyMap
            .AddInt("i", 10)
            .AddString("s", "Bye!")
            .AddBool("b", false);
        var unmarshalData2 = a.InfoSet.Unmarshal({
            ":someData1": a.InfoSet.Marshal(data),
            "s:someData2": a.InfoSet.Marshal(data1)
        });
        expect(unmarshalData2.IsMap).toBeTruthy();
        expect(unmarshalData2.ToMap()[""].ToArtefact()).toEqual({ TypeId: "someData1", Content: data });
        expect(unmarshalData2.ToMap()["s"].ToArtefact()).toEqual({ TypeId: "someData2", Content: data1 });
    });

    it("Unmarshal object which name ends on 'array'", () => {
        var d = [a.InfoSet.String("t"), a.InfoSet.String("p")];
        var marshd = [a.InfoSet.Marshal(d[0]), a.InfoSet.Marshal(d[1])];
        var unmarshalData = a.InfoSet.Unmarshal({ ":some array": marshd });
        expect(unmarshalData.IsArtefact).toBeTruthy();
        expect(unmarshalData.ToArtefact().Content.ToSeq()).toEqual(d);
    });

}); 
import { Angara } from "./Angara.InfoSet";

describe("Angara.InfoSet", () => {

    it("parses and deserializes object arrays", () => {
        var json = {
            ":object array": [
                null,
                3.1415926535897931,
                {
                    ":record": {
                        "Age:int": 25,
                        "Dogs": [
                            "Alba",
                            "Eva"],
                        "Name": "Adam"
                    }
                }]
        }
        var infoSet = Angara.InfoSet.Unmarshal(json);
        var a = infoSet.ToArtefact();
        expect(a.TypeId).toBe("object array");
        var c = a.Content;
        var a2 = c.ToSeq();
        expect(a2[0].IsNull).toBeTruthy();
        expect(a2[1].ToDouble()).toBe(3.1415926535897931);
        expect(a2[2].ToArtefact().TypeId).toBe("record");
    });

    it("generates server-compatible JSON for arrays", () => {
        var c_json = Angara.InfoSet.Marshal(Angara.InfoSet.EmptyMap
            .AddIntArray("ia", [20, 30])
            .AddBoolArray("ba", [true, false])
            .AddDoubleArray("da", [1e-12, 1e+20, 3.1415, 2.87])
            .AddDateTimeArray("datea", [new Date("2015-10-08"), new Date("2014-08-15")])
            .AddStringArray("sa", ["hello", "world", "!"]));
        var s_json = {
            "ia:int array": "FAAAAB4AAAA=",
            "ba:bool array": "AQA=",
            "da:double array": "EeotgZmXcT1AjLV4Ha8VRG8Sg8DKIQlA9ihcj8L1BkA=",
            "datea:datetime array": "AAAA9UsEdUIAAMBrb310Qg==",//???
            "sa": ["hello", "world", "!"]
        };
        expect(JSON.stringify(c_json)).toEqual(JSON.stringify(s_json));
    });

    it("generates server-compatible JSON for seq ", () => {
        var da = Angara.InfoSet.Seq([Angara.InfoSet.Int(10), Angara.InfoSet.String("hello"), Angara.InfoSet.DoubleArray([10.3, 5])]);
        var c_json = Angara.InfoSet.Marshal(da);
        var s_json = [{ ":int": 10 },
            "hello",
        { ":double array": "mpmZmZmZJEAAAAAAAAAUQA==" }];
        expect(JSON.stringify(c_json)).toEqual(JSON.stringify(s_json));
    });

}); 
import { Record } from "../GuidGenerator.Client/src/fable_modules/fable-library.3.6.3/Types.js";
import { record_type, class_type } from "../GuidGenerator.Client/src/fable_modules/fable-library.3.6.3/Reflection.js";
import { guid, object } from "../GuidGenerator.Client/src/fable_modules/Thoth.Json.5.1.0/Encode.fs.js";
import { guid as guid_1, object as object_1 } from "../GuidGenerator.Client/src/fable_modules/Thoth.Json.5.1.0/Decode.fs.js";

export class PropertyInspectorSettings extends Record {
    constructor(LastGeneratedGuid) {
        super();
        this.LastGeneratedGuid = LastGeneratedGuid;
    }
}

export function PropertyInspectorSettings$reflection() {
    return record_type("GuidGenerator.SharedTypes.PropertyInspectorSettings", [], PropertyInspectorSettings, () => [["LastGeneratedGuid", class_type("System.Guid")]]);
}

export function PropertyInspectorSettings_Create_244AC511(g) {
    return new PropertyInspectorSettings(g);
}

export function PropertyInspectorSettings__Encode(this$) {
    return object([["lastGeneratedGuid", guid(this$.LastGeneratedGuid)]]);
}

export function PropertyInspectorSettings_get_Decoder() {
    return (path_1) => ((v) => object_1((get$) => (new PropertyInspectorSettings(get$.Required.Field("lastGeneratedGuid", (path, value) => guid_1(path, value)))), path_1, v));
}


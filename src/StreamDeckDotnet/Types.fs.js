import { some, value as value_16, defaultArg } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Option.js";
import { ofArray, singleton, empty } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/List.js";
import { StreamDeckDotnet_Logging_Types_ILog__ILog_trace_1302DC96, LogProvider_getLoggerByName } from "../../paket-files/TheAngryByrd/FsLibLog/src/FsLibLog/FsLibLog.fs.js";
import { replace, compare } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/String.js";
import { ResultCE_result, ResultCE_ResultBuilder__BindReturn_Z2499A5D, ResultCE_ResultBuilder__Delay_Z4709C901 } from "../../ExampleProject/Example.Client/src/.fable/FsToolkit.ErrorHandling.2.1.2/ResultCE.fs.js";
import { bool, uint32, int, string, object, fromString } from "../../ExampleProject/Example.Client/src/.fable/Thoth.Json.5.1.0/Decode.fs.js";
import { Union, toString, Record } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Types.js";
import { class_type, enum_type, union_type, bool_type, uint32_type, int32_type, record_type, option_type, string_type } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Reflection.js";
import { uncurry } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Util.js";
import { guid, toString as toString_1, object as object_1 } from "../../ExampleProject/Example.Client/src/.fable/Thoth.Json.5.1.0/Encode.fs.js";
import { empty as empty_1, singleton as singleton_1, append, delay, toList } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Seq.js";
import { Logger_Operators_op_GreaterGreaterBangPlus, Logger_Operators_op_BangBang, Logger_Operators_op_GreaterGreaterBangMinus } from "./Helpers.fs.js";

export function Encode_stringOption(so) {
    return defaultArg(so, "");
}

export function Encode_maybeEncode(n, o, f) {
    if (o == null) {
        return empty();
    }
    else {
        const x = value_16(o);
        return singleton([n, f(x)]);
    }
}

const Types_logger = LogProvider_getLoggerByName("StreamDeckDotnet.Types");

export function Types_$007CInvariantEqual$007C_$007C(str, arg) {
    if (compare(str, arg, 5) === 0) {
        return some(void 0);
    }
    else {
        return void 0;
    }
}

export function Types_tryDecodePayload(decoder, targetType, payload) {
    return ResultCE_ResultBuilder__Delay_Z4709C901(ResultCE_result, () => ResultCE_ResultBuilder__BindReturn_Z2499A5D(ResultCE_result, fromString(decoder, payload), (_arg1) => {
        const payload_1 = _arg1;
        return targetType(payload_1);
    }))();
}

export class Types_EventMetadata extends Record {
    constructor(Action, Event$, Context, Device, Payload) {
        super();
        this.Action = Action;
        this.Event = Event$;
        this.Context = Context;
        this.Device = Device;
        this.Payload = Payload;
    }
}

export function Types_EventMetadata$reflection() {
    return record_type("StreamDeckDotnet.Types.EventMetadata", [], Types_EventMetadata, () => [["Action", option_type(string_type)], ["Event", string_type], ["Context", option_type(string_type)], ["Device", option_type(string_type)], ["Payload", option_type(string_type)]]);
}

export function Types_EventMetadata_get_Decoder() {
    return (path_5) => ((v) => object((get$) => (new Types_EventMetadata(get$.Optional.Field("action", (path, value) => string(path, value)), get$.Required.Field("event", (path_1, value_1) => string(path_1, value_1)), get$.Optional.Field("context", (path_2, value_2) => string(path_2, value_2)), get$.Optional.Field("device", (path_3, value_3) => string(path_3, value_3)), get$.Optional.Field("payload", (path_4, value_4) => string(path_4, value_4)))), path_5, v));
}

export function Types_decodeEventMetadata(str) {
    return fromString(uncurry(2, Types_EventMetadata_get_Decoder()), str);
}

export function Types_encodeWithoutPayload(context, device, event) {
    return object_1(toList(delay(() => append((context != null) ? singleton_1(["context", value_16(context)]) : empty_1(), delay(() => append((device != null) ? singleton_1(["device", value_16(device)]) : empty_1(), delay(() => singleton_1(["event", event]))))))));
}

export function Types_encodeWithJson(context, device, event, json) {
    return object_1(toList((() => {
        throw 1;
    })()));
}

export function Types_encodeWithWrapper(context, device, event, payload) {
    return object_1(toList(delay(() => append((context != null) ? singleton_1(["context", value_16(context)]) : empty_1(), delay(() => append((device != null) ? singleton_1(["device", value_16(device)]) : empty_1(), delay(() => append(singleton_1(["event", event]), delay(() => singleton_1(["payload", replace(toString(object_1(payload)), "\n", "")]))))))))));
}

export function Types_Received_toJToken(s) {
    StreamDeckDotnet_Logging_Types_ILog__ILog_trace_1302DC96(Types_logger, Logger_Operators_op_GreaterGreaterBangMinus(Logger_Operators_op_BangBang("Parsing string \u0027{s}\u0027 into jtoken"), "s", s));
    const token = Newtonsoft_Json_Linq_JToken_Parse_Z721C83C5(s);
    StreamDeckDotnet_Logging_Types_ILog__ILog_trace_1302DC96(Types_logger, Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_BangBang("Token created is {t}"), "t", token));
    return token;
}

export class Types_Received_Coordinates extends Record {
    constructor(Column, Row) {
        super();
        this.Column = (Column | 0);
        this.Row = (Row | 0);
    }
}

export function Types_Received_Coordinates$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.Coordinates", [], Types_Received_Coordinates, () => [["Column", int32_type], ["Row", int32_type]]);
}

export function Types_Received_Coordinates_get_Decoder() {
    return (path) => ((v) => object((get$) => (new Types_Received_Coordinates(get$.Required.Field("column", uncurry(2, int)), get$.Required.Field("row", uncurry(2, int)))), path, v));
}

export function Types_Received_Coordinates__Encode(this$) {
    return ofArray([["column", this$.Column], ["row", this$.Row]]);
}

export class Types_Received_KeyPayload extends Record {
    constructor(Settings, Coordinates, State, UserDesiredState, IsInMultiAction) {
        super();
        this.Settings = Settings;
        this.Coordinates = Coordinates;
        this.State = State;
        this.UserDesiredState = UserDesiredState;
        this.IsInMultiAction = IsInMultiAction;
    }
}

export function Types_Received_KeyPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.KeyPayload", [], Types_Received_KeyPayload, () => [["Settings", null()], ["Coordinates", Types_Received_Coordinates$reflection()], ["State", uint32_type], ["UserDesiredState", uint32_type], ["IsInMultiAction", bool_type]]);
}

export function Types_Received_KeyPayload_get_Decoder() {
    return (path_2) => ((v) => object((get$) => (new Types_Received_KeyPayload(Types_Received_toJToken(get$.Required.Field("settings", (path, value) => string(path, value))), get$.Required.Field("coordinates", uncurry(2, Types_Received_Coordinates_get_Decoder())), get$.Required.Field("state", uncurry(2, uint32)), get$.Required.Field("userDesiredState", uncurry(2, uint32)), get$.Required.Field("isInMultiAction", (path_1, value_1) => bool(path_1, value_1)))), path_2, v));
}

export function Types_Received_KeyPayload__Encode(this$, context, device, actionName) {
    const payload = ofArray([["settings", toString(this$.Settings)], ["coordinates", object_1(Types_Received_Coordinates__Encode(this$.Coordinates))], ["state", this$.State], ["userDesiredState", this$.UserDesiredState], ["isInMultiAction", this$.IsInMultiAction]]);
    return Types_encodeWithWrapper(context, device, actionName, payload);
}

export class Types_Received_KeyPayloadDU extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["KeyDown", "KeyUp"];
    }
}

export function Types_Received_KeyPayloadDU$reflection() {
    return union_type("StreamDeckDotnet.Types.Received.KeyPayloadDU", [], Types_Received_KeyPayloadDU, () => [[["Item", Types_Received_KeyPayload$reflection()]], [["Item", Types_Received_KeyPayload$reflection()]]]);
}

export function Types_Received_KeyPayloadDU_get_Decoder() {
    return Types_Received_KeyPayload_get_Decoder();
}

export function Types_Received_KeyPayloadDU__Encode(this$, context, device) {
    if (this$.tag === 1) {
        const p_1 = this$.fields[0];
        return Types_Received_KeyPayload__Encode(p_1, context, device, "keyUp");
    }
    else {
        const p = this$.fields[0];
        return Types_Received_KeyPayload__Encode(p, context, device, "keyDown");
    }
}

export function Types_Received_KeyPayloadDU__get_Payload(this$) {
    if (this$.tag === 1) {
        const p_1 = this$.fields[0];
        return p_1;
    }
    else {
        const p = this$.fields[0];
        return p;
    }
}

export class Types_Received_SettingsPayload extends Record {
    constructor(Settings, Coordinates, IsInMultiAction) {
        super();
        this.Settings = Settings;
        this.Coordinates = Coordinates;
        this.IsInMultiAction = IsInMultiAction;
    }
}

export function Types_Received_SettingsPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.SettingsPayload", [], Types_Received_SettingsPayload, () => [["Settings", null()], ["Coordinates", Types_Received_Coordinates$reflection()], ["IsInMultiAction", bool_type]]);
}

export function Types_Received_SettingsPayload_get_Decoder() {
    return (path_2) => ((v) => object((get$) => (new Types_Received_SettingsPayload(Newtonsoft_Json_Linq_JObject_$ctor_4E60E31B(get$.Required.Field("settings", (path, value) => string(path, value))), get$.Required.Field("coordinates", uncurry(2, Types_Received_Coordinates_get_Decoder())), get$.Required.Field("isInMultiAction", (path_1, value_1) => bool(path_1, value_1)))), path_2, v));
}

export function Types_Received_SettingsPayload__Encode(this$, context, device) {
    const payload = ofArray([["settings", toString(this$.Settings)], ["coordinates", object_1(Types_Received_Coordinates__Encode(this$.Coordinates))], ["isInMultiAction", this$.IsInMultiAction]]);
    return Types_encodeWithWrapper(context, device, "didReceiveSettings", payload);
}

export class Types_Received_GlobalSettingsPayload extends Record {
    constructor(Settings) {
        super();
        this.Settings = Settings;
    }
}

export function Types_Received_GlobalSettingsPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.GlobalSettingsPayload", [], Types_Received_GlobalSettingsPayload, () => [["Settings", null()]]);
}

export function Types_Received_GlobalSettingsPayload_get_Decoder() {
    return (path_1) => ((v) => object((get$) => (new Types_Received_GlobalSettingsPayload(Newtonsoft_Json_Linq_JObject_$ctor_4E60E31B(get$.Required.Field("settings", (path, value) => string(path, value))))), path_1, v));
}

export function Types_Received_GlobalSettingsPayload__Encode(this$, context, device) {
    const payload = singleton(["settings", toString(this$.Settings)]);
    return Types_encodeWithWrapper(context, device, "didReceiveGlobalSettings", payload);
}

export class Types_Received_AppearPayload extends Record {
    constructor(Settings, Coordinates, State, IsInMultiAction) {
        super();
        this.Settings = Settings;
        this.Coordinates = Coordinates;
        this.State = (State | 0);
        this.IsInMultiAction = IsInMultiAction;
    }
}

export function Types_Received_AppearPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.AppearPayload", [], Types_Received_AppearPayload, () => [["Settings", null()], ["Coordinates", Types_Received_Coordinates$reflection()], ["State", int32_type], ["IsInMultiAction", bool_type]]);
}

export function Types_Received_AppearPayload_get_Decoder() {
    return (path_2) => ((v) => object((get$) => (new Types_Received_AppearPayload(Newtonsoft_Json_Linq_JObject_$ctor_4E60E31B(get$.Required.Field("settings", (path, value) => string(path, value))), get$.Required.Field("coordinates", uncurry(2, Types_Received_Coordinates_get_Decoder())), get$.Required.Field("state", uncurry(2, int)), get$.Required.Field("isInMultiAction", (path_1, value_1) => bool(path_1, value_1)))), path_2, v));
}

export function Types_Received_AppearPayload__Encode(this$, context, device) {
    const payload = ofArray([["settings", toString(this$.Settings)], ["coordinates", object_1(Types_Received_Coordinates__Encode(this$.Coordinates))], ["state", this$.State], ["isInMultiAction", this$.IsInMultiAction]]);
    return Types_encodeWithWrapper(context, device, "willAppear", payload);
}

export class Types_Received_TitleParameters extends Record {
    constructor(FontFamily, FontSize, FontStyle, FontUnderline, ShowTitle, TitleAlignment, TitleColor) {
        super();
        this.FontFamily = FontFamily;
        this.FontSize = (FontSize | 0);
        this.FontStyle = FontStyle;
        this.FontUnderline = FontUnderline;
        this.ShowTitle = ShowTitle;
        this.TitleAlignment = TitleAlignment;
        this.TitleColor = TitleColor;
    }
}

export function Types_Received_TitleParameters$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.TitleParameters", [], Types_Received_TitleParameters, () => [["FontFamily", option_type(string_type)], ["FontSize", int32_type], ["FontStyle", option_type(string_type)], ["FontUnderline", bool_type], ["ShowTitle", bool_type], ["TitleAlignment", option_type(string_type)], ["TitleColor", option_type(string_type)]]);
}

export function Types_Received_TitleParameters_get_Decoder() {
    return (path_6) => ((v) => object((get$) => (new Types_Received_TitleParameters(get$.Optional.Field("fontFamily", (path, value) => string(path, value)), get$.Required.Field("fontSize", uncurry(2, int)), get$.Optional.Field("fontStyle", (path_1, value_1) => string(path_1, value_1)), get$.Required.Field("fontUnderline", (path_2, value_2) => bool(path_2, value_2)), get$.Required.Field("showTitle", (path_3, value_3) => bool(path_3, value_3)), get$.Optional.Field("titleAlignment", (path_4, value_4) => string(path_4, value_4)), get$.Optional.Field("titleColor", (path_5, value_5) => string(path_5, value_5)))), path_6, v));
}

export function Types_Received_TitleParameters__Encode(this$) {
    return ofArray([["fontFamily", Encode_stringOption(this$.FontFamily)], ["fontSize", this$.FontSize], ["fontStyle", Encode_stringOption(this$.FontStyle)], ["fondUnderline", this$.FontUnderline], ["showTitle", this$.ShowTitle], ["TitleAlignment", Encode_stringOption(this$.TitleAlignment)], ["TitleColor", Encode_stringOption(this$.TitleColor)]]);
}

export class Types_Received_TitleParametersPayload extends Record {
    constructor(Coordinates, Settings, State, Title, TitleParameters) {
        super();
        this.Coordinates = Coordinates;
        this.Settings = Settings;
        this.State = (State | 0);
        this.Title = Title;
        this.TitleParameters = TitleParameters;
    }
}

export function Types_Received_TitleParametersPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.TitleParametersPayload", [], Types_Received_TitleParametersPayload, () => [["Coordinates", Types_Received_Coordinates$reflection()], ["Settings", null()], ["State", int32_type], ["Title", option_type(string_type)], ["TitleParameters", Types_Received_TitleParameters$reflection()]]);
}

export function Types_Received_TitleParametersPayload_get_Decoder() {
    return (path_2) => ((v) => object((get$) => (new Types_Received_TitleParametersPayload(get$.Required.Field("coordinates", uncurry(2, Types_Received_Coordinates_get_Decoder())), Newtonsoft_Json_Linq_JObject_$ctor_4E60E31B(get$.Required.Field("settings", (path, value) => string(path, value))), get$.Required.Field("state", uncurry(2, int)), get$.Optional.Field("title", (path_1, value_1) => string(path_1, value_1)), get$.Required.Field("titleParameters", uncurry(2, Types_Received_TitleParameters_get_Decoder())))), path_2, v));
}

export function Types_Received_TitleParametersPayload__Encode(this$, context, device) {
    const payload = ofArray([["coordinates", object_1(Types_Received_Coordinates__Encode(this$.Coordinates))], ["settings", toString(this$.Settings)], ["state", this$.State], ["title", Encode_stringOption(this$.Title)], ["titleParameters", object_1(Types_Received_TitleParameters__Encode(this$.TitleParameters))]]);
    return Types_encodeWithWrapper(context, device, "titleParametersDidChange", payload);
}

export class Types_Received_Size extends Record {
    constructor(Columns, Rows) {
        super();
        this.Columns = (Columns | 0);
        this.Rows = (Rows | 0);
    }
}

export function Types_Received_Size$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.Size", [], Types_Received_Size, () => [["Columns", int32_type], ["Rows", int32_type]]);
}

export function Types_Received_Size_get_Decoder() {
    return (path) => ((v) => object((get$) => (new Types_Received_Size(get$.Required.Field("columns", uncurry(2, int)), get$.Required.Field("rows", uncurry(2, int)))), path, v));
}

export function Types_Received_Size__Encode(this$) {
    return ofArray([["columns", this$.Columns], ["rows", this$.Rows]]);
}

export function Types_Received_DeviceTypeFromInt(v) {
    switch (v) {
        case 1: {
            return 1;
        }
        case 2: {
            return 2;
        }
        case 3: {
            return 3;
        }
        case 4: {
            return 4;
        }
        default: {
            return 0;
        }
    }
}

export function Types_Received_DeviceTypeToInt(v) {
    switch (v) {
        case 1: {
            return 1;
        }
        case 2: {
            return 2;
        }
        case 3: {
            return 3;
        }
        case 4: {
            return 4;
        }
        default: {
            return 0;
        }
    }
}

export class Types_Received_DeviceInfoPayload extends Record {
    constructor(Name, Type, Size) {
        super();
        this.Name = Name;
        this.Type = (Type | 0);
        this.Size = Size;
    }
}

export function Types_Received_DeviceInfoPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.DeviceInfoPayload", [], Types_Received_DeviceInfoPayload, () => [["Name", string_type], ["Type", enum_type("StreamDeckDotnet.Types.Received.DeviceType", int32_type, [["StreamDeck", 0], ["StreamDeckMini", 1], ["StreamDeckXL", 2], ["StreamDeckMobile", 3], ["CorsairGKeys", 4]])], ["Size", Types_Received_Size$reflection()]]);
}

export function Types_Received_DeviceInfoPayload_get_Decoder() {
    return (path_1) => ((v_1) => object((get$) => (new Types_Received_DeviceInfoPayload(get$.Required.Field("name", (path, value) => string(path, value)), Types_Received_DeviceTypeFromInt(get$.Required.Field("type", uncurry(2, int))), get$.Required.Field("size", uncurry(2, Types_Received_Size_get_Decoder())))), path_1, v_1));
}

export function Types_Received_DeviceInfoPayload__Encode(this$, context, device) {
    const payload = ofArray([["name", this$.Name], ["type", Types_Received_DeviceTypeToInt(this$.Type)], ["size", object_1(Types_Received_Size__Encode(this$.Size))]]);
    return Types_encodeWithWrapper(context, device, "deviceDidConnect", payload);
}

export class Types_Received_ApplicationPayload extends Record {
    constructor(Application) {
        super();
        this.Application = Application;
    }
}

export function Types_Received_ApplicationPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Received.ApplicationPayload", [], Types_Received_ApplicationPayload, () => [["Application", string_type]]);
}

export function Types_Received_ApplicationPayload_get_Decoder() {
    return (path_1) => ((v) => object((get$) => (new Types_Received_ApplicationPayload(get$.Required.Field("application", (path, value) => string(path, value)))), path_1, v));
}

export function Types_Received_ApplicationPayload__Encode(this$, context, device, eventName) {
    const payload = singleton(["application", this$.Application]);
    return Types_encodeWithWrapper(context, device, eventName, payload);
}

export class Types_Received_ApplicationPayloadDU extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["Launch", "Terminate"];
    }
}

export function Types_Received_ApplicationPayloadDU$reflection() {
    return union_type("StreamDeckDotnet.Types.Received.ApplicationPayloadDU", [], Types_Received_ApplicationPayloadDU, () => [[["payload", Types_Received_ApplicationPayload$reflection()]], [["payload", Types_Received_ApplicationPayload$reflection()]]]);
}

export function Types_Received_ApplicationPayloadDU_get_Decoder() {
    return Types_Received_ApplicationPayload_get_Decoder();
}

export function Types_Received_ApplicationPayloadDU__Encode(this$, context, device) {
    if (this$.tag === 1) {
        const p_1 = this$.fields[0];
        return Types_Received_ApplicationPayload__Encode(p_1, context, device, "applicationDidTerminate");
    }
    else {
        const p = this$.fields[0];
        return Types_Received_ApplicationPayload__Encode(p, context, device, "applicationDidLaunch");
    }
}

export function Types_Received_ApplicationPayloadDU__get_Payload(this$) {
    if (this$.tag === 1) {
        const p_1 = this$.fields[0];
        return p_1;
    }
    else {
        const p = this$.fields[0];
        return p;
    }
}

export class Types_Received_EventReceived extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["KeyDown", "KeyUp", "DidReceiveSettings", "DidReceiveGlobalSettings", "WillAppear", "WillDisappear", "TitleParametersDidChange", "DeviceDidConnect", "DeviceDidDisconnect", "ApplicationDidLaunch", "ApplicationDidTerminate", "SystemDidWakeUp", "PropertyInspectorDidAppear", "PropertyInspectorDidDisappear", "SendToPlugin", "SendToPropertyInspector"];
    }
}

export function Types_Received_EventReceived$reflection() {
    return union_type("StreamDeckDotnet.Types.Received.EventReceived", [], Types_Received_EventReceived, () => [[["payload", Types_Received_KeyPayloadDU$reflection()]], [["payload", Types_Received_KeyPayloadDU$reflection()]], [["payload", Types_Received_SettingsPayload$reflection()]], [["payload", Types_Received_GlobalSettingsPayload$reflection()]], [["payload", Types_Received_AppearPayload$reflection()]], [["payload", Types_Received_AppearPayload$reflection()]], [["payload", Types_Received_TitleParametersPayload$reflection()]], [["payload", Types_Received_DeviceInfoPayload$reflection()]], [], [["payload", Types_Received_ApplicationPayloadDU$reflection()]], [["payload", Types_Received_ApplicationPayloadDU$reflection()]], [], [], [], [["payload", null()]], [["payload", null()]]]);
}

export function Types_Received_EventReceived__GetName(this$) {
    switch (this$.tag) {
        case 1: {
            return "keyUp";
        }
        case 2: {
            return "didReceiveSettings";
        }
        case 3: {
            return "didReceiveGlobalSettings";
        }
        case 4: {
            return "willAppear";
        }
        case 5: {
            return "willDisappear";
        }
        case 6: {
            return "titleParametersDidChange";
        }
        case 7: {
            return "deviceDidConnect";
        }
        case 8: {
            return "deviceDidDisconnect";
        }
        case 9: {
            return "applicationDidLaunch";
        }
        case 10: {
            return "applicationDidTerminate";
        }
        case 11: {
            return "systemDidWakeUp";
        }
        case 12: {
            return "propertyInspectorDidAppear";
        }
        case 13: {
            return "propertyInspectorDidDisappear";
        }
        case 14: {
            return "sendToPlugin";
        }
        case 15: {
            return "sendToPropertyInspector";
        }
        default: {
            return "keyDown";
        }
    }
}

export function Types_Received_EventReceived__Encode(this$, context, device) {
    switch (this$.tag) {
        case 1: {
            const payload_1 = this$.fields[0];
            return toString_1(0, Types_Received_KeyPayloadDU__Encode(payload_1, context, device));
        }
        case 2: {
            const payload_2 = this$.fields[0];
            return toString_1(0, Types_Received_SettingsPayload__Encode(payload_2, context, device));
        }
        case 3: {
            const payload_3 = this$.fields[0];
            return toString_1(0, Types_Received_GlobalSettingsPayload__Encode(payload_3, context, device));
        }
        case 4: {
            const payload_4 = this$.fields[0];
            return toString_1(0, Types_Received_AppearPayload__Encode(payload_4, context, device));
        }
        case 5: {
            const payload_5 = this$.fields[0];
            return toString_1(0, Types_Received_AppearPayload__Encode(payload_5, context, device));
        }
        case 6: {
            const payload_6 = this$.fields[0];
            return toString_1(0, Types_Received_TitleParametersPayload__Encode(payload_6, context, device));
        }
        case 7: {
            const payload_7 = this$.fields[0];
            return toString_1(0, Types_Received_DeviceInfoPayload__Encode(payload_7, context, device));
        }
        case 8: {
            return toString_1(0, Types_encodeWithoutPayload(context, device, "deviceDidDisconnect"));
        }
        case 9: {
            const payload_8 = this$.fields[0];
            return toString_1(0, Types_Received_ApplicationPayloadDU__Encode(payload_8, context, device));
        }
        case 10: {
            const payload_9 = this$.fields[0];
            return toString_1(0, Types_Received_ApplicationPayloadDU__Encode(payload_9, context, device));
        }
        case 11: {
            return toString_1(0, Types_encodeWithoutPayload(context, device, "deviceDidDisconnect"));
        }
        case 12: {
            return toString_1(0, Types_encodeWithoutPayload(context, device, "deviceDidDisconnect"));
        }
        case 13: {
            return toString_1(0, Types_encodeWithoutPayload(context, device, "deviceDidDisconnect"));
        }
        case 14: {
            const payload_10 = this$.fields[0];
            return toString_1(0, Types_encodeWithJson(context, device, "sendToPlugin", payload_10));
        }
        case 15: {
            const payload_11 = this$.fields[0];
            return toString_1(0, Types_encodeWithJson(context, device, "sendToPropertyInspector", payload_11));
        }
        default: {
            const payload = this$.fields[0];
            return toString_1(0, Types_Received_KeyPayloadDU__Encode(payload, context, device));
        }
    }
}

function Types_Sent_TargetToInt(t) {
    switch (t) {
        case 0: {
            return 0;
        }
        case 1: {
            return 1;
        }
        case 2: {
            return 2;
        }
        default: {
            return 0;
        }
    }
}

function Types_Sent_TargetFromInt(v) {
    switch (v) {
        case 1: {
            return 1;
        }
        case 2: {
            return 2;
        }
        default: {
            return 0;
        }
    }
}

export class Types_Sent_LogMessagePayload extends Record {
    constructor(Message) {
        super();
        this.Message = Message;
    }
}

export function Types_Sent_LogMessagePayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.LogMessagePayload", [], Types_Sent_LogMessagePayload, () => [["Message", string_type]]);
}

export function Types_Sent_LogMessagePayload__Encode(this$, context, device) {
    const payload = singleton(["message", this$.Message]);
    return Types_encodeWithWrapper(context, device, "logMessage", payload);
}

export function Types_Sent_LogMessagePayload_get_Decoder() {
    return (path_1) => ((v) => object((get$) => (new Types_Sent_LogMessagePayload(get$.Required.Field("message", (path, value) => string(path, value)))), path_1, v));
}

export class Types_Sent_RegisterPluginPayload extends Record {
    constructor(Event$, PluginGuid) {
        super();
        this.Event = Event$;
        this.PluginGuid = PluginGuid;
    }
}

export function Types_Sent_RegisterPluginPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.RegisterPluginPayload", [], Types_Sent_RegisterPluginPayload, () => [["Event", string_type], ["PluginGuid", class_type("System.Guid")]]);
}

export function Types_Sent_RegisterPluginPayload__Encode(this$) {
    return object_1([["event", this$.Event], ["uuid", guid(this$.PluginGuid)]]);
}

export function Types_Sent_RegisterPluginPayload_Create(event, id) {
    return new Types_Sent_RegisterPluginPayload(event, id);
}

export class Types_Sent_OpenUrlPayload extends Record {
    constructor(Url) {
        super();
        this.Url = Url;
    }
}

export function Types_Sent_OpenUrlPayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.OpenUrlPayload", [], Types_Sent_OpenUrlPayload, () => [["Url", string_type]]);
}

export function Types_Sent_OpenUrlPayload__Encode(this$, context, device) {
    return Types_encodeWithWrapper(context, device, "openUrl", [["url", this$.Url]]);
}

export function Types_Sent_OpenUrlPayload_get_Decoder() {
    return (path_1) => ((v) => object((get$) => (new Types_Sent_OpenUrlPayload(get$.Required.Field("url", (path, value) => string(path, value)))), path_1, v));
}

export class Types_Sent_SetTitlePayload extends Record {
    constructor(Title, Target, State) {
        super();
        this.Title = Title;
        this.Target = (Target | 0);
        this.State = State;
    }
}

export function Types_Sent_SetTitlePayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.SetTitlePayload", [], Types_Sent_SetTitlePayload, () => [["Title", option_type(string_type)], ["Target", enum_type("StreamDeckDotnet.Types.Sent.Target", int32_type, [["HardwareAndSoftware", 0], ["Hardware", 1], ["Software", 2]])], ["State", option_type(int32_type)]]);
}

export function Types_Sent_SetTitlePayload__Encode(this$, context, device) {
    return Types_encodeWithWrapper(context, device, "setTitle", toList(delay(() => append(Encode_maybeEncode("title", this$.Title, (value) => value), delay(() => append(singleton_1(["target", Types_Sent_TargetToInt(this$.Target)]), delay(() => Encode_maybeEncode("state", this$.State, (value_3) => value_3))))))));
}

export function Types_Sent_SetTitlePayload_get_Decoder() {
    return (path_1) => ((v_1) => object((get$) => (new Types_Sent_SetTitlePayload(get$.Optional.Field("title", (path, value) => string(path, value)), Types_Sent_TargetFromInt(get$.Required.Field("target", uncurry(2, int))), get$.Optional.Field("state", uncurry(2, int)))), path_1, v_1));
}

export class Types_Sent_SetImagePayload extends Record {
    constructor(Image, Target, State) {
        super();
        this.Image = Image;
        this.Target = (Target | 0);
        this.State = State;
    }
}

export function Types_Sent_SetImagePayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.SetImagePayload", [], Types_Sent_SetImagePayload, () => [["Image", string_type], ["Target", enum_type("StreamDeckDotnet.Types.Sent.Target", int32_type, [["HardwareAndSoftware", 0], ["Hardware", 1], ["Software", 2]])], ["State", option_type(int32_type)]]);
}

export function Types_Sent_SetImagePayload__Encode(this$, context, device) {
    return Types_encodeWithWrapper(context, device, "setImage", toList(delay(() => append(singleton_1(["image", this$.Image]), delay(() => append(singleton_1(["target", Types_Sent_TargetToInt(this$.Target)]), delay(() => Encode_maybeEncode("state", this$.State, (value_2) => value_2))))))));
}

export function Types_Sent_SetImagePayload__get_Decoder(this$) {
    return (path_1) => ((v_1) => object((get$) => (new Types_Sent_SetImagePayload(get$.Required.Field("image", (path, value) => string(path, value)), Types_Sent_TargetFromInt(get$.Required.Field("target", uncurry(2, int))), get$.Optional.Field("state", uncurry(2, int)))), path_1, v_1));
}

export class Types_Sent_SetStatePayload extends Record {
    constructor(State) {
        super();
        this.State = (State | 0);
    }
}

export function Types_Sent_SetStatePayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.SetStatePayload", [], Types_Sent_SetStatePayload, () => [["State", int32_type]]);
}

export function Types_Sent_SetStatePayload__Encode(this$, context, device) {
    return Types_encodeWithWrapper(context, device, "setState", [["state", this$.State]]);
}

export function Types_Sent_SetStatePayload_get_Decoder() {
    return (path) => ((v) => object((get$) => (new Types_Sent_SetStatePayload(get$.Required.Field("state", uncurry(2, int)))), path, v));
}

export class Types_Sent_SwitchToProfilePayload extends Record {
    constructor(Profile) {
        super();
        this.Profile = Profile;
    }
}

export function Types_Sent_SwitchToProfilePayload$reflection() {
    return record_type("StreamDeckDotnet.Types.Sent.SwitchToProfilePayload", [], Types_Sent_SwitchToProfilePayload, () => [["Profile", string_type]]);
}

export function Types_Sent_SwitchToProfilePayload__Encode(this$, context, device) {
    return Types_encodeWithWrapper(context, device, "switchToProfile", [["profile", this$.Profile]]);
}

export function Types_Sent_SwitchToProfilePayload_get_Decoder() {
    return (path_1) => ((v) => object((get$) => (new Types_Sent_SwitchToProfilePayload(get$.Required.Field("profile", (path, value) => string(path, value)))), path_1, v));
}

export class Types_Sent_EventSent extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["RegisterPlugin", "LogMessage", "SetSettings", "GetSettings", "SetGlobalSettings", "GetGlobalSettings", "OpenUrl", "SetTitle", "SetImage", "ShowAlert", "ShowOk", "SetState", "SwitchToProfile", "SendToPropertyInspector"];
    }
}

export function Types_Sent_EventSent$reflection() {
    return union_type("StreamDeckDotnet.Types.Sent.EventSent", [], Types_Sent_EventSent, () => [[["payload", Types_Sent_RegisterPluginPayload$reflection()]], [["Item", Types_Sent_LogMessagePayload$reflection()]], [["payload", null()]], [], [["payload", null()]], [], [["payload", Types_Sent_OpenUrlPayload$reflection()]], [["paylod", Types_Sent_SetTitlePayload$reflection()]], [["payload", Types_Sent_SetImagePayload$reflection()]], [], [], [["payload", Types_Sent_SetStatePayload$reflection()]], [["payload", Types_Sent_SwitchToProfilePayload$reflection()]], [["payload", null()]]]);
}

export function Types_Sent_EventSent__Encode(this$, context, device) {
    const encode = (x) => toString_1(0, x);
    switch (this$.tag) {
        case 1: {
            const payload_1 = this$.fields[0];
            return encode(Types_Sent_LogMessagePayload__Encode(payload_1, context, device));
        }
        case 2: {
            const payload_2 = this$.fields[0];
            return encode(Types_encodeWithWrapper(context, device, "setSettings", [["payload", (() => {
                throw 1;
            })()]]));
        }
        case 3: {
            return encode(Types_encodeWithWrapper(context, device, "getSettings", []));
        }
        case 4: {
            const payload_3 = this$.fields[0];
            return encode(Types_encodeWithWrapper(context, device, "setGlobalSettings", [["payload", (() => {
                throw 1;
            })()]]));
        }
        case 5: {
            return encode(Types_encodeWithWrapper(context, device, "getGlobalSettings", []));
        }
        case 6: {
            const payload_4 = this$.fields[0];
            return encode(Types_Sent_OpenUrlPayload__Encode(payload_4, context, device));
        }
        case 7: {
            const payload_5 = this$.fields[0];
            return encode(Types_Sent_SetTitlePayload__Encode(payload_5, context, device));
        }
        case 8: {
            const payload_6 = this$.fields[0];
            return encode(Types_Sent_SetImagePayload__Encode(payload_6, context, device));
        }
        case 9: {
            return encode(Types_encodeWithWrapper(context, device, "showAlert", []));
        }
        case 10: {
            return encode(Types_encodeWithWrapper(context, device, "showOk", []));
        }
        case 11: {
            const payload_7 = this$.fields[0];
            return encode(Types_Sent_SetStatePayload__Encode(payload_7, context, device));
        }
        case 12: {
            const payload_8 = this$.fields[0];
            return encode(Types_Sent_SwitchToProfilePayload__Encode(payload_8, context, device));
        }
        case 13: {
            const payload_9 = this$.fields[0];
            return encode(Types_encodeWithWrapper(context, device, "sendToPropertyInspector", [["payload", (() => {
                throw 1;
            })()]]));
        }
        default: {
            const payload = this$.fields[0];
            return encode(Types_Sent_RegisterPluginPayload__Encode(payload));
        }
    }
}

export function Types_createLogEvent(msg) {
    const payload = new Types_Sent_LogMessagePayload(msg);
    return new Types_Sent_EventSent(1, payload);
}

export function Types_createOkEvent() {
    return new Types_Sent_EventSent(10);
}

export function Types_createAlertEvent() {
    return new Types_Sent_EventSent(9);
}


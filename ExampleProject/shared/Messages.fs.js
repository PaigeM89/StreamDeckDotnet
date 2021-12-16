import { Record, Union } from "../Example.Client/src/.fable/fable-library.3.1.15/Types.js";
import { record_type, class_type, union_type, string_type } from "../Example.Client/src/.fable/fable-library.3.1.15/Reflection.js";

export class ServerMessage extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["HelloWorld"];
    }
}

export function ServerMessage$reflection() {
    return union_type("Example.Shared.ServerMessage", [], ServerMessage, () => [[["Item", string_type]]]);
}

export class ClientMessage extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["HelloFromClient"];
    }
}

export function ClientMessage$reflection() {
    return union_type("Example.Shared.ClientMessage", [], ClientMessage, () => [[["Item", string_type]]]);
}

export class Types_PropertyInspectorRegisterEvent extends Record {
    constructor(Event$, UUID) {
        super();
        this.Event = Event$;
        this.UUID = UUID;
    }
}

export function Types_PropertyInspectorRegisterEvent$reflection() {
    return record_type("Example.Shared.Types.PropertyInspectorRegisterEvent", [], Types_PropertyInspectorRegisterEvent, () => [["Event", string_type], ["UUID", class_type("System.Guid")]]);
}

export function Types_PropertyInspectorRegisterEvent_Default() {
    return new Types_PropertyInspectorRegisterEvent("registerPropertyInspector", "00000000-0000-0000-0000-000000000000");
}

export function Types_PropertyInspectorRegisterEvent_Create_244AC511(uuid) {
    return new Types_PropertyInspectorRegisterEvent(Types_PropertyInspectorRegisterEvent_Default().Event, uuid);
}

export function Types_PropertyInspectorRegisterEvent__Encode(this$) {
    throw 1;
}

export class Types_ClientSendEvent extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["PiRegisterEvent"];
    }
}

export function Types_ClientSendEvent$reflection() {
    return union_type("Example.Shared.Types.ClientSendEvent", [], Types_ClientSendEvent, () => [[["pire", Types_PropertyInspectorRegisterEvent$reflection()]]]);
}

export function Types_ClientSendEvent__Encode(this$) {
    let payload;
    const e = this$.fields[0];
    payload = Types_PropertyInspectorRegisterEvent__Encode(e);
    throw 1;
}


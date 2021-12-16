import { toText, printf, toConsole } from "./.fable/fable-library.3.1.15/String.js";
import { toString } from "./.fable/fable-library.3.1.15/Types.js";
import { fromString } from "./.fable/Thoth.Json.5.1.0/Decode.fs.js";
import { createAtom, uncurry } from "./.fable/fable-library.3.1.15/Util.js";
import { PropertyInspectorSettings_get_Decoder } from "../../GuidGenerator.Shared/SharedTypes.fs.js";
import { singleton } from "./.fable/fable-library.3.1.15/AsyncBuilder.js";
import { choose, SEND_TO_PROPERTY_INSPECTOR, op_GreaterEqualsGreater, log, addLogToContext } from "../../../src/StreamDeckDotnet/Core.fs.js";
import { EventBinders_tryBindSendToPropertyInspectorEvent } from "../../../src/StreamDeckDotnet/Routing.fs.js";
import { singleton as singleton_1 } from "./.fable/fable-library.3.1.15/List.js";
import { socketMsgHandler } from "../../../src/StreamDeckDotnet.Fable/Client.fs.js";
import { Websocket_$ctor_Z6ACAEAE2 } from "./Websocket.fs.js";

export function updateLastGeneratedGuid(g) {
    const ele = document.getElementById("last-generated-guid-output");
    toConsole(printf("element is not an html input element"));
}

export function decipherPayload(payload) {
    let piSettingsResult;
    const value = toString(payload);
    piSettingsResult = fromString(uncurry(2, PropertyInspectorSettings_get_Decoder()), value);
    if (piSettingsResult.tag === 1) {
        const e = piSettingsResult.fields[0];
        toConsole(printf("Error decoding: %A"))(e);
    }
    else {
        const settings = piSettingsResult.fields[0];
        updateLastGeneratedGuid(settings.LastGeneratedGuid);
    }
}

export function sendToPIHandler(payload, next, ctx) {
    return singleton.Delay(() => {
        const msg = toText(printf("In PI sendToPIHandler, payload is %A"))(payload);
        toConsole(printf("msg in send to pI handler is %A"))(msg);
        decipherPayload(payload);
        const ctx$0027 = addLogToContext(msg + ", 2nd log line", ctx);
        return singleton.ReturnFrom(next(ctx$0027));
    });
}

export function errorHandler(err) {
    const msg = toText(printf("In PI error handler, err is : %A"))(err);
    return (next) => ((ctx) => log(msg, next, ctx));
}

export const eventPipeline = (() => {
    const handlers = singleton_1(op_GreaterEqualsGreater(uncurry(2, op_GreaterEqualsGreater(uncurry(2, SEND_TO_PROPERTY_INSPECTOR))((next) => ((ctx) => log("in PI handler", next, ctx)))))(EventBinders_tryBindSendToPropertyInspectorEvent(uncurry(3, (err) => errorHandler(err)), (payload, next_1, ctx_1) => sendToPIHandler(payload, next_1, ctx_1))));
    return (next_2) => choose(handlers, next_2);
})();

export let websocket = createAtom(void 0);

export function messageHandler(msg) {
    return socketMsgHandler(uncurry(2, eventPipeline), msg);
}

export function connectStreamDeck(inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo) {
    toConsole(printf("Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"))(inPort)(inPropertyInspectorUUID)(inRegisterEvent)(inInfo)(inActionInfo);
    const ws = Websocket_$ctor_Z6ACAEAE2(inPort, inPropertyInspectorUUID, (msg) => messageHandler(msg));
    websocket(ws, true);
}


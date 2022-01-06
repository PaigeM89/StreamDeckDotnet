import { fromValue } from "./fable_modules/Thoth.Json.5.1.0/Decode.fs.js";
import { createAtom, uncurry } from "./fable_modules/fable-library.3.6.3/Util.js";
import { PropertyInspectorSettings_get_Decoder } from "../../GuidGenerator.Shared/SharedTypes.fs.js";
import { toText, printf, toConsole } from "./fable_modules/fable-library.3.6.3/String.js";
import { singleton } from "./fable_modules/fable-library.3.6.3/AsyncBuilder.js";
import { choose, SEND_TO_PROPERTY_INSPECTOR, compose, log } from "../../../src/StreamDeckDotnet.Library/Core.fs.js";
import { EventBinders_tryBindSendToPropertyInspectorEvent } from "../../../src/StreamDeckDotnet.Library/Routing.fs.js";
import { singleton as singleton_1 } from "./fable_modules/fable-library.3.6.3/List.js";
import { socketMsgHandler } from "../../../src/StreamDeckDotnet.Fable/Client.fs.js";
import { Websocket_$ctor_Z6ACAEAE2 } from "./Websocket.fs.js";

export function updateLastGeneratedGuid(g) {
    const ele = document.getElementById("last-generated-guid-output");
    ele.value = g;
}

export function decipherPayload(payload) {
    const piSettingsResult = fromValue("$", uncurry(2, PropertyInspectorSettings_get_Decoder()), payload);
    if (piSettingsResult.tag === 1) {
        toConsole(printf("Error decoding: %A"))(piSettingsResult.fields[0]);
    }
    else {
        const settings = piSettingsResult.fields[0];
        toConsole(printf("Got custom property inspector settings: %A"))(settings);
        updateLastGeneratedGuid(settings.LastGeneratedGuid);
    }
}

export function sendToPIHandler(payload, next, ctx) {
    return singleton.Delay(() => {
        toConsole(printf("in send to PI handler"));
        decipherPayload(payload);
        return singleton.ReturnFrom(next(ctx));
    });
}

export function errorHandler(err) {
    toConsole(printf("In PI error handler, err is %A"))(err);
    const msg = toText(printf("In PI error handler, err is : %A"))(err);
    return (next) => ((ctx) => log(msg, next, ctx));
}

export const eventPipeline = (() => {
    let action2;
    const handlers = singleton_1((action2 = EventBinders_tryBindSendToPropertyInspectorEvent(uncurry(3, (err) => errorHandler(err)), (payload, next, ctx) => sendToPIHandler(payload, next, ctx)), (final) => compose(uncurry(2, SEND_TO_PROPERTY_INSPECTOR), uncurry(2, action2), final)));
    return (next_1) => choose(handlers, next_1);
})();

export let websocket = createAtom(void 0);

export function messageHandler(msg) {
    return socketMsgHandler(uncurry(2, eventPipeline), msg);
}

export function connectStreamDeck(inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo) {
    const ws = Websocket_$ctor_Z6ACAEAE2(inPort, inPropertyInspectorUUID, (msg) => messageHandler(msg));
    websocket(ws, true);
}


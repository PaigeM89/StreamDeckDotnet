import { toConsole, printf, toText } from "./fable_modules/fable-library.3.6.3/String.js";
import { Convert_serialize } from "./fable_modules/Fable.SimpleJson.3.21.0/Json.Converter.fs.js";
import { createTypeInfo } from "./fable_modules/Fable.SimpleJson.3.21.0/TypeInfo.Converter.fs.js";
import { anonRecord_type, class_type, string_type } from "./fable_modules/fable-library.3.6.3/Reflection.js";
import { singleton } from "./fable_modules/fable-library.3.6.3/AsyncBuilder.js";
import { reverse, cons, empty, iterate } from "./fable_modules/fable-library.3.6.3/List.js";
import { EventContext__GetEncodedEventsToSend } from "../../../src/StreamDeckDotnet.Library/Context.fs.js";
import { toString, FSharpRef } from "./fable_modules/fable-library.3.6.3/Types.js";
import { startAsPromise } from "./fable_modules/fable-library.3.6.3/Async.js";

export function getWebsocketServerUrl(port) {
    return toText(printf("ws://127.0.0.1:%i"))(port);
}

export function getRegisterWebsocket(uuid) {
    return Convert_serialize({
        event: "registerPropertyInspector",
        uuid: uuid,
    }, createTypeInfo(anonRecord_type(["event", string_type], ["uuid", class_type("System.Guid")])));
}

export function handleMessage(messageHandler, responseHandler, input) {
    return singleton.Delay(() => singleton.Bind(messageHandler(input), (_arg1) => {
        const ctxResponse = _arg1;
        if (ctxResponse.tag === 1) {
            return singleton.Return();
        }
        else {
            iterate(responseHandler, EventContext__GetEncodedEventsToSend(ctxResponse.fields[0]));
            return singleton.Zero();
        }
    }));
}

export class Websocket {
    constructor(port, uuid, messageHandler) {
        this.port = (port | 0);
        this.uuid = uuid;
        this.messageHandler = messageHandler;
        this.msgQueue = empty();
        this.wsref = (new FSharpRef(void 0));
        Websocket__createWebsocket(this);
    }
}

export function Websocket$reflection() {
    return class_type("GuidGenerator.Websockets.Websocket", void 0, Websocket);
}

export function Websocket_$ctor_Z6ACAEAE2(port, uuid, messageHandler) {
    return new Websocket(port, uuid, messageHandler);
}

export function Websocket__IsOpen(this$) {
    const matchValue = this$.wsref.contents;
    if (matchValue == null) {
        return false;
    }
    else {
        return matchValue.readyState === 1;
    }
}

export function Websocket__SendToSocket_Z721C83C5(this$, payload) {
    if (Websocket__IsOpen(this$)) {
        const matchValue = this$.wsref.contents;
        if (matchValue == null) {
        }
        else {
            const ws = matchValue;
            const payload_1 = Convert_serialize(payload, createTypeInfo(string_type));
            toConsole(printf("websocket sending \"%s\""))(payload_1);
            ws.send(payload_1);
        }
    }
    else {
        const payload_2 = Convert_serialize(payload, createTypeInfo(string_type));
        this$.msgQueue = cons(payload_2, this$.msgQueue);
    }
}

function Websocket__createWebsocket(this$) {
    const connect = (timeout, server) => {
        const matchValue = this$.wsref.contents;
        if (matchValue == null) {
            const socket = new WebSocket(server);
            this$.wsref.contents = socket;
            socket.onerror = ((e) => {
                toConsole(printf("Socket had error: %A"))(e);
            });
            socket.onopen = ((e_1) => {
                const arg10_1 = toString(e_1.currentTarget);
                toConsole(printf("Socket was opened, on open being called! Event Target: %A"))(arg10_1);
                const registerPayload = getRegisterWebsocket(this$.uuid);
                this$.msgQueue = cons(registerPayload, this$.msgQueue);
                toConsole(printf("Sending registration message"));
                iterate((arg00) => {
                    socket.send(arg00);
                }, reverse(this$.msgQueue));
            });
            socket.onclose = ((_arg1) => {
                toConsole(printf("Socket was closed!"));
                window.setTimeout(() => {
                    connect(timeout, server);
                }, timeout, void 0);
            });
            socket.onmessage = ((e_2) => {
                const msg = toString(e_2.data);
                toConsole(printf("raw message from socket is %s"))(msg);
                const pr = startAsPromise(handleMessage(this$.messageHandler, (arg00_1) => {
                    socket.send(arg00_1);
                }, msg));
                pr.then();
            });
        }
    };
    connect(60000, getWebsocketServerUrl(this$.port));
}


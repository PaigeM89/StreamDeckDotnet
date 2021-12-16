import { toConsole, printf, toText } from "./.fable/fable-library.3.1.15/String.js";
import { Convert_fromJson, Fable_SimpleJson_Json__Json_stringify_Static_4E60E31B } from "./.fable/Fable.SimpleJson.3.21.0/Json.Converter.fs.js";
import { reverse, iterate, cons, empty } from "./.fable/fable-library.3.1.15/List.js";
import { toString, FSharpRef } from "./.fable/fable-library.3.1.15/Types.js";
import { string_type, class_type } from "./.fable/fable-library.3.1.15/Reflection.js";
import { FSharpResult$2 } from "./.fable/fable-library.3.1.15/Choice.js";
import { SimpleJson_parseNative } from "./.fable/Fable.SimpleJson.3.21.0/SimpleJson.fs.js";
import { createTypeInfo } from "./.fable/Fable.SimpleJson.3.21.0/TypeInfo.Converter.fs.js";
import { startAsPromise } from "./.fable/fable-library.3.1.15/Async.js";
import { EventContext__GetEncodedEventsToSend } from "../../../src/StreamDeckDotnet/Context.fs.js";

export function getWebsocketServerUrl(port) {
    return toText(printf("ws://127.0.0.1:%i"))(port);
}

export function getRegisterWebsocket(uuid) {
    const json = {
        event: "registerPropertyInspector",
        uuid: uuid,
    };
    return Fable_SimpleJson_Json__Json_stringify_Static_4E60E31B(json);
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
    toConsole(printf("in IsOpen func"));
    const matchValue = this$.wsref.contents;
    if (matchValue == null) {
        return false;
    }
    else {
        const ws = matchValue;
        return ws.readyState === 1;
    }
}

export function Websocket__SendToSocket_Z721C83C5(this$, payload) {
    if (Websocket__IsOpen(this$)) {
        const matchValue = this$.wsref.contents;
        if (matchValue == null) {
        }
        else {
            const ws = matchValue;
            const payload_1 = Fable_SimpleJson_Json__Json_stringify_Static_4E60E31B(payload);
            toConsole(printf("websocket sending \"%s\""))(payload_1);
            ws.send(payload_1);
        }
    }
    else {
        const payload_2 = Fable_SimpleJson_Json__Json_stringify_Static_4E60E31B(payload);
        this$.msgQueue = cons(payload_2, this$.msgQueue);
    }
}

function Websocket__createWebsocket(this$) {
    const connect = (timeout, server) => {
        toConsole(printf("attempting to connect web socket to %s with timeout %i..."))(server)(timeout);
        const matchValue = this$.wsref.contents;
        if (matchValue == null) {
            const socket = new WebSocket(server);
            this$.wsref.contents = socket;
            socket.onerror = ((_arg1) => {
                toConsole(printf("Socket had error!"));
            });
            socket.onopen = ((e) => {
                const arg10_1 = e.currentTarget;
                toConsole(printf("Socket was opened, on open being called! Event Target: %A"))(arg10_1);
                const registerPayload = getRegisterWebsocket(this$.uuid);
                this$.msgQueue = cons(registerPayload, this$.msgQueue);
                const arg10_2 = this$.msgQueue;
                toConsole(printf("MsgQueue is %A"))(arg10_2);
                iterate((arg00) => {
                    socket.send(arg00);
                }, reverse(this$.msgQueue));
            });
            socket.onclose = ((_arg2) => {
                toConsole(printf("Socket was closed!"));
                void window.setTimeout(() => {
                    connect(timeout, server);
                }, timeout, void 0);
            });
            socket.onmessage = ((e_1) => {
                let inputJson, typeInfo;
                toConsole(printf("socket.onmessage was called"));
                let _arg1_1;
                const input = toString(e_1.data);
                try {
                    _arg1_1 = (new FSharpResult$2(0, (inputJson = SimpleJson_parseNative(input), (typeInfo = createTypeInfo(string_type), Convert_fromJson(inputJson, typeInfo)))));
                }
                catch (ex) {
                    _arg1_1 = (new FSharpResult$2(1, ex.message));
                }
                if (_arg1_1.tag === 0) {
                    const msg = _arg1_1.fields[0];
                    toConsole(printf("websocket msg is %A"))(msg);
                    let pr_1;
                    const pr = startAsPromise(this$.messageHandler(msg));
                    pr_1 = (pr.then(((ctx) => {
                        if (ctx.tag === 1) {
                            const e_2 = ctx.fields[0];
                            toConsole(printf("Error handling message: %A"))(e_2);
                        }
                        else {
                            const ctx_1 = ctx.fields[0];
                            iterate((arg00_2) => {
                                socket.send(arg00_2);
                            }, EventContext__GetEncodedEventsToSend(ctx_1));
                        }
                    })));
                    pr_1.then();
                }
                else {
                    toConsole(printf("could not parse message %A"))(e_1);
                }
            });
        }
    };
    connect(60000, getWebsocketServerUrl(this$.port));
    toConsole(printf("Websocket finished connect(), returning out of constructor"));
    const matchValue_1 = this$.wsref.contents;
    if (matchValue_1 == null) {
        toConsole(printf("socket is none"));
    }
    else {
        const ws = matchValue_1;
        const arg10_6 = ws.readyState | 0;
        toConsole(printf("Socket state is %A"))(arg10_6);
    }
}


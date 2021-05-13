import { toConsole, printf, toText } from "./.fable/fable-library.3.1.15/String.js";
import { cons, empty } from "./.fable/fable-library.3.1.15/List.js";
import { FSharpRef } from "./.fable/fable-library.3.1.15/Types.js";
import { class_type } from "./.fable/fable-library.3.1.15/Reflection.js";
import { some, value } from "./.fable/fable-library.3.1.15/Option.js";
import { equals } from "./.fable/fable-library.3.1.15/Util.js";

export function getBaseUrl(port) {
    const url = (() => {
        throw 1;
    })()(window.location.href);
    throw 1;
    throw 1;
    return url;
}

export function getWebsocketServerUrl(port) {
    return toText(printf("ws://localhost:%i"))(port);
}

export class Websocket {
    constructor(port, uuid, messageHandler) {
        this.port = (port | 0);
        this.msgQueue = empty();
        this.wsref = (new FSharpRef(void 0));
        Websocket__createWebsocket(this);
    }
}

export function Websocket$reflection() {
    return class_type("Example.React.Client.Websockets.Websocket", void 0, Websocket);
}

export function Websocket_$ctor_29A09D7C(port, uuid, messageHandler) {
    return new Websocket(port, uuid, messageHandler);
}

export function Websocket__IsOpen(this$) {
    toConsole(printf("in IsOpen func"));
    const matchValue = this$.wsref.contents;
    if (matchValue == null) {
        return false;
    }
    else {
        const ws = value(matchValue);
        return equals((() => {
            throw 1;
        })(), (() => {
            throw 1;
        })());
    }
}

export function Websocket__SendToSocket_Z721C83C5(this$, payload) {
    if (Websocket__IsOpen(this$)) {
        const matchValue = this$.wsref.contents;
        if (matchValue == null) {
        }
        else {
            const ws = value(matchValue);
            let payload_1;
            throw 1;
            toConsole(printf("websocket sending \"%s\""))(payload_1);
            throw 1;
        }
    }
    else {
        let payload_2;
        throw 1;
        this$.msgQueue = cons(payload_2, this$.msgQueue);
    }
}

function Websocket__createWebsocket(this$) {
    const connect = (timeout, server) => {
        toConsole(printf("attempting to connect web socket to %s with timeout %i..."))(server)(timeout);
        const matchValue = this$.wsref.contents;
        if (matchValue == null) {
            let socket;
            throw 1;
            this$.wsref.contents = some(socket);
            throw 1;
            throw 1;
            throw 1;
            throw 1;
        }
    };
    connect(60000, getWebsocketServerUrl(this$.port));
    toConsole(printf("Websocket finished connect(), returning out of constructor"));
    const matchValue_1 = this$.wsref.contents;
    if (matchValue_1 == null) {
        toConsole(printf("socket is none"));
    }
    else {
        const ws = value(matchValue_1);
        let arg10_1;
        throw 1;
        toConsole(printf("Socket state is %A"))(arg10_1);
    }
}


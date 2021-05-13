import { createElement } from "react";
import { toString } from "./.fable/fable-library.3.1.15/Types.js";
import { useFeliz_React__React_useState_Static_1505, React_functionComponent_2F9D7239 } from "./.fable/Feliz.1.43.0/React.fs.js";
import { Websocket_$ctor_29A09D7C } from "./Websocket.fs.js";
import { Interop_reactApi } from "./.fable/Feliz.1.43.0/Interop.fs.js";
import { int32ToString } from "./.fable/fable-library.3.1.15/Util.js";
import { toConsole, printf, toText } from "./.fable/fable-library.3.1.15/String.js";
import { render } from "react-dom";

export const sdpiItem = "sdpi-item";

export const msgClass = "message";

export function htmlPItem(content) {
    return createElement("p", {
        className: sdpiItem,
        children: toString(content),
    });
}

export const Counter = React_functionComponent_2F9D7239((tupledArg) => {
    const port = tupledArg[0] | 0;
    const uuid = tupledArg[1];
    const event = tupledArg[2];
    const info = tupledArg[3];
    const actionInfo = tupledArg[4];
    const patternInput = useFeliz_React__React_useState_Static_1505(0);
    const setCount = patternInput[1];
    const count = patternInput[0] | 0;
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(void 0);
    const socket = patternInput_1[0];
    const setSocket = patternInput_1[1];
    const patternInput_2 = useFeliz_React__React_useState_Static_1505("");
    const setLastMsg = patternInput_2[1];
    const lastMsg = patternInput_2[0];
    const connectSocket = () => {
        const websocket = Websocket_$ctor_29A09D7C(port, uuid, setLastMsg);
        setSocket(websocket);
    };
    throw 1;
    return createElement("div", {
        className: sdpiItem,
        children: Interop_reactApi.Children.toArray([createElement("button", {
            className: sdpiItem,
            style: {
                marginRight: 5,
            },
            onClick: (_arg1) => {
                setCount(count + 1);
            },
            children: "Increment",
        }), createElement("button", {
            className: sdpiItem,
            style: {
                marginLeft: 5,
            },
            onClick: (_arg2) => {
                setCount(count - 1);
            },
            children: "Decrement",
        }), createElement("h1", {
            className: sdpiItem,
            children: int32ToString(count),
        }), htmlPItem(toText(printf("Port is %i"))(port)), htmlPItem(toText(printf("UUID is %A"))(uuid)), htmlPItem(toText(printf("event is %s"))(event)), htmlPItem(toText(printf("info is %s"))(info)), htmlPItem(toText(printf("actionInfo is %s"))(actionInfo))]),
    });
});

export function connectStreamDeck(inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo) {
    toConsole(printf("Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"))(inPort)(inPropertyInspectorUUID)(inRegisterEvent)(inInfo)(inActionInfo);
    const element = Counter([inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo]);
    return render(element, document.getElementById("root"));
}


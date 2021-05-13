import { printf, toFail, interpolate, toText } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/String.js";
import { validateEvent, validateAction, tryBindEvent, log } from "./Core.fs.js";
import { partialApply, uncurry } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Util.js";
import { Types_Sent_EventSent__Encode, Types_Sent_EventSent, Types_Sent_RegisterPluginPayload_Create, Types_decodeEventMetadata, Types_Received_ApplicationPayloadDU__get_Payload, Types_Received_EventReceived__GetName, Types_Received_KeyPayloadDU__get_Payload } from "./Types.fs.js";
import { EventContext__get_EventName, EventContext_$ctor_401617BA, PipelineFailure } from "./Context.fs.js";
import { StreamDeckDotnet_Logging_Types_ILog__ILog_error_1302DC96, StreamDeckDotnet_Logging_Types_ILog__ILog_warn_1302DC96, StreamDeckDotnet_Logging_Types_ILog__ILog_trace_1302DC96, StreamDeckDotnet_Logging_Types_ILog__ILog_info_1302DC96, LogProvider_getLoggerByName } from "../../paket-files/TheAngryByrd/FsLibLog/src/FsLibLog/FsLibLog.fs.js";
import { AsyncResultCE_asyncResult, AsyncResultCE_AsyncResultBuilder__Return_1505, AsyncResultCE_AsyncResultBuilder__Delay_Z64727ECD } from "../../ExampleProject/Example.Client/src/.fable/FsToolkit.ErrorHandling.2.1.2/AsyncResultCE.fs.js";
import { Logger_Operators_op_GreaterGreaterBangPlus, Logger_Operators_op_BangBang, Logger_Operators_op_GreaterGreaterBangMinus } from "./Helpers.fs.js";
import { singleton } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/AsyncBuilder.js";
import { FSharpResult$2 } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Choice.js";
import { retn } from "../../ExampleProject/Example.Client/src/.fable/FsToolkit.ErrorHandling.2.1.2/AsyncOption.fs.js";
import { StreamDeckConnection__Run, StreamDeckConnection_$ctor_365C1FAD } from "./Websockets.fs.js";
import { class_type } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Reflection.js";

function Routing_PayloadRouting_multiAction() {
    return true;
}

function Routing_appState(stateCheck, next, ctx) {
    return next(ctx);
}

export function Routing_matcher(matchFunc) {
    const logErrorHandler = (err) => {
        const msg = toText(interpolate("Error handling event: %P()", [err]));
        return (next) => ((ctx) => log(msg, next, ctx));
    };
    return (next_1) => ((ctx_1) => tryBindEvent(uncurry(3, logErrorHandler), uncurry(3, partialApply(3, matchFunc, [ctx_1])), next_1, ctx_1));
}

export function Routing_eventMatch(eventName, next, ctx) {
    const validate = (t) => validateAction(eventName, t);
    return validateEvent(validate, next, ctx);
}

export function Routing_EventBinders_tryBindKeyDownEvent(errorHandler, successHandler, next, ctx) {
    const filter = (e) => {
        if (e.tag === 0) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [Types_Received_KeyPayloadDU__get_Payload(payload)]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    return tryBindEvent(errorHandler, uncurry(3, filter), next, ctx);
}

export function Routing_EventBinders_tryBindKeyUpEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 1) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [Types_Received_KeyPayloadDU__get_Payload(payload)]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindDidReceiveSettingsEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 2) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindDidReceiveGlobalSettingsEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 3) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_trybindWillAppearEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 4) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindWillDisappearEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 5) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindTitleParametersDidChangeEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 6) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindDeviceDidConnectEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 7) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindDeviceDidDisconnectEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 8) {
            return partialApply(2, successHandler, [void 0]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindApplicationDidLaunchEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 9) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [Types_Received_ApplicationPayloadDU__get_Payload(payload)]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindApplicationDidTerminateEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 10) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [Types_Received_ApplicationPayloadDU__get_Payload(payload)]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindSystemDidWakeUpEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 4) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindPropertyInspectorDidAppearEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 12) {
            return partialApply(2, successHandler, [void 0]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindPropertyInspectorDidDisappearEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 13) {
            return partialApply(2, successHandler, [void 0]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindSendToPluginEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 14) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

export function Routing_EventBinders_tryBindSendToPropertyInspectorEvent(errorHandler, successHandler) {
    const filter = (e) => {
        if (e.tag === 15) {
            const payload = e.fields[0];
            return partialApply(2, successHandler, [payload]);
        }
        else {
            return partialApply(2, errorHandler, [new PipelineFailure(4, Types_Received_EventReceived__GetName(e), "keyDown")]);
        }
    };
    const errHandler = errorHandler;
    return (next) => ((ctx) => {
        const filter_1 = (e_1) => filter(e_1);
        return tryBindEvent(errHandler, uncurry(3, filter_1), next, ctx);
    });
}

const Client_logger = LogProvider_getLoggerByName("StreamDeckDotnet.Client");

export function Client_socketMsgHandler(routes, msg) {
    return AsyncResultCE_AsyncResultBuilder__Delay_Z64727ECD(AsyncResultCE_asyncResult, () => {
        StreamDeckDotnet_Logging_Types_ILog__ILog_info_1302DC96(Client_logger, Logger_Operators_op_GreaterGreaterBangMinus(Logger_Operators_op_BangBang("Decoding event metadata from msg \u0027{msg}\u0027"), "msg", msg));
        const asyncResult_1 = singleton.Return(Types_decodeEventMetadata(msg));
        return singleton.Delay(() => singleton.Bind(asyncResult_1, (_arg1_2) => {
            let eventMetadata, ctx, initHandler, asyncResult;
            const result_2 = _arg1_2;
            if (result_2.tag === 1) {
                const x_7 = result_2.fields[0];
                return singleton.Return(new FSharpResult$2(1, x_7));
            }
            else {
                const x_6 = result_2.fields[0];
                return singleton.ReturnFrom((eventMetadata = x_6, (StreamDeckDotnet_Logging_Types_ILog__ILog_info_1302DC96(Client_logger, Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_BangBang("Building context from metadata object {meta}"), "meta", eventMetadata)), (ctx = EventContext_$ctor_401617BA(eventMetadata), (initHandler = ((ctx_1) => retn(ctx_1)), (StreamDeckDotnet_Logging_Types_ILog__ILog_trace_1302DC96(Client_logger, Logger_Operators_op_BangBang("Beginning Event Handling")), (asyncResult = singleton.Bind(routes(initHandler, ctx), (arg) => singleton.Return(new FSharpResult$2(0, arg))), singleton.Delay(() => singleton.Bind(asyncResult, (_arg1_1) => {
                    let _arg2, ctx_2;
                    const result_1 = _arg1_1;
                    if (result_1.tag === 1) {
                        const x_5 = result_1.fields[0];
                        return singleton.Return(new FSharpResult$2(1, x_5));
                    }
                    else {
                        const x_4 = result_1.fields[0];
                        return singleton.ReturnFrom((_arg2 = x_4, (_arg2 == null) ? (StreamDeckDotnet_Logging_Types_ILog__ILog_warn_1302DC96(Client_logger, Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_BangBang("No route found to handle event {eventName}"), "eventName", EventContext__get_EventName(ctx))), AsyncResultCE_AsyncResultBuilder__Return_1505(AsyncResultCE_asyncResult, ctx)) : (ctx_2 = _arg2, (StreamDeckDotnet_Logging_Types_ILog__ILog_trace_1302DC96(Client_logger, Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_BangBang("Event {name} was successfully handled"), "name", EventContext__get_EventName(ctx_2))), AsyncResultCE_AsyncResultBuilder__Return_1505(AsyncResultCE_asyncResult, ctx_2)))));
                    }
                })))))))));
            }
        }));
    });
}

function Client_forceMessageHandling(routes, msg) {
    return singleton.Delay(() => singleton.Bind(Client_socketMsgHandler(routes, msg), (_arg1) => {
        const r = _arg1;
        if (r.tag === 1) {
            const e = r.fields[0];
            StreamDeckDotnet_Logging_Types_ILog__ILog_error_1302DC96(Client_logger, Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_BangBang("Received Error {e} when handling msg {msg}"), "e", e), "msg", msg));
            return singleton.Return(toFail(printf("%A"))(e));
        }
        else {
            const x = r.fields[0];
            return singleton.Return(x);
        }
    }));
}

function Client_handleSocketRegister(args, unitVar1) {
    const register = Types_Sent_RegisterPluginPayload_Create(args.RegisterEvent, args.PluginUUID);
    StreamDeckDotnet_Logging_Types_ILog__ILog_info_1302DC96(Client_logger, Logger_Operators_op_GreaterGreaterBangPlus(Logger_Operators_op_BangBang("Creating registration event of {event}"), "event", register));
    const sendEvent = new Types_Sent_EventSent(0, register);
    return Types_Sent_EventSent__Encode(sendEvent, void 0, void 0);
}

export class Client_StreamDeckClient {
    constructor(args, handler) {
        const msgHandler = (msg) => Client_forceMessageHandling(handler, msg);
        const registerHandler = () => Client_handleSocketRegister(args, void 0);
        this._socket = StreamDeckConnection_$ctor_365C1FAD(args, msgHandler, registerHandler);
    }
}

export function Client_StreamDeckClient$reflection() {
    return class_type("StreamDeckDotnet.Client.StreamDeckClient", void 0, Client_StreamDeckClient);
}

export function Client_StreamDeckClient_$ctor_5F5C2057(args, handler) {
    return new Client_StreamDeckClient(args, handler);
}

export function Client_StreamDeckClient__Run(_) {
    StreamDeckConnection__Run(_._socket);
}


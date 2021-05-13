import { Async_lift } from "./Helpers.fs.js";
import { mapCurriedArgs, partialApply } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Util.js";
import { singleton } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/AsyncBuilder.js";
import { map, head, tail as tail_1, isEmpty } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/List.js";
import { lift, EventContext__AddAlert, EventContext__AddOk, addSendEvent, EventContext__get_EventMetadata, EventContext__get_TryBindEventAsync } from "./Context.fs.js";
import { Types_createLogEvent } from "./Types.fs.js";
import { interpolate, toText } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/String.js";

export const skipPipeline = Async_lift(void 0);

export const earlyReturn = (arg) => Async_lift(arg);

export function compose(action1, action2, final) {
    return partialApply(1, action1, [partialApply(1, action2, [final])]);
}

export const op_GreaterEqualsGreater = (action1) => ((action2) => ((final) => compose(action1, action2, final)));

function chooseEventFunc(funcs, ctx) {
    return singleton.Delay(() => {
        if (!isEmpty(funcs)) {
            const tail = tail_1(funcs);
            const func = head(funcs);
            return singleton.Bind(func(ctx), (_arg1) => {
                const result = _arg1;
                if (result == null) {
                    return singleton.ReturnFrom(chooseEventFunc(tail, ctx));
                }
                else {
                    const c = result;
                    return singleton.Return(c);
                }
            });
        }
        else {
            return singleton.Return(void 0);
        }
    });
}

export function choose(handlers, next) {
    const funcs = map(mapCurriedArgs((h) => partialApply(1, h, [next]), [[0, 2]]), handlers);
    return (ctx) => chooseEventFunc(funcs, ctx);
}

export function tryBindEvent(errorHandler, successHandler, next, ctx) {
    return singleton.Delay(() => singleton.Bind(EventContext__get_TryBindEventAsync(ctx), (_arg1) => {
        const result = _arg1;
        if (result.tag === 1) {
            const err = result.fields[0];
            return singleton.ReturnFrom(errorHandler(err, next, ctx));
        }
        else {
            const event = result.fields[0];
            return singleton.ReturnFrom(successHandler(event, next, ctx));
        }
    }));
}

export function validateEvent(validate, next, ctx) {
    const x = EventContext__get_EventMetadata(ctx).Event;
    if (validate(x)) {
        return next(ctx);
    }
    else {
        return skipPipeline;
    }
}

export function validateAction(s, t) {
    return s.toLowerCase() === t.toLowerCase();
}

export const KEY_DOWN = (next) => ((ctx) => validateEvent((t) => validateAction("keyDown", t), next, ctx));

export const KEY_UP = (next) => ((ctx) => validateEvent((t) => validateAction("keyUp", t), next, ctx));

export const DID_RECEIVE_SETTINGS = (next) => ((ctx) => validateEvent((t) => validateAction("didReceiveSettings", t), next, ctx));

export const DID_RECEIVE_GLOBAL_SETTINGS = (next) => ((ctx) => validateEvent((t) => validateAction("didReceiveGlobalSettings", t), next, ctx));

export const WILL_APPEAR = (next) => ((ctx) => validateEvent((t) => validateAction("willAppear", t), next, ctx));

export const WILL_DISAPPEAR = (next) => ((ctx) => validateEvent((t) => validateAction("willDisappear", t), next, ctx));

export const TITLE_PARAMETERS_DID_CHANGE = (next) => ((ctx) => validateEvent((t) => validateAction("titleParametersDidChange", t), next, ctx));

export const DEVICE_DID_CONNECT = (next) => ((ctx) => validateEvent((t) => validateAction("deviceDidConnect", t), next, ctx));

export const DEVICE_DID_DISCONNECT = (next) => ((ctx) => validateEvent((t) => validateAction("deviceDidDisconnect", t), next, ctx));

export const APPLICATION_DID_LAUNCH = (next) => ((ctx) => validateEvent((t) => validateAction("applicationDidLaunch", t), next, ctx));

export const APPLICATION_DID_TERMINATE = (next) => ((ctx) => validateEvent((t) => validateAction("applicationDidTerminate", t), next, ctx));

export const SYSTEM_DID_WAKE_UP = (next) => ((ctx) => validateEvent((t) => validateAction("systemDidWakeUp", t), next, ctx));

export const PROPERTY_INSPECTOR_DID_APPEAR = (next) => ((ctx) => validateEvent((t) => validateAction("propertyInspectorDidAppear", t), next, ctx));

export const PROPERTY_INSPECTOR_DID_DISAPPEAR = (next) => ((ctx) => validateEvent((t) => validateAction("propertyInspectorDidDisappear", t), next, ctx));

export const SEND_TO_PLUGIN = (next) => ((ctx) => validateEvent((t) => validateAction("sendToPlugin", t), next, ctx));

export const SEND_TO_PROPERTY_INSPECTOR = (next) => ((ctx) => validateEvent((t) => validateAction("sendToPropertyInspector", t), next, ctx));

export function addLogToContext(msg, ctx) {
    const log_1 = Types_createLogEvent(msg);
    return addSendEvent(log_1, ctx);
}

export function addShowOk(ctx) {
    EventContext__AddOk(ctx);
    return ctx;
}

export function addShowAlert(ctx) {
    EventContext__AddAlert(ctx);
    return ctx;
}

export function log(msg, next, ctx) {
    return next(addLogToContext(msg, ctx));
}

export function logWithContext(msg, next, ctx) {
    const msg_1 = msg + toText(interpolate("Event Metadata: %P()", [EventContext__get_EventMetadata(ctx)]));
    return next(addLogToContext(msg_1, ctx));
}

export function showOk(next, ctx) {
    return next(addShowOk(ctx));
}

export function showAlert(next, ctx) {
    return next(addShowAlert(ctx));
}

export function flow(_arg1, ctx) {
    return lift(ctx);
}


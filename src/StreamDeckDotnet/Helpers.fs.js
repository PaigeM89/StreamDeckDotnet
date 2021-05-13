import { singleton } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/AsyncBuilder.js";
import { catchAsync } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Async.js";
import { Types_LogModule_addException, Types_LogModule_addExn, Types_LogModule_addContextDestructured, Types_LogModule_addContext, Types_LogModule_addParameter, Types_LogModule_setMessage } from "../../paket-files/TheAngryByrd/FsLibLog/src/FsLibLog/FsLibLog.fs.js";

export function Task_map(projection, task) {
    const r = null;
    return null;
}

export function Exception_Reraise(ex) {
    throw ex;
    return null;
}

export function Async_lift(a$0027) {
    return singleton.Delay(() => singleton.Return(a$0027));
}

export function Async_tryFinally(finalize, body) {
    return singleton.Delay(() => singleton.Bind(catchAsync(body), (_arg1) => {
        const result = _arg1;
        return singleton.Bind(finalize, () => {
            let exn, value;
            return singleton.Return((result.tag === 1) ? (exn = result.fields[0], (() => {
                throw exn;
            })()) : (value = result.fields[0], value));
        });
    }));
}

export function Logger_message(m) {
    return (log) => Types_LogModule_setMessage(m, log);
}

export function Logger_withParam(v) {
    return (log) => Types_LogModule_addParameter(v, log);
}

export function Logger_withValue(k, v) {
    return (log) => Types_LogModule_addContext(k, v, log);
}

export function Logger_withObject(k, v) {
    return (log) => Types_LogModule_addContextDestructured(k, v, log);
}

export function Logger_withExn(e) {
    return (log) => Types_LogModule_addExn(e, log);
}

export function Logger_withException(e) {
    return (log) => Types_LogModule_addException(e, log);
}

export function Logger_Operators_op_BangBang(m) {
    return Logger_message(m);
}

export function Logger_Operators_op_GreaterGreaterBang(log, v) {
    return (arg) => Logger_withParam(v)(log(arg));
}

export function Logger_Operators_op_GreaterGreaterBangMinus(log, k, v) {
    return (arg) => Logger_withValue(k, v)(log(arg));
}

export function Logger_Operators_op_GreaterGreaterBangPlus(log, k, v) {
    return (arg) => Logger_withObject(k, v)(log(arg));
}

export function Logger_Operators_op_GreaterGreaterBangBang(log, e) {
    return (arg) => Logger_withException(e)(log(arg));
}


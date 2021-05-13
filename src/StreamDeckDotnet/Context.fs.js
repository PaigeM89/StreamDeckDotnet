import { StreamDeckDotnet_Logging_Types_ILog__ILog_debug_1302DC96, LogProvider_getLoggerByName } from "../../paket-files/TheAngryByrd/FsLibLog/src/FsLibLog/FsLibLog.fs.js";
import { FSharpRef, Union } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Types.js";
import { class_type, option_type, lambda_type, union_type, string_type } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Reflection.js";
import { ResultCE_result, ResultCE_ResultBuilder__Return_1505, ResultCE_ResultBuilder__Delay_Z4709C901 } from "../../ExampleProject/Example.Client/src/.fable/FsToolkit.ErrorHandling.2.1.2/ResultCE.fs.js";
import { FSharpResult$2 } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Choice.js";
import { Types_Sent_EventSent__Encode, Types_createAlertEvent, Types_createOkEvent, Types_createLogEvent, Types_Received_ApplicationPayloadDU, Types_Received_DeviceInfoPayload_get_Decoder, Types_Received_TitleParametersPayload_get_Decoder, Types_Received_AppearPayload_get_Decoder, Types_Received_GlobalSettingsPayload_get_Decoder, Types_Received_SettingsPayload_get_Decoder, Types_Received_EventReceived, Types_Received_KeyPayloadDU, Types_$007CInvariantEqual$007C_$007C, Types_Received_ApplicationPayloadDU_get_Decoder, Types_tryDecodePayload, Types_Received_KeyPayloadDU_get_Decoder, Types_Received_EventReceived$reflection } from "./Types.fs.js";
import { AsyncResultCE_asyncResult, AsyncResultCE_AsyncResultBuilder__Delay_Z64727ECD } from "../../ExampleProject/Example.Client/src/.fable/FsToolkit.ErrorHandling.2.1.2/AsyncResultCE.fs.js";
import { uncurry } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Util.js";
import { singleton } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/AsyncBuilder.js";
import { map, ofArray, empty, singleton as singleton_1, cons } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/List.js";
import { Async_lift, Logger_Operators_op_BangBang, Logger_Operators_op_GreaterGreaterBangMinus } from "./Helpers.fs.js";
import { tryParse } from "../../ExampleProject/Example.Client/src/.fable/fable-library.3.1.15/Guid.js";

const logger = LogProvider_getLoggerByName("StreamDeckDotnet.Context");

export class PipelineFailure extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["DecodeFailure", "UnknownEventType", "NoPayloadForType", "PayloadMissing", "WrongEvent"];
    }
}

export function PipelineFailure$reflection() {
    return union_type("StreamDeckDotnet.Context.PipelineFailure", [], PipelineFailure, () => [[["input", string_type], ["errorMsg", string_type]], [["eventName", string_type]], [["eventName", string_type]], [], [["encounteredEvent", string_type], ["expectedEvent", string_type]]]);
}

export function tryDecode(input, decodeFunc) {
    return ResultCE_ResultBuilder__Delay_Z4709C901(ResultCE_result, () => {
        const matchValue = decodeFunc(input);
        if (matchValue.tag === 1) {
            const msg = matchValue.fields[0];
            return new FSharpResult$2(1, new PipelineFailure(0, input, msg));
        }
        else {
            const x = matchValue.fields[0];
            return ResultCE_ResultBuilder__Return_1505(ResultCE_result, x);
        }
    })();
}

export class Decoding$1 extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["PayloadRequired", "NoPayloadRequired"];
    }
}

export function Decoding$1$reflection(gen0) {
    return union_type("StreamDeckDotnet.Context.Decoding`1", [gen0], Decoding$1, () => [[["decodeFunc", lambda_type(string_type, union_type("Microsoft.FSharp.Core.FSharpResult`2", [gen0, string_type], FSharpResult$2, () => [[["ResultValue", gen0]], [["ErrorValue", string_type]]]))], ["payload", option_type(string_type)]], [["Item", Types_Received_EventReceived$reflection()]]]);
}

export class EventContext {
    constructor(eventMetadata) {
        this.eventMetadata = eventMetadata;
        this._eventReceived = (void 0);
        this._eventsToSend = (void 0);
        this._sendEventQueue = null;
    }
}

export function EventContext$reflection() {
    return class_type("StreamDeckDotnet.Context.EventContext", void 0, EventContext);
}

export function EventContext_$ctor_401617BA(eventMetadata) {
    return new EventContext(eventMetadata);
}

export function EventContext__get_EventMetadata(_) {
    return _.eventMetadata;
}

export function EventContext__get_EventReceived(_) {
    return _._eventReceived;
}

export function EventContext__get_EventName(this$) {
    return EventContext__get_EventMetadata(this$).Event;
}

export function EventContext__get_TryBindEventAsync(_) {
    return AsyncResultCE_AsyncResultBuilder__Delay_Z64727ECD(AsyncResultCE_asyncResult, () => {
        const keyPayloadFunc = (mapper) => {
            const decoder = Types_Received_KeyPayloadDU_get_Decoder();
            return (payload) => Types_tryDecodePayload(uncurry(2, decoder), mapper, payload);
        };
        const applicationPayloadFunc = (mapper_1) => {
            const decoder_1 = Types_Received_ApplicationPayloadDU_get_Decoder();
            return (payload_1) => Types_tryDecodePayload(uncurry(2, decoder_1), mapper_1, payload_1);
        };
        let decoder_8;
        const event = _.eventMetadata.Event.toLowerCase();
        if (Types_$007CInvariantEqual$007C_$007C("keyDown", event) != null) {
            const mapper_2 = (v) => (new Types_Received_EventReceived(0, new Types_Received_KeyPayloadDU(0, v)));
            decoder_8 = ((p) => {
                const func = new Decoding$1(0, keyPayloadFunc(mapper_2), p);
                if (func.tag === 1) {
                    const e = func.fields[0];
                    return new FSharpResult$2(0, e);
                }
                else {
                    const payload_2 = func.fields[1];
                    const func_1 = func.fields[0];
                    if (payload_2 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_1 = payload_2;
                        const res_1 = func_1(p_1);
                        if (res_1.tag === 1) {
                            const msg = res_1.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_1, msg));
                        }
                        else {
                            const x = res_1.fields[0];
                            return new FSharpResult$2(0, x);
                        }
                    }
                }
            });
        }
        else if (Types_$007CInvariantEqual$007C_$007C("keyUp", event) != null) {
            const mapper_3 = (v_1) => (new Types_Received_EventReceived(1, new Types_Received_KeyPayloadDU(1, v_1)));
            decoder_8 = ((p_2) => {
                const func_2 = new Decoding$1(0, keyPayloadFunc(mapper_3), p_2);
                if (func_2.tag === 1) {
                    const e_1 = func_2.fields[0];
                    return new FSharpResult$2(0, e_1);
                }
                else {
                    const payload_3 = func_2.fields[1];
                    const func_3 = func_2.fields[0];
                    if (payload_3 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_3 = payload_3;
                        const res_3 = func_3(p_3);
                        if (res_3.tag === 1) {
                            const msg_1 = res_3.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_3, msg_1));
                        }
                        else {
                            const x_1 = res_3.fields[0];
                            return new FSharpResult$2(0, x_1);
                        }
                    }
                }
            });
        }
        else {
            decoder_8 = ((Types_$007CInvariantEqual$007C_$007C("didReceiveSettings", event) != null) ? ((p_4) => {
                let decoder_2;
                let func_5;
                const tupledArg = [(decoder_2 = Types_Received_SettingsPayload_get_Decoder(), (payload_4) => Types_tryDecodePayload(uncurry(2, decoder_2), (arg0_6) => (new Types_Received_EventReceived(2, arg0_6)), payload_4)), p_4];
                func_5 = (new Decoding$1(0, tupledArg[0], tupledArg[1]));
                if (func_5.tag === 1) {
                    const e_2 = func_5.fields[0];
                    return new FSharpResult$2(0, e_2);
                }
                else {
                    const payload_5 = func_5.fields[1];
                    const func_6 = func_5.fields[0];
                    if (payload_5 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_5 = payload_5;
                        const res_5 = func_6(p_5);
                        if (res_5.tag === 1) {
                            const msg_2 = res_5.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_5, msg_2));
                        }
                        else {
                            const x_2 = res_5.fields[0];
                            return new FSharpResult$2(0, x_2);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("didReceiveGlobalSettings", event) != null) ? ((p_6) => {
                let decoder_3;
                let func_8;
                const tupledArg_1 = [(decoder_3 = Types_Received_GlobalSettingsPayload_get_Decoder(), (payload_6) => Types_tryDecodePayload(uncurry(2, decoder_3), (arg0_10) => (new Types_Received_EventReceived(3, arg0_10)), payload_6)), p_6];
                func_8 = (new Decoding$1(0, tupledArg_1[0], tupledArg_1[1]));
                if (func_8.tag === 1) {
                    const e_3 = func_8.fields[0];
                    return new FSharpResult$2(0, e_3);
                }
                else {
                    const payload_7 = func_8.fields[1];
                    const func_9 = func_8.fields[0];
                    if (payload_7 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_7 = payload_7;
                        const res_7 = func_9(p_7);
                        if (res_7.tag === 1) {
                            const msg_3 = res_7.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_7, msg_3));
                        }
                        else {
                            const x_3 = res_7.fields[0];
                            return new FSharpResult$2(0, x_3);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("willAppear", event) != null) ? ((p_8) => {
                let decoder_4;
                let func_11;
                const tupledArg_2 = [(decoder_4 = Types_Received_AppearPayload_get_Decoder(), (payload_8) => Types_tryDecodePayload(uncurry(2, decoder_4), (arg0_14) => (new Types_Received_EventReceived(4, arg0_14)), payload_8)), p_8];
                func_11 = (new Decoding$1(0, tupledArg_2[0], tupledArg_2[1]));
                if (func_11.tag === 1) {
                    const e_4 = func_11.fields[0];
                    return new FSharpResult$2(0, e_4);
                }
                else {
                    const payload_9 = func_11.fields[1];
                    const func_12 = func_11.fields[0];
                    if (payload_9 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_9 = payload_9;
                        const res_9 = func_12(p_9);
                        if (res_9.tag === 1) {
                            const msg_4 = res_9.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_9, msg_4));
                        }
                        else {
                            const x_4 = res_9.fields[0];
                            return new FSharpResult$2(0, x_4);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("willDisappear", event) != null) ? ((p_10) => {
                let decoder_5;
                let func_14;
                const tupledArg_3 = [(decoder_5 = Types_Received_AppearPayload_get_Decoder(), (payload_10) => Types_tryDecodePayload(uncurry(2, decoder_5), (arg0_18) => (new Types_Received_EventReceived(5, arg0_18)), payload_10)), p_10];
                func_14 = (new Decoding$1(0, tupledArg_3[0], tupledArg_3[1]));
                if (func_14.tag === 1) {
                    const e_5 = func_14.fields[0];
                    return new FSharpResult$2(0, e_5);
                }
                else {
                    const payload_11 = func_14.fields[1];
                    const func_15 = func_14.fields[0];
                    if (payload_11 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_11 = payload_11;
                        const res_11 = func_15(p_11);
                        if (res_11.tag === 1) {
                            const msg_5 = res_11.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_11, msg_5));
                        }
                        else {
                            const x_5 = res_11.fields[0];
                            return new FSharpResult$2(0, x_5);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("titleParametersDidChange", event) != null) ? ((p_12) => {
                let decoder_6;
                let func_17;
                const tupledArg_4 = [(decoder_6 = Types_Received_TitleParametersPayload_get_Decoder(), (payload_12) => Types_tryDecodePayload(uncurry(2, decoder_6), (arg0_22) => (new Types_Received_EventReceived(6, arg0_22)), payload_12)), p_12];
                func_17 = (new Decoding$1(0, tupledArg_4[0], tupledArg_4[1]));
                if (func_17.tag === 1) {
                    const e_6 = func_17.fields[0];
                    return new FSharpResult$2(0, e_6);
                }
                else {
                    const payload_13 = func_17.fields[1];
                    const func_18 = func_17.fields[0];
                    if (payload_13 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_13 = payload_13;
                        const res_13 = func_18(p_13);
                        if (res_13.tag === 1) {
                            const msg_6 = res_13.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_13, msg_6));
                        }
                        else {
                            const x_6 = res_13.fields[0];
                            return new FSharpResult$2(0, x_6);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("deviceDidConnect", event) != null) ? ((p_14) => {
                let decoder_7;
                let func_20;
                const tupledArg_5 = [(decoder_7 = Types_Received_DeviceInfoPayload_get_Decoder(), (payload_14) => Types_tryDecodePayload(uncurry(2, decoder_7), (arg0_26) => (new Types_Received_EventReceived(7, arg0_26)), payload_14)), p_14];
                func_20 = (new Decoding$1(0, tupledArg_5[0], tupledArg_5[1]));
                if (func_20.tag === 1) {
                    const e_7 = func_20.fields[0];
                    return new FSharpResult$2(0, e_7);
                }
                else {
                    const payload_15 = func_20.fields[1];
                    const func_21 = func_20.fields[0];
                    if (payload_15 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_15 = payload_15;
                        const res_15 = func_21(p_15);
                        if (res_15.tag === 1) {
                            const msg_7 = res_15.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_15, msg_7));
                        }
                        else {
                            const x_7 = res_15.fields[0];
                            return new FSharpResult$2(0, x_7);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("deviceDidDisconnect", event) != null) ? ((_arg1) => {
                const func_22 = new Decoding$1(1, new Types_Received_EventReceived(8));
                if (func_22.tag === 1) {
                    const e_8 = func_22.fields[0];
                    return new FSharpResult$2(0, e_8);
                }
                else {
                    const payload_16 = func_22.fields[1];
                    const func_23 = func_22.fields[0];
                    if (payload_16 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_16 = payload_16;
                        const res_17 = func_23(p_16);
                        if (res_17.tag === 1) {
                            const msg_8 = res_17.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_16, msg_8));
                        }
                        else {
                            const x_8 = res_17.fields[0];
                            return new FSharpResult$2(0, x_8);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("applicationDidLaunch", event) != null) ? ((p_17) => {
                const mapper_4 = (v_2) => (new Types_Received_EventReceived(9, new Types_Received_ApplicationPayloadDU(0, v_2)));
                let func_25;
                const tupledArg_6 = [applicationPayloadFunc(mapper_4), p_17];
                func_25 = (new Decoding$1(0, tupledArg_6[0], tupledArg_6[1]));
                if (func_25.tag === 1) {
                    const e_9 = func_25.fields[0];
                    return new FSharpResult$2(0, e_9);
                }
                else {
                    const payload_17 = func_25.fields[1];
                    const func_26 = func_25.fields[0];
                    if (payload_17 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_18 = payload_17;
                        const res_19 = func_26(p_18);
                        if (res_19.tag === 1) {
                            const msg_9 = res_19.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_18, msg_9));
                        }
                        else {
                            const x_9 = res_19.fields[0];
                            return new FSharpResult$2(0, x_9);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("applicationDidTerminate", event) != null) ? ((p_19) => {
                const mapper_5 = (v_3) => (new Types_Received_EventReceived(10, new Types_Received_ApplicationPayloadDU(1, v_3)));
                let func_28;
                const tupledArg_7 = [applicationPayloadFunc(mapper_5), p_19];
                func_28 = (new Decoding$1(0, tupledArg_7[0], tupledArg_7[1]));
                if (func_28.tag === 1) {
                    const e_10 = func_28.fields[0];
                    return new FSharpResult$2(0, e_10);
                }
                else {
                    const payload_18 = func_28.fields[1];
                    const func_29 = func_28.fields[0];
                    if (payload_18 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_20 = payload_18;
                        const res_21 = func_29(p_20);
                        if (res_21.tag === 1) {
                            const msg_10 = res_21.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_20, msg_10));
                        }
                        else {
                            const x_10 = res_21.fields[0];
                            return new FSharpResult$2(0, x_10);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("systemDidWakeUp", event) != null) ? ((_arg2) => {
                const func_30 = new Decoding$1(1, new Types_Received_EventReceived(11));
                if (func_30.tag === 1) {
                    const e_11 = func_30.fields[0];
                    return new FSharpResult$2(0, e_11);
                }
                else {
                    const payload_19 = func_30.fields[1];
                    const func_31 = func_30.fields[0];
                    if (payload_19 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_21 = payload_19;
                        const res_23 = func_31(p_21);
                        if (res_23.tag === 1) {
                            const msg_11 = res_23.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_21, msg_11));
                        }
                        else {
                            const x_11 = res_23.fields[0];
                            return new FSharpResult$2(0, x_11);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("propertyInspectorDidAppear", event) != null) ? ((_arg3) => {
                const func_33 = new Decoding$1(1, new Types_Received_EventReceived(12));
                if (func_33.tag === 1) {
                    const e_12 = func_33.fields[0];
                    return new FSharpResult$2(0, e_12);
                }
                else {
                    const payload_20 = func_33.fields[1];
                    const func_34 = func_33.fields[0];
                    if (payload_20 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_22 = payload_20;
                        const res_25 = func_34(p_22);
                        if (res_25.tag === 1) {
                            const msg_12 = res_25.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_22, msg_12));
                        }
                        else {
                            const x_12 = res_25.fields[0];
                            return new FSharpResult$2(0, x_12);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("propertyInspectorDidDisappear", event) != null) ? ((_arg4) => {
                const func_36 = new Decoding$1(1, new Types_Received_EventReceived(13));
                if (func_36.tag === 1) {
                    const e_13 = func_36.fields[0];
                    return new FSharpResult$2(0, e_13);
                }
                else {
                    const payload_21 = func_36.fields[1];
                    const func_37 = func_36.fields[0];
                    if (payload_21 == null) {
                        return new FSharpResult$2(1, new PipelineFailure(3));
                    }
                    else {
                        const p_23 = payload_21;
                        const res_27 = func_37(p_23);
                        if (res_27.tag === 1) {
                            const msg_13 = res_27.fields[0];
                            return new FSharpResult$2(1, new PipelineFailure(0, p_23, msg_13));
                        }
                        else {
                            const x_13 = res_27.fields[0];
                            return new FSharpResult$2(0, x_13);
                        }
                    }
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("sendToPlugin", event) != null) ? ((p_24) => {
                if (p_24 == null) {
                    return new FSharpResult$2(0, new Types_Received_EventReceived(14, Newtonsoft_Json_Linq_JToken_Parse_Z721C83C5("{}")));
                }
                else {
                    const payload_22 = p_24;
                    return new FSharpResult$2(0, new Types_Received_EventReceived(14, Newtonsoft_Json_Linq_JToken_Parse_Z721C83C5(payload_22)));
                }
            }) : ((Types_$007CInvariantEqual$007C_$007C("sendToPropertyInspector", event) != null) ? ((p_25) => {
                if (p_25 == null) {
                    return new FSharpResult$2(0, new Types_Received_EventReceived(15, Newtonsoft_Json_Linq_JToken_Parse_Z721C83C5("{}")));
                }
                else {
                    const payload_23 = p_25;
                    return new FSharpResult$2(0, new Types_Received_EventReceived(15, Newtonsoft_Json_Linq_JToken_Parse_Z721C83C5(payload_23)));
                }
            }) : ((_arg5) => (new FSharpResult$2(1, new PipelineFailure(1, event))))))))))))))))));
        }
        return singleton.Return(decoder_8(_.eventMetadata.Payload));
    });
}

export function EventContext__AddSendEvent_Z6467CC7(_, e) {
    const matchValue = _._eventsToSend;
    if (matchValue != null) {
        const es = matchValue;
        _._eventsToSend = cons(e, es);
    }
    else {
        _._eventsToSend = singleton_1(e);
    }
}

export function EventContext__AddLog_Z721C83C5(_, msg) {
    const log = Types_createLogEvent(msg);
    return null;
}

export function EventContext__AddOk(_) {
    const ok = Types_createOkEvent();
    return null;
}

export function EventContext__AddAlert(_) {
    const ohno = Types_createAlertEvent();
    return null;
}

export function EventContext__GetEventsToSendFromList(_) {
    const matchValue = _._eventsToSend;
    if (matchValue == null) {
        return empty();
    }
    else {
        const x = matchValue;
        return x;
    }
}

export function EventContext__GetEventsToSend(_) {
    return ofArray(null);
}

export function EventContext__GetEncodedEventsToSend(this$) {
    return map((payload) => {
        let payload_1;
        const arg10 = EventContext__get_EventMetadata(this$).Device;
        payload_1 = Types_Sent_EventSent__Encode(payload, EventContext__get_EventMetadata(this$).Context, arg10);
        StreamDeckDotnet_Logging_Types_ILog__ILog_debug_1302DC96(logger, Logger_Operators_op_GreaterGreaterBangMinus(Logger_Operators_op_BangBang("Created event sent payload of {payload}"), "payload", payload_1));
        return payload_1;
    }, EventContext__GetEventsToSend(this$));
}

export function EventContext__PurgeEventsMatching_2E8A31C5(_, f) {
    const filteredEvents = null.filter((x) => (!f(x)));
    _._sendEventQueue = null;
    filteredEvents.forEach((x_1) => null);
}

export function EventContext__TryGetContextGuid(this$) {
    const matchValue = EventContext__get_EventMetadata(this$).Context;
    if (matchValue == null) {
        return void 0;
    }
    else {
        const x = matchValue;
        let matchValue_1;
        let outArg = "00000000-0000-0000-0000-000000000000";
        matchValue_1 = [tryParse(x, new FSharpRef(() => outArg, (v) => {
            outArg = v;
        })), outArg];
        if (matchValue_1[0]) {
            const v_1 = matchValue_1[1];
            return v_1;
        }
        else {
            return void 0;
        }
    }
}

export function addSendEvent(e, ctx) {
    EventContext__AddSendEvent_Z6467CC7(ctx, e);
    return ctx;
}

export function lift(ctx) {
    return Async_lift(ctx);
}


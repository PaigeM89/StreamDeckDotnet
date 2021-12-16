import { singleton } from "./.fable/fable-library.3.1.15/AsyncBuilder.js";

export function decodeString(s) {
    return "not working yet";
}

export function sendToPIHandler(payload, next, ctx) {
    return singleton.Delay(() => {
        const msg = null;
        throw 1;
        let ctx$0027;
        throw 1;
        return singleton.ReturnFrom(next(ctx$0027));
    });
}

export function keyDownHandler(payload, next, ctx) {
    return singleton.Delay(() => {
        throw 1;
        return singleton.ReturnFrom(next(ctx));
    });
}

export function errorHandler(err) {
    throw 1;
}

export const eventPipeline = (() => {
    throw 1;
})();


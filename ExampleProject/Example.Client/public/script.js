import { connectStreamDeck, sayHello } from '../src/App.fs';

function connectElgatoStreamDeckSocket(inPort, inPluginUUID, inRegisterEvent, inInfo) {
  connectStreamDeck(inPort, inPluginUUID, inRegisterEvent, inInfo);
}

function doesThisExist() {
  console.log('yes');
}

console.log('running in script.js');

console.log(sayHello());

// export { connectElgatoStreamDeckSocket };
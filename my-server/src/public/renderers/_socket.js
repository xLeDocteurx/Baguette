// Websocket implementation
//
// Starts a connection to the server and emits events for other scripts to
// listen for.

const socket = io();

console.info(`%cBoltobserv %cv1.0%c, at your service ❤ `, "font-weight: bold", "font-weight: bold; color:red", "font-weight: bold", "https://github.com/boltgolt/boltobserv/")

// socket.onAny((eventName, ...args) => {
// 	console.log(`Événement reçu : ${eventName}`, args);
// });

// // On a round indicator packet
// socket.element.addEventListener("round", event => {
// 	let phase = event.data

// 	// Abort if there's no change in phase
// 	if (global.gamePhase == phase) return

// 	// If the round has ended
// 	if ((phase == "freezetime" && global.gamePhase == "over") || (phase == "live" && global.gamePhase == "over")) {
// 		// Emit a custom event
// 		let roundend = new Event("roundend")
// 		socket.element.dispatchEvent(roundend)
// 	}

// 	// Set the new phase
// 	global.gamePhase = phase
// })

// socket.element.addEventListener("effect", event => {
// 	global.effects[event.data.key] = event.data.value
// })

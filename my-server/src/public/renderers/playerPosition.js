// Player position calculations
//
// Sets player dot style and pushed to player location buffer, but does not set
// the location.

socket.on("players", event => {
	// 	let data = event.sort((a, b) => a.Name - b.Name)
	let data = event

	// Abort if no map has been selected yet
	if (global.currentMap == "none") return

	// Loop though each player
	for (let i = 0; i < data.length; i++) {
		const player = data[i]
		// Get their player element and start building the class
		let playerDot = global.playerDots[i]
		let playerLabel = global.playerLabels[i]
		let playerTeam = 0;
		switch (player.Team) {
			case 2:
				playerTeam = "T"
				break;
			case 3:
				playerTeam = "CT"
				break;
			default:
				break;
		}
		let classes = [playerTeam]

		// Mark dead players with a cross
		if (player.Health <= 0) {
			// TODO : remove ???
			classes.push("dead")
			global.playerBuffers.map(el => [])
			console.log('global.playerBuffers : ', global.playerBuffers)
		}
		else {
			// Make the bomb carrier orange and and a line around the spectated player
			if (player.hasBomb) classes.push("bomb")
			// TODO : wtf ?
			// if (player.active) classes.push("active")
			if (true) classes.push("active")
			// TODO : wtf ?
			// if (player.flashed > 31) classes.push("flashed")

			// TODO : wtf ?
			// // If drawing muzzle flashes is enabled
			// if (global.config.radar.shooting) {
			// 	// Go through each weapon the player has
			// 	for (let weapon in player.ammo) {
			// 		if (global.playerAmmos[i][weapon]) {
			// 			// They are shooting if there's less ammo in the clip than the packet before
			// 			if (global.playerAmmos[i][weapon] > player.ammo[weapon]) {
			// 				classes.push("shooting")
			// 			}
			// 		}
			// 	}

			// 	// Save the last ammo stats for the next packet
			// 	global.playerAmmos[i] = player.ammo
			// }

			// If damage indicators are enabled
			if (global.config.radar.damage) {
				// If we have less health than last packet we are hurtin'
				if (player.Health < global.playerHealths[i]) {
					classes.push("hurting")
				}

				// Save the health value for next time
				global.playerHealths[i] = player.Health
			}

			// Save the position so the main loop can interpolate it
			global.playerPos[i].x = global.positionToPerc(player.PositionV3, "x", i)
			global.playerPos[i].y = global.positionToPerc(player.PositionV3, "y", i)

			global.playerPos[i].a = player.Angle * -1
			global.playerPos[i].z = player.PositionV3.Z
		}

		// Add all classes as a class string
		let newClasses = classes.join(" ")

		// Check if the new classname is different than the one already applied
		// This prevents unnecessary className updates and CSS recalculations
		if (playerDot.className != "dot " + newClasses) playerDot.className = "dot " + newClasses
		if (playerLabel.className != "label " + newClasses) playerLabel.className = "label " + newClasses

		// Set the player alive attribute (used in autozoom)
		global.playerPos[i].alive = player.Health > 0

		if (global.config.radar.showName == "both") {
			playerLabel.children[0].textContent = player.name.substring(0, global.config.radar.maxNameLength)
		}
		if (global.config.radar.showName == "always") {
			playerLabel.textContent = player.name.substring(0, global.config.radar.maxNameLength)
		}
	}
})

// // On round reset
// socket.on("roundend", event => {
// 	let phase = event.data

// 	// Go through each player
// 	for (let num in global.playerBuffers) {
// 		// Empty the location buffer
// 		global.playerBuffers[num] = []
// 		// Reset the player position
// 		global.playerPos[num] = {
// 			x: null,
// 			y: null,
// 			z: null,
// 			a: null,
// 			alive: false
// 		}

// 		// Force a re-render on the dot and label, can sometimes bug out in electron
// 		global.playerDots[num].style.display = "none"
// 		global.playerDots[num].style.display = "block"
// 		global.playerLabels[num].style.display = "none"
// 		global.playerLabels[num].style.display = ""
// 	}
// })

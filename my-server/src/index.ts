// /**
//  * IMPORTANT:
//  * ---------
//  * Do not manually edit this file if you'd like to host your server on Colyseus Cloud
//  *
//  * If you're self-hosting (without Colyseus Cloud), you can manually
//  * instantiate a Colyseus Server as documented here:
//  *
//  * See: https://docs.colyseus.io/server/api/#constructor-options
//  */
// import { listen } from "@colyseus/tools";

// // Import Colyseus config
// import app from "./app.config";

// // Create and listen on 2567 (or PORT environment variable.)
// listen(app);

import express from 'express';
import { createServer } from 'node:http';
import { DefaultEventsMap, Server, Socket } from 'socket.io';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { welcome } from './boltobserv';

const app = express();
const server = createServer(app);
const io = new Server(server, {
  pingInterval: 25000, // toutes les 25s
  pingTimeout: 60000    // attente max 5s pour le pong
});

// const __dirname = dirname(fileURLToPath(import.meta.url));
// const __dirname = __dirname; // déjà défini automatiquement
// const __dirname = require('path').dirname(__filename);


// let lastReceivedMap = "";
// let lastReceivedPlayers = [];

// Serve files from the "public" directory
app.use(express.static(join(__dirname, 'public')));

app.get("/favicon.ico", (req, res) => {res.sendFile(join(__dirname, "public/img/favicon.ico"))})

app.get('/', (req, res) => {
    res.sendFile(join(__dirname, 'public/index.html'));
});

app.get('/map', (req, res) => {
    res.sendFile(join(__dirname, 'public/map.html'));
});

io.on('connect', (socket) => {
    console.log(`User connected : ${socket.id}`);
    // ➕ Log en cas de déconnexion
    socket.on("disconnect", (reason) => {
        console.log(`User disconnected : ${socket.id} (raison : ${reason})`);
    });

    welcome(socket);

    socket.on('*', (msg) => {
        console.log('* message: ' + msg);
        // io.emit('chat message', msg);
    });

    socket.on('map', (msg) => {
        // console.log('map message : ' + msg);
        // lastReceivedMap = msg;
        try {
            io.emit('map', JSON.parse(msg))
        } catch (error) {
            console.log('WTF map : ', error)
        }
    });

    socket.on('players', (msg) => {
        // console.log('players message : ' + msg);
        // lastReceivedPlayers = msg;

        try {
            io.emit('players', JSON.parse(msg))
        } catch (error) {
            console.log('WTF players : ', error)
        }
    });
});

server.listen(3000, () => {
    console.log('server running at http://localhost:3000');
});

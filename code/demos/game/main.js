var express = require('express');
var http = require('http');
var path = require('path');
var dgram = require('dgram');

var WEB_PORT = 3000,
    OSC_PORT = 4344;

var app = express();

app.use('/static', express.static(path.join(__dirname, 'static')));

var server = http.createServer(app);
var io =require('socket.io').listen(server);
server.listen(WEB_PORT);

io.of('/ping').on('connection', function(socket) {
  socket.on('ping', function() {
    socket.emit('pong', null);
  });
});

var oscServer = dgram.createSocket('udp4');
oscServer.on('message', function(msg, rinfo) {
  io.of('/ping').emit('pong', msg.toString());
});
oscServer.bind(OSC_PORT);

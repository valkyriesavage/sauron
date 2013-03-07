// OSC Bridge by Javi Agenjo @tamat

var WebSocket = require('./node_modules/faye-websocket/lib/faye/websocket');

var fs        = require('fs'),
    http      = require('http'),
    https     = require('https'),
    qs		  = require('querystring'),
	osc		  = require('node-osc'),
    url		  = require('url');

//input parameters
var pos = process.argv.indexOf("-port")
var port   = (pos != -1 && (process.argv.length > pos + 1) ? process.argv[pos+1] : 4343);
    secure = process.argv.indexOf("-ssl") != -1;
var verbose = (process.argv.indexOf("-v") != -1 ? true : false);
if(verbose) console.log("verbose mode ON");

var OSC_PORT = port + 1;

//Server
var BroadcastServer = {
	clients: [],
	last_id: 1, //0 is reserved for server messages

	init: function()
	{
	},

	//NEW CLIENT
	onConnection: function(ws)
	{
		//initialize
		ws.user_id = this.last_id;
		this.last_id++;
		var path_info = url.parse(ws.url);
		var params = qs.parse(path_info.query);

		this.clients.push(ws);

		//ON MESSAGE CALLBACK
		ws.onmessage = function(event) {
			//console.log(ws.ip + ' = ' + typeof(event.data) + "["+event.data.length+"]:" + event.data );
			//console.dir(event.data); //like var_dump

			//this.send(...);
		};

		//ON CLOSE CALLBACK
		ws.onclose = function(event) {
			console.log('close', event.code, event.reason);
			BroadcastServer.clients.splice( BroadcastServer.clients.indexOf(ws), 1);
			ws = null;
		};
	},

	sendToAll: function(data, skip_id )
	{
		//broadcast
		for(var i in BroadcastServer.clients)
			if (BroadcastServer.clients[i].user_id != skip_id)
				BroadcastServer.clients[i].send(data);
	}
};

// OSC SERVER **********************************************

// so let's start to listen on OSC_PORT
console.log("OSC Server in port: " + OSC_PORT );
var OSCserver = new osc.Server(OSC_PORT, '127.0.0.1');
var messages_detected = {};

OSCserver.on('message', function (args) {
	//client.send({ message: '/lp/scene ' + args });
	//console.log("msg! " + args);
	if(!messages_detected[args[0]])
	{
		if(args[0] == "#bundle")
		{
			for(var i = 2; i < args.length; i++)
			{
				var bundle_args = args[i];
				if(!messages_detected[bundle_args[0]])
				{
					console.log("New OSC msg detected: " + bundle_args[0]);
					messages_detected[ bundle_args[0] ] = true;
				}
			}
		}
		else
		{
			console.log("New OSC msg detected: " + args[0]);
			messages_detected[ args[0] ] = true;
		}
	}
	//console.dir(args);
	BroadcastServer.sendToAll( args.toString() );
});


//create packet server
var connectionHandler = function(request, socket, head) {
	var ws = new WebSocket(request, socket, head, ['irc', 'xmpp'], {ping: 5});
	console.log('open', ws.url, ws.version, ws.protocol);
	BroadcastServer.onConnection(ws);
};

// HTTP SERVER  (used for administration) **********************
var staticHandler = function(request, response)
{
	var path = request.url;
	console.log("http request: " + path);

	function sendResponse(response,status_code,data)
	{
		response.writeHead(status_code, {'Content-Type': 'text/plain', "Access-Control-Allow-Origin":"*"});
		if( typeof(data) == "object")
			response.write( JSON.stringify(data) );
		else
			response.write( data );
		response.end();
	}

	fs.readFile(__dirname + path, function(err, content) {
		var status = err ? 404 : 200;
		sendResponse(response, status, content || "file not found");
	});
};

//Prepare server
BroadcastServer.init();

//create the server (if it is SSL then add the cripto keys)
var server = secure
           ? https.createServer({
               key:  fs.readFileSync(__dirname + '/../spec/server.key'),
               cert: fs.readFileSync(__dirname + '/../spec/server.crt')
             })
           : http.createServer();
server.addListener('request', staticHandler); //incoming http connections
server.addListener('upgrade', connectionHandler); //incomming websocket connections

//launch the server
console.log('WebSocket Server in port...', port);
server.listen(port);





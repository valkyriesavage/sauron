// Handle keyboard controls
var keysDown = [];

addEventListener("keydown", function (e) {
	keysDown[e.keyCode] = true;
}, false);

addEventListener("keyup", function (e) {
	delete keysDown[e.keyCode];
}, false);

// WebSocketssssssss!!!!
var ws = new WebSocket("ws://localhost:5002");
ws.onmessage = function(event) {
	var message = event.data;
	switch(message.split("/")[0]) {
		case "joystick":
			break;
		case "dpad":
			break;
		case "button":
			break;
		default:
			break;
	}
}

var joystickDealings = function(message) {

}

var dpadDealings = function(message) {
	var payload = message.substring(message.split(" ")[0].length);
	switch(payload) {
		case 'up':
			keysDown['w'] = true;
			break;
		case 'down':
			keysDown['s'] = true;
			break;
		case 'right':
			keysDown['a'] = true;
			break;
		case 'left':
			keysDown['d'] = true;
			break;
		default:
			console.log(payload);
	}
}

var buttonDealings = function(message) {
	var id = message.split("/")[1].split()[0];
}

// Create the canvas
var canvas = document.createElement("canvas");
var ctx = canvas.getContext("2d");
canvas.width = 512;
canvas.height = 480;
document.body.appendChild(canvas);

// Background image
var bgReady = false;
var bgImage = new Image();
bgImage.onload = function () {
	bgReady = true;
};
bgImage.src = "images/background.png";

// Hero image
var heroReady = false;
var heroImage = new Image();
heroImage.onload = function () {
	heroReady = true;
};
heroImage.src = "images/hero.png";

// Monster image
var monsterReady = false;
var monsterImage = new Image();
monsterImage.onload = function () {
	monsterReady = true;
};
monsterImage.src = "images/monster.png";

// Game objects
var hero = {
	speed: 256, // movement in pixels per second
	color: 'green'
};
var monster = {};
var monstersCaught = 0;

var fireballs = new Array();

// Reset the game when the player catches a monster
var reset = function () {
	hero.x = canvas.width / 2;
	hero.y = canvas.height / 2;

	// Throw the monster somewhere on the screen randomly
	monster.x = 32 + (Math.random() * (canvas.width - 64));
	monster.y = 32 + (Math.random() * (canvas.height - 64));

	fireballs = new Array();
};

var fireBall = function(color, nextX, nextY) {
	var myFireball = {};
	myFireball.color = color;
	myFireball.x = hero.x;
	myFireball.y = hero.y;
	myFireball.nextX = nextX;
	myFireball.nextY = nextY;

	myFireball.image = new Image();
	myFireball.image.src = "images/ball-"+color+".png";

	return myFireball;
}

// Update game objects
var update = function (modifier) {
	if (38 in keysDown) { // Player holding up
		hero.y -= hero.speed * modifier;
	}
	if (40 in keysDown) { // Player holding down
		hero.y += hero.speed * modifier;
	}
	if (37 in keysDown) { // Player holding left
		hero.x -= hero.speed * modifier;
	}
	if (39 in keysDown) { // Player holding right
		hero.x += hero.speed * modifier;
	}

	if (87 in keysDown) { // Player pushed w
		fireballs[fireballs.length] = fireBall('red', function(x) {
			return x+hero.speed*modifier;
		}, function(y) {
			return y;
		});
		delete keysDown[87];
	}
	if (65 in keysDown) { // Player pushed a
		fireballs[fireballs.length] = fireBall('pink', function(x) {
			return x-hero.speed*modifier;
		}, function(y) {
			return y;
		});
		delete keysDown[65];
	}
	if (83 in keysDown) { // Player pushed s
		fireballs[fireballs.length] = fireBall('grey', function(x) {
			return x;
		}, function(y) {
			return y+hero.speed*modifier;
		});
		delete keysDown[83];
	}
	if (68 in keysDown) { // player pushed d
		fireballs[fireballs.length] = fireBall('lime', function(x) {
			return x;
		}, function(y) {
			return y-hero.speed*modifier;
		});
		delete keysDown[68];
	}

	if (49 in keysDown) { // player pushed 1
		hero.color = 'blue';
	}
	if (50 in keysDown) { // player pushed 2
		hero.color = 'green';
	}
	if (51 in keysDown) { // player pushed 3
		hero.color = 'red';
	}
	if (52 in keysDown) { // player pushed 4
		hero.color = 'yellow';
	}

	// Are they touching?
	if (
		hero.x <= (monster.x + 32)
		&& monster.x <= (hero.x + 32)
		&& hero.y <= (monster.y + 32)
		&& monster.y <= (hero.y + 32)
	) {
		++monstersCaught;
		reset();
	}

	// move all the fireballs
	for(var i = 0; i < fireballs.length; i++) {
		fireballs[i].x = fireballs[i].nextX(fireballs[i].x);
		fireballs[i].y = fireballs[i].nextY(fireballs[i].y);
	}
};

// Draw everything
var render = function () {
	if (bgReady) {
		ctx.drawImage(bgImage, 0, 0);
	}

	if (heroReady) {
		heroImage.src = "images/hero-"+hero.color+".png";
		ctx.drawImage(heroImage, hero.x, hero.y);
	}

	if (monsterReady) {
		ctx.drawImage(monsterImage, monster.x, monster.y);
	}

	for(var i =0; i < fireballs.length; i++) {
		ctx.drawImage(fireballs[i].image, fireballs[i].x, fireballs[i].y);
	}

	// Score
	ctx.fillStyle = "rgb(250, 250, 250)";
	ctx.font = "24px Helvetica";
	ctx.textAlign = "left";
	ctx.textBaseline = "top";
	ctx.fillText("Goblins caught: " + monstersCaught, 32, 32);
};

// The main game loop
var main = function () {
	var now = Date.now();
	var delta = now - then;

	update(delta / 1000);
	render();

	then = now;
};

// Let's play this game!
reset();
var then = Date.now();
setInterval(main, 1); // Execute as fast as possible

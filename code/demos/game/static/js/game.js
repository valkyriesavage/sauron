

// Handle keyboard controls
var keysDown = [];

addEventListener("keydown", function (e) {
	keysDown[e.keyCode] = true;
}, false);

addEventListener("keyup", function (e) {
	delete keysDown[e.keyCode];
}, false);


var joystickDealings = function(payload) {
    if(payload.indexOf('up') > -1) {
      keysDown[38] = true;
    } else {
      delete keysDown[38];
    }
    if(payload.indexOf('down') > -1) {
      keysDown[40] = true;
    } else {
      delete keysDown[40];
    }
    if(payload.indexOf('right') > -1) {
      keysDown[37] = true;
    } else {
      delete keysDown[37];
    }
    if(payload.indexOf('left') > -1) {
      keysDown[39] = true;
    } else {
      delete keysDown[39];
    }
};

var dpadDealings = function(payload) {
	switch(payload) {
		case 'up':
			keysDown[49] = true;
			break;
		case 'down':
			keysDown[50] = true;
			break;
		case 'right':
			keysDown[51] = true;
			break;
		case 'left':
			keysDown[52] = true;
			break;
                case 'none':
                        delete keysDown[49];
                        delete keysDown[50];
                        delete keysDown[51];
                        delete keysDown[52];
		default:
			console.log(payload);
	}
};

var buttonDealings = function(id, state) {
  id = "" + id;
  if(state.indexOf('true') > 0) {
    if(id.indexOf("0") > -1 || id.indexOf("4") > -1) {
        keysDown[87] = true;
    } else if (id.indexOf("1") > -1) {
        keysDown[65] = true;
    } else if (id.indexOf("2") > -1) {
        keysDown[83] = true;
    } else if (id.indexOf("3") > -1) {
        keysDown[68] = true;
    } else {
        console.log(id);
    }
  } else {
    if(id.indexOf("0") > -1 || id.indexOf("4") > -1) {
        delete keysDown[87];
    } else if (id.indexOf("1") > -1) {
        delete keysDown[65];
    } else if (id.indexOf("2") > -1) {
        delete keysDown[83];
    } else if (id.indexOf("3") > -1) {
        delete keysDown[68];
    }
  }
};

// WebSocketssssssss!!!!
var socket = io.connect('http://localhost:3000/ping');
socket.on('pong', function(message) {
        var id = message.split('/')[2].split(',s')[0];
        var payload = message.split(',s')[1];
	switch(message.split("/")[1]) {
		case "dpad":
                  if(id.indexOf('1') > -1) {
                    joystickDealings(payload);
                  } else {
                    dpadDealings(payload);
                  }
                  break;
                case "joystick":
                  joystickDealings(payload);
		case "button":
                  buttonDealings(id, payload);
                  break;
		default:
                  console.log(message.split("/")[1]);
                  break;
	}
});

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
var heroPrevColor = "";

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

        if (hero.x > canvas.width - 32) hero.x = canvas.width - 32;
        if (hero.x < 0) hero.x = 0;
        if (hero.y > canvas.height - 32) hero.y = canvas.height - 32;
        if (hero.y < 0) hero.y = 0;

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
            if(hero.color != heroPrevColor) {
		heroImage.src = "/static/images/hero-"+hero.color+".png";
                heroPrevColor = hero.color;
            }
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

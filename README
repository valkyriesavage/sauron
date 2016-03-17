Sauron
======
Sauron is a tool for prototyping functional objects, like video game
controllers, DJ mixers, or ergonomic mice. It works on computer vision, so
you don't have to spend time wiring up each button or joystick that you want
in your final prototype: a single camera can see them all.

The way it works is this: first, you create a 3D model of your object in
Solidworks, and put in special tagged geometry so Sauron knows what to look
for:
![modeled object](images/shots/00.png "Modeled game controller")
Then, you add in a 3D model which represents the camera you plan to use for
sensing. It's field of vision also needs to be modeled and tagged:
![modeled object](images/shots/01.png "Modeled game controller with FOV")
As you start working through the user interface, Sauron will do some analysis
on the locations of the parts in your object:
![modeled object](images/shots/02.png "Transparent controller")
First, we decide if there's anything that already falls in the camera's
field of view (FOV). In this case, the joystick and D-Pad on the front
of the controller are already visible.
![modeled object](images/shots/03.png "visbility analysis")
So we ignore those.
![modeled object](images/shots/04.png "ignore visible parts")
Sauron can also extrude pieces to go into the camera's field of view. The
bumper buttons on the side of the controller were processed in this way:
![modeled object](images/shots/05.png "extended sides")
Originally they were stumpy little tagged geometry pieces,
![modeled object](images/shots/06.png "stumpy geometry")
But we just stretch them until they reach the FOV cone.
![modeled object](images/shots/07.png "stretched sides")
So, those are done once we extend them.
![modeled object](images/shots/08.png "extend sides final")
So we'll ignore them for now.
![modeled object](images/shots/09.png "ignore the sides, they are done")
These buttons in the back are a different problem. We could extend them until
they hit the FOV cone, but unfortunately they'll hit the controller body if we
do that. Sauron does simulation to check, and finds that that's true.
![modeled object](images/shots/10.png "red buttons")
So we'll have to go for plan B. Sauron's plan B is mirrors. We can figure out
where the mirrors need to go by doing raycasting: the camera sends out a bunch
of rays, looks at where they go after they bounce off the inside surface of the
controller, and checks to see if any of them actually hit the buttons we're
looking for. All the ones that do are shown here. Sauron will put stars to
indicate where a mirror should go after printing (the camera will be able
to see the reflection of the buttons in the mirror):
![modeled object](images/shots/11.png "reflection")
So, all put together, our processed controller looks like this:
![modeled object](images/shots/12.png "all together")
And when we print it out, we attach the camera that we modeled, color the
buttons, add the mirrors, and show it to the computer vision algorithms.
Then we're set to go!
![modeled object](images/process.jpg "The whole Sauron process")
Sauron supports several kinds of inputs, which have different kinds of
motion, like buttons, dials, trackballs, and sliders.
![modeled object](images/sauron-inputs.png "Sauron takes a wide range
of inputs!") 
You can make your own inputs if you want to code up the computer vision
algorithms for them, but you can also just modify the existing inputs
to fit your needs, like turning a dial into a volume knob or a DJ
mixing wheel:
![modeled object](images/changeknob.png "dial can become many things")
The computer vision works in different ways depending on the way the
component moves. You can dig around and check it out, but the gist
is this:
![modeled object](images/vision-all-components.png "computer vision")

Technical Details
=================
The Sauron model-modification tool is written as a plugin for SolidWorks.
I wrote and tested it on SW2012, but theoretically it will work in newer
versions of that tool. The computer vision subsystems are written with
OpenFrameworks, and everything in here communicates via OSC messages.

Here there be Dragons
=====================
This code was written as a part of my PhD, and thus it can be sketchy in
some places! It's not very well documented, but I wanted to provide it
on github for anyone curious about spelunking through it and hopefully
making use of it in some way.

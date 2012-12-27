#include <math.h>

#include "ofxOpenCv.h"
#include "utilities.h"

#pragma once
class ComponentTracker {
public:
	enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel};
	enum Direction {up, down, left, right};
	
	ofxCvContourFinder contourFinder;
	ComponentType comptype;
	CvRect regionOfInterest;
	int numBlobsNeeded;
	
	bool buttonEventDetected();
	bool sliderEventDetected(int* sliderPosition);
	bool dialEventDetected(int* dialPosition);
    bool scrollWheelEventDetected(Direction* scrollDirection);
	bool joystickEventDetected(int* xPosition, int* yPosition);
	bool dpadEventDetected(Direction* direction);
};
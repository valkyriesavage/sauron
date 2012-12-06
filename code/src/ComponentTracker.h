#include "ofxOpenCv.h"
#include "utilities.h"

#pragma once
class ComponentTracker {
public:
	enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel};
	enum Direction {up, down, left, right};
	
	ComponentType comptype;
	CvRect regionOfInterest;
	int numBlobsNeeded;
	
	bool buttonEventDetected(ofxCvContourFinder contourFinder);
	bool sliderEventDetected(ofxCvContourFinder contourFinder, int* sliderPosition);
	bool dialEventDetected(ofxCvContourFinder contourFinder, int* dialPosition);
	bool joystickEventDetected(ofxCvContourFinder contourFinder, int* xPosition, int* yPosition);
	bool dpadEventDetected(ofxCvContourFinder contourFinder, Direction* direction);
};
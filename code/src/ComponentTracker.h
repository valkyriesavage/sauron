#include <math.h>
#include <vector>

#include "ofxOpenCv.h"
#include "utilities.h"

#pragma once
class ComponentTracker {
public:
	enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel};
	enum Direction {up, down, left, right};
    
    ComponentTracker();
	
	ofxCvContourFinder contourFinder;
	ComponentType comptype;
	CvRect regionOfInterest;
	int numBlobsNeeded;
    
    std::vector<ofxCvBlob> previousBlobs;
	
	bool buttonEventDetected();
	bool sliderEventDetected(int* sliderPosition);
	bool dialEventDetected(int* dialPosition);
    bool scrollWheelEventDetected(Direction* scrollDirection, int* scrollAmount);
	bool joystickEventDetected(int* xPosition, int* yPosition);
	bool dpadEventDetected(Direction* direction);
	
	void setROI(std::vector<ofPoint> bounds);
};
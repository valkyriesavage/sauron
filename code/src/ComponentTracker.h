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
	ofRectangle ROI;//there's a bit of ambiguity with regionOfInterest. Not sure why regionOfInterest is a CvRect rather than a ofRectangle
	int numBlobsNeeded;
    
    std::vector<ofxCvBlob> previousBlobs;
	
	bool buttonEventDetected();
	bool sliderEventDetected(int* sliderPosition);
	bool dialEventDetected(int* dialPosition);
    bool scrollWheelEventDetected(Direction* scrollDirection, int* scrollAmount);
	bool joystickEventDetected(int* xPosition, int* yPosition);
	bool dpadEventDetected(Direction* direction);
	
	void setROI(std::vector<ofRectangle> bounds);
	void setSliderROI(std::vector<ofRectangle> bounds);
	void setDialROI(std::vector<ofRectangle> bounds);
	void setButtonROI(std::vector<ofRectangle> bounds);
	void setDpadROI(std::vector<ofRectangle> bounds);
	void setContourFinder(ofRectangle ROI, int numBlobs);
	float calculateSliderProgress(ofxCvBlob blob);
};
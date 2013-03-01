#include <math.h>
#include <vector>

#include "ofxOpenCv.h"
#include "utilities.h"

#pragma once
class ComponentTracker {
public:
	enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel, no_component};
	
	enum Direction {up, down, left, right, none};
	

    ComponentTracker();
	ComponentTracker(ComponentType type, int id);
	string getComponentType();
	ofxCvContourFinder contourFinder;
	ComponentType comptype;
	CvRect regionOfInterest;
	ofRectangle ROI;//there's a bit of ambiguity with regionOfInterest. Not sure why regionOfInterest is a CvRect rather than a ofRectangle
	int numBlobsNeeded;
	int id;
	float delta;
	bool mIsRegistered;
	float mThreshold;
    
    std::vector<ofxCvBlob> previousBlobs;
	std::vector<ofRectangle> previousRectangles;//rectangles are used during the ROI set stage of registration (instead of blobs)
	
	bool isRegistered();
	bool finalizeRegistration();
	
	bool buttonEventDetected();
	bool sliderEventDetected(int* sliderPosition);
	bool dialEventDetected(int* dialPosition);
    bool scrollWheelEventDetected(Direction* scrollDirection, int* scrollAmount);
	bool joystickEventDetected(int* xPosition, int* yPosition);
	bool dpadEventDetected(Direction* direction);
	
	void setROI(std::vector<ofxCvBlob> blobs);

	void setContourFinder(ofRectangle ROI, int numBlobs);
	float calculateSliderProgress(std::vector<ofxCvBlob> blobs);
	float calculateDialProgress(std::vector<ofxCvBlob> blobs);
	ComponentTracker::Direction calculateScrollWheelDirection(std::vector<ofxCvBlob> blobs);
	bool isButtonPressed(std::vector<ofxCvBlob> blobs);
	ofPoint measureJoystickLocation(std::vector<ofxCvBlob> blobs);
	
	float getDelta();
	void setDelta(float f);
	
	bool ROIUnset();
	
	std::vector<ofxCvBlob> keepInsideBlobs(std::vector<ofxCvBlob> blobs);
};
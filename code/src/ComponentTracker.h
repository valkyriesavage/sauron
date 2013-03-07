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
	const char*  EnumDirectionToString(ComponentTracker::Direction dir);
	ofxCvContourFinder contourFinder;
	ComponentType comptype;
	CvRect regionOfInterest;
	ofRectangle ROI;//there's a bit of ambiguity with regionOfInterest. Not sure why regionOfInterest is a CvRect rather than a ofRectangle
	int numBlobsNeeded;
	int id;
	string delta;
	bool mIsRegistered;
	float mThreshold;
	ComponentTracker::Direction mPreviousScrollWheelDirection;
    
    std::vector<ofxCvBlob> previousBlobs;
	std::vector<ofRectangle> previousRectangles;//rectangles are used during the ROI set stage of registration (instead of blobs)
	
	bool isRegistered();
	void finalizeRegistration();
	
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
	ComponentTracker::Direction calculateDpadDirection(std::vector<ofxCvBlob> blobs);
	ofPoint measureJoystickLocation(std::vector<ofxCvBlob> blobs);
	
	string getDelta();
	void setDelta(string f);
	
	bool ROIUnset();
	
	std::vector<ofxCvBlob> keepInsideBlobs(std::vector<ofxCvBlob> blobs);
	
	ComponentTracker::Direction getRelativeDirection(ofxCvBlob largestBlob, std::vector<ofxCvBlob> dpadBlobs);
};
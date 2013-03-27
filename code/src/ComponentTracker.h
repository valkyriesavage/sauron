#include <math.h>
#include <limits>
#include <vector>

#include "ofxOpenCv.h"
#include "utilities.h"

#pragma once
class ComponentTracker {
public:
		//enums
	enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel, no_component};
	enum Direction {up, down, left, right, none};

		//constructors
    ComponentTracker();
	ComponentTracker(ComponentType type, int id);
	
		//enums to strings
	string getComponentTypeString();
	const char*  EnumDirectionToString(ComponentTracker::Direction dir);

		//flagged for deletion...
	ofxCvContourFinder contourFinder;
	CvRect regionOfInterest;
	bool buttonEventDetected();
	bool sliderEventDetected(int* sliderPosition);
	bool dialEventDetected(int* dialPosition);
    bool scrollWheelEventDetected(Direction* scrollDirection, int* scrollAmount);
	bool joystickEventDetected(int* xPosition, int* yPosition);
	bool dpadEventDetected(Direction* direction);
		//...until here
	
		//registration
	bool isRegistered();
	void finalizeRegistration();
	
		//ROI
	void setROI(std::vector<ofxCvBlob> blobs);
	ofRectangle getROI();
	bool ROIUnset();
	
		//component calculations
	bool measureComponent(std::vector<ofxCvBlob> blobs);
	float calculateSliderProgress(std::vector<ofxCvBlob> blobs);
	float calculateDialProgress(std::vector<ofxCvBlob> blobs);
	ComponentTracker::Direction calculateScrollWheelDirection(std::vector<ofxCvBlob> blobs);
	bool isButtonPressed(std::vector<ofxCvBlob> blobs);
	ComponentTracker::Direction calculateDpadDirection(std::vector<ofxCvBlob> blobs);
	ofPoint measureJoystickLocation(std::vector<ofxCvBlob> blobs);
	
		//deltas
	string getDelta();
	void setDelta(string f);
	bool isPrevDeltaInitialized();
	
	int getId();
	ComponentTracker::ComponentType getComponentType();
	void setContourFinder(ofRectangle ROI, int numBlobs);
	bool isDeltaSignificant();
	bool verifyNumBlobs(int numBlobs);

	void determineDialEllipse();
	
private:
	double jiggleThreshold;
	bool mIsRegistered;
	void init(ComponentType type, int id);
	int numBlobsNeeded;
	int id;
	string delta;
	string prevDelta;
	ComponentType comptype;
	float mThreshold;
	ComponentTracker::Direction mPreviousScrollWheelDirection;
	ofRectangle ROI;//there's a bit of ambiguity with regionOfInterest. Not sure why regionOfInterest is a CvRect rather than a ofRectangle

    std::vector<ofxCvBlob> previousBlobs;
	std::vector<ofRectangle> previousRectangles;//rectangles are used during the ROI set stage of registration (instead of blobs)
	
	ofxCvBlob mButtonOrigin;
	
	std::vector<ofxCvBlob> keepInsideBlobs(std::vector<ofxCvBlob> blobs);
	ComponentTracker::Direction getRelativeDirection(ofxCvBlob largestBlob, std::vector<ofxCvBlob> dpadBlobs);


	// can I just say that this NEEDS TO BE REFACTORED
	// we should split this crap out into detectors for each type of component, rather than this horrible, horrible thing
	ofPoint sliderStart;
	ofPoint sliderEnd;

	std::vector<ofPoint> dialPoints;
	ofPoint dialEllipseCenter;
	double dialEllipseWidth, dialEllipseHeight;
};

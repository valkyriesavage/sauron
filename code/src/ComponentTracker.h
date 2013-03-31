#include <math.h>
#include <limits>
#include <vector>

#include "ofxOpenCv.h"
#include "utilities.h"

#pragma once
class ComponentTracker {
public:
		//enums
	enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel, trackball, no_component};
	enum Direction {up, down, left, right, none};

		//constructors
    ComponentTracker();
	ComponentTracker(ComponentType type, int id);

	// let everyone know!!
	ComponentType comptype;
	
		//enums to strings
	string getComponentTypeString();
	const char*  EnumDirectionToString(ComponentTracker::Direction dir);

		//flagged for deletion...
	ofxCvContourFinder contourFinder;
	
	bool buttonEventDetected();
	bool sliderEventDetected(int* sliderPosition);
	bool dialEventDetected(int* dialPosition);
    bool scrollWheelEventDetected(Direction* scrollDirection, int* scrollAmount);
	bool joystickEventDetected(int* xPosition, int* yPosition);
	bool dpadEventDetected(Direction* direction);
	CvRect regionOfInterest;
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
	ofPoint calculateTrackballValue(std::vector<ofxCvBlob> blobs);

	float xJoystick(ofPoint middleCentroid);
	float yJoystick(ofPoint flankCentroid);
	
		//deltas
	string getDelta();
	void setDelta(string f);
	bool isPrevDeltaInitialized();
	
	int getId();
	ComponentTracker::ComponentType getComponentType();
	void setContourFinder(ofRectangle ROI, int numBlobs);
	bool isDeltaSignificant();
	bool verifyNumBlobs(int numBlobs);

	// can I just say that this NEEDS TO BE REFACTORED
	// we should split this crap out into detectors for each type of component, rather than this horrible, horrible thing
	ofPoint sliderStart;
	ofPoint sliderEnd;

	std::vector<ofxCvBlob> dpadAtRestBlobs;

	ofPoint joystickMiddleStart;
	ofPoint joystickMiddleEnd;
	ofPoint joystickFlankStart;
	ofPoint joystickFlankEnd;

	ofPoint trackballXDirection;  // this should be a vector...
	ofPoint trackballYDirection;  // this should, too... :(


	// Well, for some reason we have two of these
	ofRectangle ROI;

private:
	double jiggleThreshold;
	bool mIsRegistered;
	void init(ComponentType type, int id);
	int numBlobsNeeded;
	int id;
	string delta;
	string prevDelta;
	float mThreshold;
	ComponentTracker::Direction mPreviousScrollWheelDirection;
	
    std::vector<ofxCvBlob> previousBlobs;
	std::vector<ofRectangle> previousRectangles;//rectangles are used during the ROI set stage of registration (instead of blobs)
	
	ofxCvBlob mButtonOrigin;
	
	std::vector<ofxCvBlob> keepInsideBlobs(std::vector<ofxCvBlob> blobs);
	ComponentTracker::Direction getRelativeDirection(ofxCvBlob largestBlob, std::vector<ofxCvBlob> dpadBlobs);

	
};

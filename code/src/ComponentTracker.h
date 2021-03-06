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
	bool buttonSentTrue;
	
	ofPoint sliderStart;
	ofPoint sliderEnd;
	
	time_t changedAtTime;

	std::vector<ofxCvBlob> atRestBlobs;

	ofPoint joystickMiddleStart;
	ofPoint joystickMiddleEnd;
	ofPoint joystickFlankStart;
	ofPoint joystickFlankEnd;
	
	ofPoint joystickLocation;
	ofPoint prevJoystickLocation;

	ofPoint trackballXDirection;  // this should be a vector...
	ofPoint trackballYDirection;  // this should, too... :(
	int trackballCenterBlob;
	
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
	
	int closestBlobToROICentre(std::vector<ofxCvBlob> blobs);
	
	int idOfMiddleBlob;
	bool blobIsMiddle(ofxCvBlob blob, std::vector<ofxCvBlob> blobs);
};

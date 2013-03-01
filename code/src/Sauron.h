#pragma once
#include <vector>
#include <stdio.h>
#include <string.h>
#include <algorithm>
#include "ofMain.h"
#include "ofxOpenCv.h"
#include "ofxOsc.h"
#include "ofxOscReceiver.h"
#include "ofxOscMessage.h"

#include "ComponentTracker.h"
#include "utilities.h"

#define PORT 5001
#define HOST "localhost"

class Sauron : public ofBaseApp{

	public:
	void setup();
	void update();
	void draw();

	void keyPressed(int key);
	
	
	void stageComponent(ComponentTracker::ComponentType type, int id);
	void startRegistrationMode();
	void stopRegistrationMode();
	void sauronRegister();
	void sauronLoad();
	int assignSauronId();
	void registerComponent(ComponentTracker*);

	ofVideoGrabber 		vidGrabber;

	ofxCvColorImage			colorImg;

	ofxCvGrayscaleImage 	grayImage;
	
	ofxCvContourFinder	contourFinderGrayImage;

	int 				threshold;
	bool				registering;
	bool				testing;//just since we are testing testing
	int					mNumBlobsConsidered;

	std::vector<ComponentTracker*> components;
	ComponentTracker* currentRegisteringComponent;
	std::vector<ofxCvBlob> blobsCaptured;//assists with registration

	char mSliderProgress[128];
	char mDialProgress[128];
	char mScrollWheelDirection[128];
	char mButtonPressed[128];
	char mJoystickLocation[128];

	ofxOscReceiver receiver;
	ofxOscSender sender;
};


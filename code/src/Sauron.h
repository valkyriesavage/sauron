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
#include "TrackballTracker.h"
#include <iostream>
#include <fstream>

#define RECEIVE_PORT 5001
#define SEND_PORT 8000
#define WEBSOCKETS_PORT 4344
#define HOST "128.32.45.234"

#define PRINTED_IN_WHITE true

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
	void registerComponent(ComponentTracker*);
	
	void resetSauron();

	ofVideoGrabber 		vidGrabber;

	ofxCvColorImage			colorImg;

	ofxCvGrayscaleImage 	grayImage;
	
	ofxCvGrayscaleImage prevImage;
	
	ofxCvContourFinder	contourFinderGrayImage;

	int 				threshold;
	bool				registering;
	bool				testing;
	int					mNumBlobsConsidered;
	bool				mArduinoTest;
	
	std::vector<ComponentTracker*> components;
	ComponentTracker* currentRegisteringComponent;
	std::vector<ofxCvBlob> blobsCaptured;//assists with registration

	char mSliderProgress[128];
	char mDialProgress[128];
	char mScrollWheelDirection[128];
	char mButtonPressed[128];
	char mDpadDirection[128];
	char mJoystickLocation[128];
	char mTrackballValue[128];
	
	TrackballTracker trackballer;

	ofxOscReceiver receiver;
	ofxOscSender sender;
	ofxOscSender websocketsSender;
	
	void arduinoTest();
	void recordMeasurements(ComponentTracker* comp, char* filename);
	
	int mFrameCounter;

	int getNumComponentsRegistered();

	void visualizePoint(ofPoint point, int r, int g, int b);
	
	int componentId;
};


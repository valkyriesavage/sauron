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
	ComponentTracker* getSauronComponent();
	void registerComponent(ComponentTracker*);

	void registerButton(ComponentTracker* ct);
	void registerSlider(ComponentTracker* ct);
	void registerDPad(ComponentTracker* ct);
	void registerDial(ComponentTracker* ct);
	void registerScrollWheel(ComponentTracker* ct);
	void registerJoystick(ComponentTracker* ct);

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

	float sliderProgress;
	float dialProgress;
	ComponentTracker::Direction scrollWheelDirection;
	bool buttonPressed;
	ofPoint	mJoystickLocation;

	ofxOscReceiver receiver;
	ofxOscSender sender;
};


#pragma once
#include <vector>
#include <stdio.h>
#include <string.h>

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

	bool isSauronRegistered();
	void sauronRegister();
	void sauronLoad();
	int assignSauronId();
	std::vector<ComponentTracker*> getSauronComponents();
	void registerComponent(ComponentTracker*);

	void registerButton(ComponentTracker* ct);
	void registerSlider(ComponentTracker* ct);
	void registerDPad(ComponentTracker* ct);
	void registerDial(ComponentTracker* ct);
	void registerScrollWheel(ComponentTracker* ct);

	int Sid;

	ofVideoGrabber 		vidGrabber;

	ofxCvColorImage			colorImg;

	ofxCvGrayscaleImage 	grayImage;
	ofxCvGrayscaleImage 	grayBg;
	ofxCvGrayscaleImage 	grayDiff;

	ofxCvContourFinder 	contourFinder;
	
	ofxCvContourFinder	contourFinderGrayImage;

	int 				threshold;
	bool				bLearnBakground;
	bool				registering;
	bool				isRegistered;
	bool				testing;//just since we are testing testing

	std::vector<ComponentTracker*> components;
	std::vector<ofxCvBlob> blobsCaptured;//assists with registration

	float sliderProgress;
	float dialProgress;
	ComponentTracker::Direction scrollWheelDirection;

	ofxOscReceiver receiver;
	ofxOscSender sender;
};


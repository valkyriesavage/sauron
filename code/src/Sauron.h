#pragma once
#include <vector.h>

#include "ofMain.h"
#include "ofxOpenCv.h"

#include "ComponentTracker.h"
#include "utilities.h"

class Sauron : public ofBaseApp{

	bool isRegistered;
	public:
		void setup();
		void update();
		void draw();
		
		void keyPressed(int key);
		void keyReleased(int key);
		void mouseMoved(int x, int y );
		void mouseDragged(int x, int y, int button);
		void mousePressed(int x, int y, int button);
		void mouseReleased(int x, int y, int button);
		void windowResized(int w, int h);
		void dragEvent(ofDragInfo dragInfo);
		void gotMessage(ofMessage msg);
	
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
	
		int Sid;

        ofVideoGrabber 		vidGrabber;

        ofxCvColorImage			colorImg;

        ofxCvGrayscaleImage 	grayImage;
		ofxCvGrayscaleImage 	grayBg;
		ofxCvGrayscaleImage 	grayDiff;

        ofxCvContourFinder 	contourFinder;

		int 				threshold;
		bool				bLearnBakground;
		bool registering;//true if in the process of registering

        std::vector<ComponentTracker*> components;
		std::vector<ofxCvBlob> blobsCaptured;//assists with registration

		float sliderProgress;
		float dialProgress;
};


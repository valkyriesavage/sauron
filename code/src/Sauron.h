#pragma once
#include <vector.h>

#include "ofMain.h"
#include "ofxOpenCv.h"

#include "ComponentTracker.h"
#include "utilities.h"

class Sauron : public ofBaseApp{

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

        ofVideoGrabber 		vidGrabber;

        ofxCvColorImage			colorImg;

        ofxCvGrayscaleImage 	grayImage;
		ofxCvGrayscaleImage 	grayBg;
		ofxCvGrayscaleImage 	grayDiff;

        ofxCvContourFinder 	contourFinder;

		int 				threshold;
		bool				bLearnBakground;

        std::vector<ComponentTracker*> components;
};


#include "testApp.h"

//--------------------------------------------------------------
void testApp::setup(){

	#if defined (_WIN32) // if we are on Valkyrie's computer, use the Eye camera rather than the FaceTime
		vidGrabber.setDeviceID(1);
	#endif

	#ifdef _USE_LIVE_VIDEO
        vidGrabber.setVerbose(true);
        vidGrabber.initGrabber(320,240);
	#else
        vidPlayer.loadMovie("fingers.mov");
        vidPlayer.play();
	#endif

    colorImg.allocate(320,240);
	grayImage.allocate(320,240);
	grayBg.allocate(320,240);
	grayDiff.allocate(320,240);

	bLearnBakground = true;
	threshold = 80;

	ComponentTracker button = ComponentTracker();
	button.comptype = ComponentTracker::button;
	button.regionOfInterest = CvRect();
	// TODO : give these nice values that actually work
	button.regionOfInterest.x = 0;
	button.regionOfInterest.y = 0;
	button.regionOfInterest.height = 100;
	button.regionOfInterest.width = 100;
	button.numBlobsNeeded = 2;

	ComponentTracker slider = ComponentTracker();
	slider.comptype = ComponentTracker::slider;
	slider.regionOfInterest = CvRect();
	// TODO : the same thing here
	slider.regionOfInterest.x = 0;
	slider.regionOfInterest.y = 0;
	slider.regionOfInterest.height = 200;
	slider.regionOfInterest.width = 600;
	slider.numBlobsNeeded = 2;
	
	components.push_back(button);
	components.push_back(slider);
}

//--------------------------------------------------------------
void testApp::update(){
	ofBackground(100,100,100);

    bool bNewFrame = false;

	#ifdef _USE_LIVE_VIDEO
       vidGrabber.update();
	   bNewFrame = vidGrabber.isFrameNew();
    #else
        vidPlayer.update();
        bNewFrame = vidPlayer.isFrameNew();
	#endif

	if (bNewFrame){

		#ifdef _USE_LIVE_VIDEO
            colorImg.setFromPixels(vidGrabber.getPixels(), 320,240);
	    #else
            colorImg.setFromPixels(vidPlayer.getPixels(), 320,240);
        #endif

        grayImage = colorImg;
		if (bLearnBakground == true){
			grayBg = grayImage;		// the = sign copys the pixels from grayImage into grayBg (operator overloading)
			bLearnBakground = false;
		}

		// take the abs value of the difference between background and incoming and then threshold:
		grayDiff.absDiff(grayBg, grayImage);
		grayDiff.threshold(threshold);

		for (list<ComponentTracker>::iterator it = components.begin(); it != components.end(); it++) {
			ComponentTracker cur = *it;
		
			// just look at the ROI for that component
			cvSetImageROI(grayDiff.getCvImage(), cur.regionOfInterest);
		
			// find contours which are between the size of 20 pixels and 1/3 the w*h pixels.
			// only considering the number of blobs needed for this input device.
			cur.contourFinder.findContours(grayDiff, 20, (340*240)/3, cur.numBlobsNeeded, false);	

			// decide if we saw something interesting
			if (cur.comptype == ComponentTracker::button && cur.buttonEventDetected(contourFinder)) {
					cout << "Holy crap you pushed the button" << endl;
			} else if (cur.comptype == ComponentTracker::slider) {
				int sliderPos;
				if (cur.sliderEventDetected(contourFinder, &sliderPos)) {
					cout << "Holy crap you slid the slider to " << sliderPos << endl;
				}
			} else if (cur.comptype == ComponentTracker::dial) {
				int dialPos;
				if (cur.dialEventDetected(contourFinder, &dialPos)) {
					cout << "Holy crap you rotated the dial to " << dialPos << endl;
				}
			}
		
		}
	}
}

//--------------------------------------------------------------
void testApp::draw(){

	// draw the incoming, the grayscale, the bg and the thresholded difference
	ofSetHexColor(0xffffff);
	colorImg.draw(20,20);
	grayImage.draw(360,20);
	grayBg.draw(20,280);
	grayDiff.draw(360,280);

	// then draw the contours:

	ofFill();
	ofSetHexColor(0x333333);
	ofRect(360,540,320,240);
	ofSetHexColor(0xffffff);

	// we could draw the whole contour finder
	//contourFinder.draw(360,540);

	// or, instead we can draw each blob individually,
	// this is how to get access to them:
    for (int i = 0; i < contourFinder.nBlobs; i++){
        contourFinder.blobs[i].draw(360,540);
    }

	// finally, a report:

	ofSetHexColor(0xffffff);
	char reportStr[1024];
	sprintf(reportStr, "bg subtraction and blob detection\npress ' ' to capture bg\nthreshold %i (press: +/-)\nnum blobs found %i, fps: %f", threshold, contourFinder.nBlobs, ofGetFrameRate());
	ofDrawBitmapString(reportStr, 20, 600);
}

//--------------------------------------------------------------
void testApp::keyPressed(int key){

	switch (key){
		case ' ':
			bLearnBakground = true;
			break;
		case '+':
			threshold ++;
			if (threshold > 255) threshold = 255;
			break;
		case '-':
			threshold --;
			if (threshold < 0) threshold = 0;
			break;
		case '0'://added by Colin to test out the setRoiFromPixels() method
			//lets get some information about the blob
			for (int i = 0; i < contourFinder.nBlobs; i++){
				//which of the numBlobs blobs we are measuring
				cout << "blobID: " << i <<endl;
				//area of the blob
				cout << "area: "<< contourFinder.blobs[i].area<<endl;
				//perimeter of the blob
				cout << "perimeter: "<< contourFinder.blobs[i].length<<endl;
				//ofPoint instance of center of blob
				cout << "centroid: "<< contourFinder.blobs[i].centroid<<endl;
				//number of points inside the blob
				cout << "nPts: "<< contourFinder.blobs[i].nPts<<endl;
				//vector of all of ofPoints in the blob
				cout << "points vector: ";
				for (int j = 0; j<contourFinder.blobs[i].pts.size(); j++) {
					cout << contourFinder.blobs[i].pts[j];
				}
				cout << endl;
			}
			break;
	}
}

//--------------------------------------------------------------
void testApp::keyReleased(int key){

}

//--------------------------------------------------------------
void testApp::mouseMoved(int x, int y ){

}

//--------------------------------------------------------------
void testApp::mouseDragged(int x, int y, int button){

}

//--------------------------------------------------------------
void testApp::mousePressed(int x, int y, int button){

}

//--------------------------------------------------------------
void testApp::mouseReleased(int x, int y, int button){

}

//--------------------------------------------------------------
void testApp::windowResized(int w, int h){

}

//--------------------------------------------------------------
void testApp::gotMessage(ofMessage msg){

}

//--------------------------------------------------------------
void testApp::dragEvent(ofDragInfo dragInfo){ 

}

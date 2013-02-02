#include "Sauron.h"

//--------------------------------------------------------------
void Sauron::setup(){

	#if defined (_WIN32) // if we are on Valkyrie's computer, use the Eye camera rather than the FaceTime
		vidGrabber.setDeviceID(1);
	#endif
    
    vidGrabber.setVerbose(true);
    vidGrabber.initGrabber(320,240);
	
    colorImg.allocate(320,240);
	grayImage.allocate(320,240);
	grayBg.allocate(320,240);
	grayDiff.allocate(320,240);

	bLearnBakground = true;
	threshold = 80;

	//check for registration
	if (!isSauronRegistered()) {
		sauronRegister();
	}else{
		sauronLoad();
	}
	ComponentTracker* button = new ComponentTracker();
	button->comptype = ComponentTracker::button;
    // TODO : give these nice values that actually work
	button->regionOfInterest = cvRect(0, 0, 100, 100);
	button->numBlobsNeeded = 2;

	ComponentTracker* slider = new ComponentTracker();
	slider->comptype = ComponentTracker::slider;
    // TODO : the same thing here
	slider->regionOfInterest = cvRect(0, 0, 200, 200);
	slider->numBlobsNeeded = 2;
    
    ComponentTracker* dpad = new ComponentTracker();
	dpad->comptype = ComponentTracker::dpad;
	// TODO : the same thing here
	dpad->regionOfInterest = cvRect(0, 0, 200, 200);
	dpad->numBlobsNeeded = 4;
    
    ComponentTracker* dial = new ComponentTracker();
	dial->comptype = ComponentTracker::dial;
	// TODO : the same thing here
	dial->regionOfInterest = cvRect(0, 0, 200, 200);
	dial->numBlobsNeeded = 2;
    
    ComponentTracker* scrollWheel = new ComponentTracker();
	scrollWheel->comptype = ComponentTracker::scroll_wheel;
	// TODO : the same thing here
	scrollWheel->regionOfInterest = cvRect(0, 0, 200, 200);
	scrollWheel->numBlobsNeeded = 2;
	
	components.push_back(button);
	components.push_back(slider);
    components.push_back(dpad);
    components.push_back(dial);
	components.push_back(scrollWheel);
}

//--------------------------------------------------------------
void Sauron::update(){
	ofBackground(100,100,100);

    bool bNewFrame = false;

	vidGrabber.update();
    bNewFrame = vidGrabber.isFrameNew();

	if (bNewFrame){

		colorImg.setFromPixels(vidGrabber.getPixels(), 320,240);
        
        grayBg.resetROI();
        colorImg.resetROI();
        grayImage.resetROI();
        grayDiff.resetROI();

        grayImage = colorImg;
		if (bLearnBakground == true){
			grayBg = grayImage;		// the = sign copys the pixels from grayImage into grayBg (operator overloading)
			bLearnBakground = false;
		}

		// take the abs value of the difference between background and incoming and then threshold:
		grayDiff.absDiff(grayBg, grayImage);
		grayDiff.threshold(threshold);

		for (int i = 0; i < components.size(); i++) {
			ComponentTracker* cur = components.at(i);
		
            if(cur->comptype == ComponentTracker::scroll_wheel) {
                // for the scroll wheel, we don't look at the difference.
                // since we can't get a background without stripes in it, we just
                // look at the raw image and look for blobs
                cvSetImageROI(grayImage.getCvImage(), cur->regionOfInterest);
                
                // find contours which are between the size of 20 pixels and 1/3 the w*h pixels.
                // only considering the number of blobs needed for this input device.
                cur->contourFinder.findContours(grayImage, 20, (340*240)/3, cur->numBlobsNeeded, false);
            } else {
                // just look at the ROI for that component, in the grey diff
                cvSetImageROI(grayDiff.getCvImage(), cur->regionOfInterest);
                
                // find contours which are between the size of 20 pixels and 1/3 the w*h pixels.
                // only considering the number of blobs needed for this input device.
                cur->contourFinder.findContours(grayDiff, 20, (340*240)/3, cur->numBlobsNeeded, false);
            }
	

			// decide if we saw something interesting
			if (cur->comptype == ComponentTracker::button && cur->buttonEventDetected()) {
					cout << "Holy crap you pushed the button" << endl;
			} else if (cur->comptype == ComponentTracker::slider) {
				int sliderPos;
				if (cur->sliderEventDetected(&sliderPos)) {
					cout << "Holy crap you slid the slider to " << sliderPos << endl;
				}
			} else if (cur->comptype == ComponentTracker::dial) {
				int dialPos;
				if (cur->dialEventDetected(&dialPos)) {
					cout << "Holy crap you rotated the dial to " << dialPos << endl;
				}
			} else if (cur->comptype == ComponentTracker::scroll_wheel) {
                ComponentTracker::Direction scrollDirection;
                int scrollAmount = 0;
                if (cur->scrollWheelEventDetected(&scrollDirection, &scrollAmount)) {
                    cout << "Holy crap you turned the scroll wheel ";
                    if (scrollDirection == ComponentTracker::up) {
                        cout << "up" << endl;
                    } else {
                        cout << "down" << endl;
                    }
                }
            } else if (cur->comptype == ComponentTracker::dpad) {
                ComponentTracker::Direction dpadDirection;
                if (cur->dpadEventDetected(&dpadDirection)) {
                    cout << "Holy crap you pushed the dpad ";
                    if (dpadDirection == ComponentTracker::up) {
                        cout << "up" << endl;
                    } else if (dpadDirection == ComponentTracker::down) {
                        cout << "down" << endl;
                    } else if (dpadDirection == ComponentTracker::right) {
                        cout << "right" << endl;
                    } else if (dpadDirection == ComponentTracker::left) {
                        cout << "left" << endl;
                    }
                }
            }
            
            // need to reset this so we can move it around for the next component
            cvResetImageROI(grayDiff.getCvImage());
            // we don't need to reset the ROI on the raw gray image, since we use
            // the same one every time
		}
	}
}

//--------------------------------------------------------------
void Sauron::draw(){
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

	// we draw the blobs
    for (int i = 0; i < components.size(); i++) {
        ComponentTracker* cur = components.at(i);
        cur->contourFinder.draw();
    }
	// finally, a report:

	ofSetHexColor(0xffffff);
	char reportStr[1024];
	sprintf(reportStr, "bg subtraction and blob detection\npress ' ' to capture bg\nthreshold %i (press: +/-)\nnum blobs found %i, fps: %f", threshold, contourFinder.nBlobs, ofGetFrameRate());
	ofDrawBitmapString(reportStr, 20, 600);
}

//--------------------------------------------------------------
void Sauron::keyPressed(int key){

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
void Sauron::keyReleased(int key){

}

//--------------------------------------------------------------
void Sauron::mouseMoved(int x, int y ){

}

//--------------------------------------------------------------
void Sauron::mouseDragged(int x, int y, int button){

}

//--------------------------------------------------------------
void Sauron::mousePressed(int x, int y, int button){

}

//--------------------------------------------------------------
void Sauron::mouseReleased(int x, int y, int button){

}

//--------------------------------------------------------------
void Sauron::windowResized(int w, int h){

}

//--------------------------------------------------------------
void Sauron::gotMessage(ofMessage msg){

}

//--------------------------------------------------------------
void Sauron::dragEvent(ofDragInfo dragInfo){ 

}


bool Sauron::isSauronRegistered(){
//do some local (or over web/websockets) to check whether this device has already been registered, in which case we can just load up the relevant information form our db
	//but for now, since we are making registration, we will just claim we are not registered
	return false;
}

void Sauron::sauronRegister(){
	if (isSauronRegistered()) {
		//hey man, you've already registered this. TODO: setup some error here
		return;
	}
	/*the function called in setup() is registration has not been performed.
	 function itself will perform a variety of operations to fultill registraion of this controller including:
	 *assigning it a unique id from the web
	 *getting information from (somewhere) with solidworks info (including which components are used, and how many, and maybe, as a bonus, relative positioninig (but we could do wihtout this))
	 *measuring and logging information (boundaries/detection setup) for the input components
	 */
	
}
void Sauron::sauronLoad(){

/* sauronLoad looks in our database and loads up the necessary knowhow for this controller to get going.
 */
}


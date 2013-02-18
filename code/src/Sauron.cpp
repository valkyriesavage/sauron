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

	isRegistered = false;//for testing purposes
	registering = false;
	int numComponents = 4;
	components.reserve(numComponents);

	sliderProgress = 0.0f;
	dialProgress = 0.0f;

	receiver.setup(PORT);
	sender.setup(HOST, PORT+1);
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
		contourFinder.findContours(grayDiff, 5, (340*240)/4, 4, false, true);

		float distanceFromCenter = 0.0f;
		std:vector<ofxCvBlob> dialBlobs;
		if (isRegistered){
			for (std::vector<ComponentTracker*>::iterator it = components.begin(); it != components.end(); ++it){
				ComponentTracker* c = *it;
				switch (c->comptype) {
				case ComponentTracker::slider:
					for (int i = 0; i < contourFinder.nBlobs; i++){
						//if the blob is in the area of the component lets keep it
						ofxCvBlob blob = contourFinder.blobs[i];
						if ((c->ROI).inside((blob.centroid))){
							float dfc =	c->calculateSliderProgress(blob);
							if (dfc > distanceFromCenter){
								sliderProgress = dfc;
								//calculate for the 'zero' case (slider in zero position
							}
						}
					}
					break;
				case ComponentTracker::dial:
					dialBlobs.clear();
					for (int i = 0; i < contourFinder.nBlobs; i++){
						//if the blob is in the area of the component lets keep it
						ofxCvBlob blob = contourFinder.blobs[i];
						if ((c->ROI).inside((blob.centroid))){
							dialBlobs.push_back(blob);
						}
					}
					dialProgress = c->calculateDialProgress(dialBlobs);
					break;
				default:
					break;
				}
			}

			if(testing) {
				// TODO COLIN : for each event you see in TEST MODE, please send it to me in SolidWorks!
				ofxOscMessage m;
				float valueOfSensorAfterAllThisTracking = 0.0f;
				m.setRemoteEndpoint(HOST, PORT+1);
				m.setAddress("/COMPONENTTYPE/ID");
				m.addFloatArg(valueOfSensorAfterAllThisTracking);
				sender.sendMessage(m);
			}
		}
	}

	while (receiver.hasWaitingMessages()) {
		ofxOscMessage msg;
		receiver.getNextMessage(&msg);

		char* address = new char[msg.getAddress().length() + 1];
		strcpy(address, msg.getAddress().c_str());
		string tld = string(strtok(address, "/")); // either "test" or a component name
		string command = msg.getArgAsString(0); // this will be either "done" or "start"

		if(tld.compare("test") == 0) {
			if(command.compare("start") == 0) {
				testing = true;
			} else {
				testing = false;
			}
		} else {

			int componentId = strtok(NULL, "/")[0] - '0';

			if(tld.compare("button") == 0) {
				if(command.compare("start") == 0) {
					// enter button registration mode for id componentId
					// NOTE COLIN : the id I am sending you is just what number button it is (i.e. buttons
					// will be numbered 0, 1, 2, ... ) these don't count up between components, unless you
					// need them to.  I mean, as it is, I will have a button 0 and a slider 0 if there is a
					// button and a slider in the design.
				} else {
					// exit button registration mode for id componentId
				}
			}

			if(tld.compare("slider") == 0) {
				if(command.compare("start") == 0) {
					// enter slider registration mode for id componentId
				} else {
					// exit slider registration mode for id componentId
				}
			}

			if(tld.compare("dial") == 0) {
				if(command.compare("start") == 0) {
					// enter dial registration mode for id componentId
				} else {
					// exit dial registration mode for id componentId
				}
			}

			if(tld.compare("dpad") == 0) {
				if(command.compare("start") == 0) {
					// enter dpad registration mode for id componentId
				} else {
					// exit dpad registration mode for id componentId
				}
			}

			if(tld.compare("scrollwheel") == 0) {
				if(command.compare("start") == 0) {
					// enter scrollwheel registration mode for id componentId
				} else {
					// exit scrollwheel registration mode for id componentId
				}
			}
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
	sprintf(reportStr, "bg subtraction and blob detection\npress ' ' to capture bg\nthreshold %i (press: +/-)\nnum blobs found %i, fps: %f\nslider completion percentage: %f\ndial completion angle: %f",
		threshold, contourFinder.nBlobs, ofGetFrameRate(), sliderProgress, dialProgress);
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
	case 'r':
		sauronRegister();
		break;
	}
}

/*
do some local (or over web/websockets) to check whether this device has already been registered, in which case we can just load up the relevant information form our db
TODO: finish this method
*/

bool Sauron::isSauronRegistered(){
	//but for now, since we are making registration, we will just claim we are not registered
	return isRegistered;
}

/*the function called in setup() is registration has not been performed.
function itself will perform a variety of operations to fultill registraion of this controller including:
- assigning it a unique id from the web
- getting information from (somewhere) with solidworks info (including which components are used, and how many, and maybe, as a bonus, relative positioninig (but we could do wihtout this))
- measuring and logging information (boundaries/detection setup) for the input components
*/
void Sauron::sauronRegister(){
	if (isSauronRegistered()) {
		//hey man, you've already registered this. TODO: setup some error here
		//		return; but for now, reregistration is possible
	}
	registering = true;
	//asign id
	Sid = assignSauronId();

	//get component information
	components = getSauronComponents();

	//register all the components
	for (std::vector<ComponentTracker*>::iterator it = components.begin(); it != components.end(); ++it){
		registerComponent(*it);
	}
	isRegistered = true;
	registering = false;
}

/* 
sauronLoad looks in our database and loads up the necessary knowhow for this controller to get going.
TODO: finish this method when we determine a way to save devices
*/
void Sauron::sauronLoad(){}

/*
assigns a unique id for Sauron devices
TODO: finish this method when determine a way to save devices
*/
int Sauron::assignSauronId(){
	return 0;//for now
}

/*
gets Sauron components from solidworks
*/
std::vector<ComponentTracker*> Sauron::getSauronComponents(){
	std::vector<ComponentTracker*> components;

	ComponentTracker* button = new ComponentTracker();
	button->comptype = ComponentTracker::button;
	// TODO : give these nice values that actually work
	//	button->regionOfInterest = cvRect(0, 0, 100, 100);
	//	button->numBlobsNeeded = 2;

	ComponentTracker* slider = new ComponentTracker();
	slider->comptype = ComponentTracker::slider;
	// TODO : the same thing here
	//	slider->regionOfInterest = cvRect(0, 0, 200, 200);
	//	slider->numBlobsNeeded = 2;

	ComponentTracker* dpad = new ComponentTracker();
	dpad->comptype = ComponentTracker::dpad;
	// TODO : the same thing here
	//	dpad->regionOfInterest = cvRect(0, 0, 200, 200);
	//	dpad->numBlobsNeeded = 4;

	ComponentTracker* dial = new ComponentTracker();
	dial->comptype = ComponentTracker::dial;
	// TODO : the same thing here
	//	dial->regionOfInterest = cvRect(0, 0, 200, 200);
	//	dial->numBlobsNeeded = 2;

	ComponentTracker* scrollWheel = new ComponentTracker();
	scrollWheel->comptype = ComponentTracker::scroll_wheel;
	// TODO : the same thing here
	//	scrollWheel->regionOfInterest = cvRect(0, 0, 200, 200);
	//	scrollWheel->numBlobsNeeded = 2;

	//components.push_back(button);
	//	components.push_back(slider);
	//    components.push_back(dpad);
	components.push_back(dial);
	//	components.push_back(scrollWheel);
	return components;
}

/*
depending on component type, does some gui bit for user to properly register this input component
TODO: finish this method. It's a switch statement between all the different component types. Each component type will have its own method.
*/
void Sauron::registerComponent(ComponentTracker* ct){
	switch (ct->comptype) {
	case ComponentTracker::button:
		//do some button business
		registerButton(ct);
		break;
	case ComponentTracker::slider:
		//do some slider business
		registerSlider(ct);
		break;
	case ComponentTracker::dpad:
		//do some dpad business
		registerDPad(ct);
		break;
	case ComponentTracker::dial:
		//do some dial business
		registerDial(ct);
		break;
	default:
		break;
	}
}

void Sauron::registerButton(ComponentTracker* ct){

}	

void Sauron::registerSlider(ComponentTracker* ct){
	//capture blob bounds (bgsubtraction should have already been set up with two blobs on either extreme side)
	// Ideally, we would not need to set up bg subtraction in this way:
	// we can just bg subtract the position when the user starts "capturing", then track the extreme sides.
	// we will get a message from solidworks of the form "/slider/1 start" and then there will be movement,
	// then we will get a message of the form "/slider/1/ done", and there won't be any more movement.
	// we can just track the max and min x,y values we see between these two messages to get the slider bounds
	std::vector<ofRectangle> componentBounds;
	for(int i = 0; i < contourFinder.nBlobs; i++) {
		ofxCvBlob blob = contourFinder.blobs.at(i);
		componentBounds.push_back(blob.boundingRect);
	}	
	//there should now be two blobs, capture both of them.
	//we should now have two points and we can do stuff with that.

	ct->setROI(componentBounds);
	ct->numBlobsNeeded = 2;
}

void Sauron::registerDPad(ComponentTracker* ct){

}

void Sauron::registerDial(ComponentTracker* ct){
	std::vector<ofRectangle> componentBounds;
	for(int i = 0; i < contourFinder.nBlobs; i++) {
		ofxCvBlob blob = contourFinder.blobs.at(i);
		componentBounds.push_back(blob.boundingRect);
	}		
	ct->setROI(componentBounds);
	ct->numBlobsNeeded = 2;
}

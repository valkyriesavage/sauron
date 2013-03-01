#include "Sauron.h"

	//--------------------------------------------------------------
void Sauron::setup(){
	#if defined (_WIN32) // if we are on Valkyrie's computer, use the Eye camera rather than the FaceTime
	vidGrabber.setDeviceID(1);
	#else
		vidGrabber.setDeviceID(3);//Colin likes using the marooned USB port
	#endif
	
	vidGrabber.setVerbose(true);
	vidGrabber.initGrabber(320,240);
	
	colorImg.allocate(320,240);
	grayImage.allocate(320,240);
	
	threshold = 190;//threshold is such to account for the next camera and brightness
	mNumBlobsConsidered = 10;//just an arbitrary number
	
	registering = false;
	int numComponents = 4;
	components.reserve(numComponents);
	
	sliderProgress = -1.0f;
	dialProgress = -1.0f;
	scrollWheelDirection = ComponentTracker::none;
	buttonPressed = false;
	
	receiver.setup(PORT);
	sender.setup(HOST, PORT+1);
	
	testing = false;
	
	currentRegisteringComponent = new ComponentTracker();
}

	//--------------------------------------------------------------
void Sauron::update(){
	ofBackground(100,100,100);
	
	bool bNewFrame = false;
	
	vidGrabber.update();
	bNewFrame = vidGrabber.isFrameNew();
	
	if (bNewFrame){
		
		colorImg.setFromPixels(vidGrabber.getPixels(), 320,240);
		
		colorImg.resetROI();
		grayImage.resetROI();
		
		grayImage = colorImg;

		
		grayImage.threshold(threshold);
		contourFinderGrayImage.findContours(grayImage, 5, (340*240)/4, mNumBlobsConsidered, false, true);
		
		if(registering){
			sauronRegister();
		}
		
		std:vector<ofxCvBlob> dialBlobs;

		for (std::vector<ComponentTracker*>::iterator it = components.begin(); it != components.end(); ++it){
			ComponentTracker* c = *it;
			if(c->isRegistered()){
				switch (c->comptype) {
					case ComponentTracker::slider:
						for (int i = 0; i < contourFinderGrayImage.nBlobs; i++){
							ofxCvBlob blob = contourFinderGrayImage.blobs[i];
							if ((c->ROI).inside((blob.centroid))){
								sliderProgress = c->calculateSliderProgress(blob);
							}
						}
						c->setDelta(sliderProgress);
						break;
					case ComponentTracker::dial:
						for (int i = 0; i < contourFinderGrayImage.nBlobs; i++){
							ofxCvBlob blob = contourFinderGrayImage.blobs[i];
							if ((c->ROI).inside((blob.centroid))){
								dialBlobs.push_back(blob);
							}
						}
						dialProgress = c->calculateDialProgress(dialBlobs);
						c->setDelta(dialProgress);
						break;
					case ComponentTracker::scroll_wheel:
						scrollWheelDirection = c->calculateScrollWheelDirection(contourFinderGrayImage.blobs);
						c->setDelta( (float) scrollWheelDirection);
						break;
					case ComponentTracker::button:
						buttonPressed = c->isButtonPressed(contourFinderGrayImage.blobs);
						c->setDelta( (float) buttonPressed);
						break;
					case ComponentTracker::joystick:
						mJoystickLocation = c->measureJoystickLocation(contourFinderGrayImage.blobs);
					//	c->setDelta( (ofPoint) mJoystickLocation);
					//TODO: set up how to send joystick info to SW
						break;
					default:
						
						break;
				}
			}
		}	
			if(testing) {
				for(std::vector<ComponentTracker*>::iterator it = components.begin();it != components.end(); ++it){		
					ComponentTracker* component = *it;
					string componentType = component->getComponentType();
					int id = component->id;
					float delta = component->getDelta();
					char idstr[21]; // enough to hold all numbers up to 64-bits
					sprintf(idstr, "%d", id);
					
					ofxOscMessage m;
					m.setRemoteEndpoint(HOST, PORT+1);
					m.setAddress(componentType + "/" + idstr);
					m.addFloatArg(delta);
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
					stageComponent(ComponentTracker::button, componentId);
					startRegistrationMode();
				} else {
						// exit button registration mode for id componentId
					stopRegistrationMode();
				}
			}
			
			if(tld.compare("slider") == 0) {
				if(command.compare("start") == 0) {
						// enter slider registration mode for id componentId
					stageComponent(ComponentTracker::slider, componentId);
					startRegistrationMode();
				} else {
						// exit slider registration mode for id componentId
					stopRegistrationMode();
				}
			}
			
			if(tld.compare("dial") == 0) {
				if(command.compare("start") == 0) {
						// enter dial registration mode for id componentId
					stageComponent(ComponentTracker::dial, componentId);
					startRegistrationMode();
				} else {
						// exit dial registration mode for id componentId
					stopRegistrationMode();
				}
			}
			
			if(tld.compare("dpad") == 0) {
				if(command.compare("start") == 0) {
						// enter dpad registration mode for id componentId
					stageComponent(ComponentTracker::dpad, componentId);
					startRegistrationMode();
				} else {
						// exit dpad registration mode for id componentId
					stopRegistrationMode();
				}
			}
			
			if(tld.compare("scrollwheel") == 0) {
				if(command.compare("start") == 0) {
						// enter scrollwheel registration mode for id componentId
					stageComponent(ComponentTracker::scroll_wheel, componentId);
					startRegistrationMode();
				} else {
						// exit scrollwheel registration mode for id componentId
					stopRegistrationMode();
				}
			}
		}
	}
}

	//--------------------------------------------------------------
void Sauron::draw(){
		//draw each image (color and gray)
	colorImg.draw(20,20);
	grayImage.draw(360,20);

		//draw the whole contour finder
	contourFinderGrayImage.draw(360,20);
	
	
		// finally, a report:
	ofSetHexColor(0xffffff);
	char reportStr[1024];
	sprintf(reportStr, "bg subtraction and blob detection\npress ' ' to capture bg\nthreshold %i (press: +/-)\nnum blobs found %i, fps: %f\nis Registering? %d\nSlider completion percentage: %f\nDial completion angle: %f\nScroll Wheel Direction: %i\nButton Pressed: %d\nJoystick Location: (%f, %f)",
			threshold, contourFinderGrayImage.nBlobs, ofGetFrameRate(), registering, sliderProgress, dialProgress, scrollWheelDirection, buttonPressed, mJoystickLocation.x, mJoystickLocation.y);
	ofDrawBitmapString(reportStr, 20, 600);
}


	//--------------------------------------------------------------
void Sauron::keyPressed(int key){
	
	switch (key){
		case '+':
			threshold ++;
			if (threshold > 255) threshold = 255;
			break;
		case '-':
			threshold --;
			if (threshold < 0) threshold = 0;
			break;
		case 'r':
				stageComponent(getSauronComponent()->comptype, getSauronComponent()->id);
				startRegistrationMode();
			break;
		case 'd':
			stopRegistrationMode();
			break;
	}
}
/*
 stageComponent takes preparatory information from solidworks and stages it to be registered with sauronRegister().
 */
void Sauron::stageComponent(ComponentTracker::ComponentType type, int id){
	if(!registering){
		currentRegisteringComponent = new ComponentTracker();
		currentRegisteringComponent->id = id;
		currentRegisteringComponent->comptype = type;
	}
}

/*
 startRegistrationMode() begins registration of the staged component
 */
void Sauron::startRegistrationMode(){
	registering = true;
}
/*
 stopRegistrationMode() is called after the registration is finished. At this point, the staged component is unstaged and added to the components vector.
 */
void Sauron::stopRegistrationMode(){
	currentRegisteringComponent->finalizeRegistration();
	components.push_back(currentRegisteringComponent);
	currentRegisteringComponent = new ComponentTracker();
	registering = false;
}

/*
 sauronRegister() is called in update() repeatedly (implicitly by being called in update()) continually redrawing the ROI for the staged component.
 */
void Sauron::sauronRegister(){
	if (currentRegisteringComponent->isRegistered()) {
			//hey man, you've already registered this. TODO: setup some error here
			//		return; but for now, reregistration is possible
	}else{
		registerComponent(currentRegisteringComponent);
	}
}

/* 
 sauronLoad looks in our database and loads up the necessary knowhow for this controller to get going.
 TODO: finish this method when we determine a way to save devices
 */
void Sauron::sauronLoad(){}

/*
 getSauronComponent currently simulates the action of receiving a component from solidworks
 */
ComponentTracker* Sauron::getSauronComponent(){
	ComponentTracker* component;
	
	ComponentTracker* button = new ComponentTracker();
	button->comptype = ComponentTracker::button;
	button->id = 0;
	
	ComponentTracker* slider = new ComponentTracker();
	slider->comptype = ComponentTracker::slider;
	slider->id = 0;
	
	ComponentTracker* dpad = new ComponentTracker();
	dpad->comptype = ComponentTracker::dpad;
	dpad->id = 0;
	
	ComponentTracker* dial = new ComponentTracker();
	dial->comptype = ComponentTracker::dial;
	dial->id = 0;
	
	ComponentTracker* scrollWheel = new ComponentTracker();
	scrollWheel->comptype = ComponentTracker::scroll_wheel;
	scrollWheel->id = 0;
	
	ComponentTracker* joystick = new ComponentTracker();
	joystick->comptype = ComponentTracker::joystick;
	joystick->id = 0;
	
	//component = button;
	//component = slider;
		//    component = dpad;
//		component = dial;
		//component = scrollWheel;
	component = joystick;
	return component;
}

/*
 depending on component type, does some gui bit for user to properly register this input component
 */
void Sauron::registerComponent(ComponentTracker* ct){
	switch (ct->comptype) {
		case ComponentTracker::button:
			registerButton(ct);
			break;
		case ComponentTracker::slider:
			registerSlider(ct);
			break;
		case ComponentTracker::dpad:
			registerDPad(ct);
			break;
		case ComponentTracker::dial:
			registerDial(ct);
			break;
		case ComponentTracker::scroll_wheel:
			registerScrollWheel(ct);
			break;
		case ComponentTracker::joystick:
			registerJoystick(ct);
			break;
		default:
			break;
	}
}

void Sauron::registerButton(ComponentTracker* ct){
	ct->setROI(contourFinderGrayImage.blobs);
	ct->numBlobsNeeded = 1;
}	

void Sauron::registerSlider(ComponentTracker* ct){
	ct->setROI(contourFinderGrayImage.blobs);
	ct->numBlobsNeeded = 1;
}

void Sauron::registerDPad(ComponentTracker* ct){
	
}

void Sauron::registerDial(ComponentTracker* ct){	
	ct->setROI(contourFinderGrayImage.blobs);
	ct->numBlobsNeeded = 1;
}

void Sauron::registerScrollWheel(ComponentTracker* ct){
	ct->setROI(contourFinderGrayImage.blobs);
	ct->numBlobsNeeded = 3;//pending
}

void Sauron::registerJoystick(ComponentTracker* ct){
	ct->setROI(contourFinderGrayImage.blobs);
	ct->numBlobsNeeded = 3;
}
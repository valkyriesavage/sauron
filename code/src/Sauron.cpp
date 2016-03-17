#include "Sauron.h"

	//--------------------------------------------------------------
void Sauron::setup(){
	#if defined (_WIN32) // if we are on Valkyrie's computer, pull in the test library
		vidGrabber.setDeviceID(1);
	this->PRINTED_IN_WHITE = true;

	#else
		vidGrabber.setDeviceID(1);//Colin likes using the marooned USB port
	#endif

	vidGrabber.setVerbose(true);
	vidGrabber.initGrabber(320,240);

	colorImg.allocate(320,240);
	grayImage.allocate(320,240);
	prevImage.allocate(320,420);

	threshold = 174;//threshold is such to account for the new camera and brightness
	mNumBlobsConsidered = 10;//just an arbitrary number high enough to capture all components

	registering = false;
	int numComponents = 4;
	components.reserve(numComponents);

	const char* notRegistered = "not registered";

	sprintf(mSliderProgress, "%s", notRegistered);
	sprintf(mDialProgress,"%s", notRegistered);
	sprintf(mScrollWheelDirection, "%s", notRegistered);
	sprintf(mButtonPressed,"%s", notRegistered);
	sprintf(mJoystickLocation, "%s", notRegistered);
	sprintf(mDpadDirection, "%s", notRegistered);
	sprintf(mTrackballValue, "%s", notRegistered);

	receiver.setup(RECEIVE_PORT);
	sender.setup(HOST, SEND_PORT);
	websocketsSender.setup(HOST, WEBSOCKETS_PORT);

	testing = false;

	currentRegisteringComponent = new ComponentTracker();

		// if mArduinoTest is set to true, every registered component will write it's deltas to a
		//file titled comptype+id + ".txt" (e.g. slider0.txt)
	mArduinoTest = false;

	mFrameCounter = 0;

	componentId = 0;
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
		int minAreaOfContours = 20;

		if(PRINTED_IN_WHITE)
			grayImage.invert();

		grayImage.threshold(threshold);
		contourFinderGrayImage.findContours(grayImage, minAreaOfContours, (340*240)/4, mNumBlobsConsidered, false, true);


		if(registering){
			sauronRegister();
		}
			//set progress amounts for drawing from each registered component
		for (std::vector<ComponentTracker*>::iterator it = components.begin(); it != components.end(); ++it){
			ComponentTracker* c = *it;
			if(c->isRegistered()){
				c->measureComponent(contourFinderGrayImage.blobs);
				// TODO : consider running the optical flow here
				// trackballer.getTrackballFlow(colorImg, greyImg, prevImg, c->ROI);

				switch (c->getComponentType()) {
					case ComponentTracker::slider:
						sprintf(mSliderProgress, "%s", c->getDelta().c_str());
						break;
					case ComponentTracker::dial:
						sprintf(mDialProgress, "%s", c->getDelta().c_str());
						break;
					case ComponentTracker::scroll_wheel:
						sprintf(mScrollWheelDirection,"%s", c->getDelta().c_str());
						break;
					case ComponentTracker::button:
						sprintf(mButtonPressed, "%s", c->getDelta().c_str());
						break;
					case ComponentTracker::dpad:
						sprintf(mDpadDirection, "%s", c->getDelta().c_str());
						break;
					case ComponentTracker::joystick:
						sprintf(mJoystickLocation, "%s", c->getDelta().c_str());
						break;
					case ComponentTracker::trackball:
						sprintf(mTrackballValue, "%s", c->getDelta().c_str());
						break;
					default:
						break;
				}

			}
		}

		if (mArduinoTest && !components.empty()) {
			arduinoTest();
		}

		if(testing) {
			for(std::vector<ComponentTracker*>::iterator it = components.begin();it != components.end(); ++it){
				ComponentTracker* component = *it;
				if (!component->isDeltaSignificant()){
					continue;
				}

				string componentType = component->getComponentTypeString();
				int id = component->getId();
				string delta = component->getDelta();
				float deltaFloat = 0;
				char idstr[21]; // enough to hold all numbers up to 64-bits
				sprintf(idstr, "%d", id);


				// this is what we send to SolidWorks or OSCulator
				ofxOscMessage m;
				m.setRemoteEndpoint(HOST, SEND_PORT);
				m.setAddress("/" + componentType + "/" + idstr);
				// ok, well, this is a little annoying
				switch(component->comptype) {
					case ComponentTracker::button:
						if(strcmp(delta.c_str(), "1") == 0) {
							deltaFloat = 1.0f;
						}
						break;
					case ComponentTracker::scroll_wheel:
						deltaFloat = .5;
						if(strcmp(delta.c_str(), "up") == 0) {
							deltaFloat = .7;
						} if (strcmp(delta.c_str(), "down") == 0) {
							deltaFloat = .3;
						}
						break;
					case ComponentTracker::dpad:
						if(strcmp(delta.c_str(), "up")) {
							deltaFloat = 1;
						} if (strcmp(delta.c_str(), "down")) {
							deltaFloat = -1;
						}  if (strcmp(delta.c_str(), "right")) {
							deltaFloat = .5;
						} if (strcmp(delta.c_str(), "left")) {
							deltaFloat = -.5;
						}
						break;
					case ComponentTracker::dial: case ComponentTracker::slider:
						deltaFloat = ::atof(delta.c_str());
						break;
					case ComponentTracker::trackball:
						/*string xDiff = split(split(delta.c_str, '(').at(1), ',').at(0);
						string yDiff = split(split(delta.c_str, ',').at(1), ')').at(0);
						m.setAddress("/" + componentType + "/" + idstr + "/X");
						m.addFloatArg(::atof(xDiff.c_str()));
						ofxOscMessage m2;
						m2.setRemoteEndpoint(HOST, SEND_PORT);
						m2.setAddress("/" + componentType + "/" + idstr + "/Y");
						m2.addFloatArg(::atof(yDiff.c_str()));
						sender.sendMessage(m2);*/
						break;
                    case ComponentTracker::joystick:
                        /*string xDiff = split(split(delta.c_str, '(').at(1), ',').at(0);
                         string yDiff = split(split(delta.c_str, ',').at(1), ')').at(0);
                         m.setAddress("/" + componentType + "/" + idstr + "/X");
                         m.addFloatArg(::atof(xDiff.c_str()));
                         ofxOscMessage m2;
                         m2.setRemoteEndpoint(HOST, SEND_PORT);
                         m2.setAddress("/" + componentType + "/" + idstr + "/Y");
                         m2.addFloatArg(::atof(yDiff.c_str()));
                         sender.sendMessage(m2);*/
                        break;
                    case ComponentTracker::no_component:
                        break;
				}
				m.addFloatArg(deltaFloat);
				sender.sendMessage(m);

				cout << "sending " << deltaFloat << " on " << "/" << componentType << "/" << idstr << endl;

				// this is what we send to websockets
				ofxOscMessage n;
				n.setRemoteEndpoint(HOST, WEBSOCKETS_PORT);
				n.setAddress("/" + componentType + "/" + idstr);
				n.addStringArg(delta);
				websocketsSender.sendMessage(n);
			}
		}

		prevImage = grayImage;

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

			if(tld.compare("trackball") == 0) {
				if(command.compare("start") == 0) {
						// enter trackball registration mode for id componentId
					stageComponent(ComponentTracker::trackball, componentId);
					startRegistrationMode();
				} else {
						// exit trackball registration mode for id componentId
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

	if(registering){
		ofPushView();
		ofSetHexColor(0xf7b82b);
		ofNoFill();
		ofTranslate(360, 20);
		ofRect(currentRegisteringComponent->getROI());
		ofPopView();
	}

	ofPushView();
		//draw all the ROIs

	ofTranslate(360, 20);
	for(std::vector<ComponentTracker*>::iterator it = components.begin();it != components.end(); ++it){
		ComponentTracker* c = *it;
		ofSetHexColor(0xffff00);
		ofNoFill();
		ofRect(c->getROI());

		// now, more special-casing.  what we actually want is to have a draw() function per componentTracker, but
		// now there's no time!

		switch(c->comptype) {
		case ComponentTracker::joystick:
			visualizePoint(c->joystickFlankStart, 255,0,0);
			visualizePoint(c->joystickFlankEnd, 0,255,0);
			visualizePoint(c->joystickMiddleStart, 255,255,0);
			visualizePoint(c->joystickMiddleEnd, 0,255,255);
			break;

		case ComponentTracker::slider:
			visualizePoint(c->sliderStart, 255,0,0);
			visualizePoint(c->sliderEnd, 0,255,0);
			break;

		case ComponentTracker::dial:
			ofNoFill();
			ofEllipse(c->ROI.x + c->ROI.width/2, c->ROI.y + c->ROI.height/2, c->ROI.width, c->ROI.height);
			break;

		case ComponentTracker::trackball:
			{
				ofSetLineWidth(5);
				ofSetColor(255,0,0);
				ofLine(c->ROI.x + c->ROI.width/2, c->ROI.y + c->ROI.height/2, c->ROI.x + c->ROI.width/2 + c->trackballXDirection.x*20, c->ROI.y + c->ROI.height/2 + c->trackballXDirection.y*20);
				ofSetColor(0,0,255);
				ofLine(c->ROI.x + c->ROI.width/2, c->ROI.y + c->ROI.height/2, c->ROI.x + c->ROI.width/2 + c->trackballYDirection.x*20, c->ROI.y + c->ROI.height/2 + c->trackballYDirection.y*20);
				ofSetColor(0,255,0);
				ofSetLineWidth(1);
				if(c->trackballCenterBlob > -1) {
					ofRectangle centreBlobBound = contourFinderGrayImage.blobs.at(c->trackballCenterBlob).boundingRect;
					ofRect(centreBlobBound.x, centreBlobBound.y, centreBlobBound.width, centreBlobBound.height);
				}
			}
				break;

		default:
			break;
		}
	}

	ofPopView();//don't keep the ROI drawing settings

		// finally, a report:
	ofSetHexColor(0xffffff);
	char reportStr[1024];
	const char* ctype = currentRegisteringComponent->getComponentTypeString().c_str();
	sprintf(reportStr, "You are currently registering: %s\nthreshold %i (press: +/-)\nnum blobs found %i, fps: %f\nSlider completion percentage: %s\nDial completion angle: %s\nScroll Wheel Direction: %s\nButton Pressed: %s\nJoystick Location: %s\nDpad Direction: %s\nTrackball value: %s",
			ctype, threshold, contourFinderGrayImage.nBlobs, ofGetFrameRate(), mSliderProgress, mDialProgress, mScrollWheelDirection, mButtonPressed, mJoystickLocation, mDpadDirection, mTrackballValue);
	ofDrawBitmapString(reportStr, 20, 280);
}

void Sauron::visualizePoint(ofPoint point, int r, int g, int b) {
	ofSetColor(r,g,b);
	ofFill();
	ofEllipse(point.x, point.y, 20, 20);
}


	//--------------------------------------------------------------
	//keyPressed allows us to simulate receieved OSC messages for registering components
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
		case '1': case 'l':
			stageComponent(ComponentTracker::slider, componentId++);
			startRegistrationMode();
			break;
		case '2': case 's':
			stageComponent(ComponentTracker::scroll_wheel, componentId++);
			startRegistrationMode();
			break;
		case '3': case 'd':
			stageComponent(ComponentTracker::dial, componentId++);
			startRegistrationMode();
			break;
		case '4': case 'j':
			stageComponent(ComponentTracker::joystick, componentId++);
			startRegistrationMode();
			break;
		case '5': case 'p':
			stageComponent(ComponentTracker::dpad, componentId++);
			startRegistrationMode();
			break;
		case '6': case 'b':
			stageComponent(ComponentTracker::button, componentId++);
			startRegistrationMode();
			break;
		case '7': case 'r':
			stageComponent(ComponentTracker::trackball, componentId++);
			startRegistrationMode();
			break;
		case 't':
			testing = !testing;
			break;
		case 'm': {
			ofxOscMessage m;
			m.setRemoteEndpoint(HOST, SEND_PORT);
			m.setAddress("/button/1");
			m.addFloatArg(1);
			sender.sendMessage(m);
			break;
		}
		case 'w': {
			ofxOscMessage n;
			n.setRemoteEndpoint(HOST, WEBSOCKETS_PORT);
			n.setAddress("/button/1");
			n.addStringArg("true");
			websocketsSender.sendMessage(n);
			break;
		}
		case '0':
			if (registering) {
				stopRegistrationMode();
			}
			resetSauron();
			break;
		case ' ':
			stopRegistrationMode();
			break;
		case 'c':
			stopRegistrationMode();
			components.pop_back();
			break;
		case '\\':
			this->PRINTED_IN_WHITE = !this->PRINTED_IN_WHITE;
			break;
	}
}
/*
 stageComponent takes preparatory information from solidworks and stages it to be registered with sauronRegister().
 */
void Sauron::stageComponent(ComponentTracker::ComponentType type, int id){
	if(!registering && type!=ComponentTracker::no_component){
		currentRegisteringComponent = new ComponentTracker(type, id);
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
			//TODO: reregistration is possible since we don't actually check with Sauron whether this component is registered (we are checking the component which doesn't really make sense).
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
 depending on component type, does some gui bit for user to properly register this input component
 */
void Sauron::registerComponent(ComponentTracker* ct){
	ct->setROI(contourFinderGrayImage.blobs);
}

/*
 *resetRegistration removes all registered components
 */
void Sauron::resetSauron(){
	components.clear();
}
/*
 * arduinoTest writes the deltas of any registered components during every update().
 * file manages our delta test files (overwritting when appropriate, appending when appropriate)
 */
void Sauron::arduinoTest(){
	if(components.empty()){
		return;
	}
	for(std::vector<ComponentTracker*>::size_type i = 0; i != components.size(); i++) {
	char s[128];
	sprintf(s, "%s%d%s", components[i]->getComponentTypeString().c_str(), components[i]->getId(), ".csv");
	if (mFrameCounter == 0) {
		remove(s);
	}
	recordMeasurements(components[i], s);
	}
	mFrameCounter++;
}
/*
 *writes measurements to file
 */
void Sauron::recordMeasurements(ComponentTracker* comp, char* filename){
	ofstream myfile;
	myfile.open (filename, ios::app);
	myfile << mFrameCounter << ", " << comp->getDelta() << endl;
	myfile.close();

}
/*
 *numComponentsRegistered returns the number of registered components
 */
int Sauron::getNumComponentsRegistered(){
	return components.size();
}

#include "ComponentTracker.h"

ComponentTracker::ComponentTracker() {
	id = -1;
	mIsRegistered = false;
	ROI = ofRectangle();
	mThreshold = 10.0f;//just a guess
	comptype = ComponentTracker::no_component;
}

ComponentTracker::ComponentTracker(ComponentType type, int id){
	mIsRegistered = false;
	ROI = ofRectangle();
	mThreshold = 5.0f;//just a guess
	
	this->id = id;
	this->comptype = type;
	
	switch(type){
		case ComponentTracker::button:
			numBlobsNeeded = 1;
			break;
		case ComponentTracker::slider:
			numBlobsNeeded = 1;
			break;
		case ComponentTracker::dpad:
			numBlobsNeeded = 4;
			break;
		case ComponentTracker::dial:
			numBlobsNeeded = 1;
			break;
		case ComponentTracker::scroll_wheel:
			numBlobsNeeded = 3;
			mPreviousScrollWheelDirection = ComponentTracker::none;
			break;
		case ComponentTracker::joystick:
			numBlobsNeeded = 3;
			break;
		default:
			break;
	}
	

}
	
string ComponentTracker::getComponentType(){
	switch (this->comptype) {
		case ComponentTracker::button:
			return "button";
			break;
		case ComponentTracker::slider:
			return "slider";
			break;
		case ComponentTracker::dial:
			return "dial";
			break;
		case ComponentTracker::joystick:
			return "joystick";
			break;
		case ComponentTracker::dpad:
			return "dpad";
			break;
		case ComponentTracker::scroll_wheel:
			return "scroll_wheel";
			break;
		case ComponentTracker::no_component:
			return "no_component";
			break;
		default:
			return "unknown_component";
			break;
	}
}

const char* ComponentTracker::EnumDirectionToString(ComponentTracker::Direction dir){
switch (dir) {
	case ComponentTracker::up:
		return "up";
		break;
	case ComponentTracker::down:
		return "down";
		break;
	case ComponentTracker::left:
		return "left";
		break;
	case ComponentTracker::right:
		return "right";
		break;
	case ComponentTracker::none:
		return "none";
		break;
	default:
		break;
}	
}

bool ComponentTracker::isRegistered(){
	return mIsRegistered;
}

void ComponentTracker::finalizeRegistration(){
	mIsRegistered = true;
}

bool ComponentTracker::buttonEventDetected() {
	return this->contourFinder.blobs.size() > 0;
}

bool ComponentTracker::sliderEventDetected(int* sliderPosition) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	*sliderPosition = this->contourFinder.blobs.at(0).centroid.distance(contourFinder.blobs.at(1).centroid) / this->regionOfInterest.width;
	return true;
}

bool ComponentTracker::dialEventDetected(int* dialPosition) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	// get the dial position : we have a triangle with one side as half the rectangle (from the edge to the
	// center of the circle) and one side as the distance from the center of the circle to the moving blob.
	// so we need to find angle between the adjacent side and hypotenuse : thus arccos
	*dialPosition = acos((this->regionOfInterest.width/2)/contourFinder.blobs.at(0).centroid.distance(centerOf(this->regionOfInterest)) * 180/PI);
	return true;
}

bool ComponentTracker::scrollWheelEventDetected(Direction* scrollDirection, int* scrollAmount) {
    if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
        return false;
    }
    if (this->previousBlobs.empty()) {
        this->previousBlobs = this->contourFinder.blobs;
        return false;
    }
    // determine whether the blobs have moved generally right or up or generally left or down
    int xDiff = this->previousBlobs.at(0).centroid.x - this->contourFinder.blobs.at(0).centroid.x;
    int yDiff = this->previousBlobs.at(0).centroid.y - this->contourFinder.blobs.at(0).centroid.y;
    if(xDiff > 0) {
        *scrollDirection = ComponentTracker::up;
    } else {
        *scrollDirection = ComponentTracker::down;
    }
    *scrollAmount = (int)this->previousBlobs.at(0).centroid.distance(this->contourFinder.blobs.at(0).centroid);
    return true;
}

bool ComponentTracker::dpadEventDetected(Direction* direction) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
    // determine whether the center of the four blobs' centers is off-center
    double xCenter = this->regionOfInterest.x + this->regionOfInterest.width/2;
    double yCenter = this->regionOfInterest.y + this->regionOfInterest.height/2;
    
    double xCenterBlobs = 0;
    double yCenterBlobs = 0;
    for (int i=0; i < this->contourFinder.blobs.size(); i++) {
        xCenterBlobs += this->contourFinder.blobs.at(i).centroid.x;
        yCenterBlobs += this->contourFinder.blobs.at(i).centroid.y;
    }
    xCenterBlobs = xCenterBlobs/this->contourFinder.blobs.size();
    yCenterBlobs = yCenterBlobs/this->contourFinder.blobs.size();
    
    if(xCenterBlobs < xCenter) {
        if(yCenterBlobs < yCenter) {
            *direction = ComponentTracker::up;
        } else {
            *direction = ComponentTracker::right;
        }
    } else {
        if(yCenterBlobs < yCenter) {
            *direction = ComponentTracker::down;
        } else {
            *direction = ComponentTracker::left;
        }
    }

	return true;
}

bool ComponentTracker::joystickEventDetected(int* xPosition, int* yPosition) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
    // TODO : finish this.  I don't have a printed joystick but can guess at stuff.
	*xPosition = 0;
	*yPosition = 0;
	return false;
}

void ComponentTracker::setROI(std::vector<ofxCvBlob> blobs){
		//ok save all in previous blobs and replace it every time. the blob that changes the most beyond a threshhold is the one we are interested in.
		//if not set
	if (previousBlobs.empty()) {
		previousBlobs = blobs;
		return;
	}
	if(ROIUnset()){
			//if movement is beyond movement threshhold
			//make a box around the previous and this blobs and set it as roi
		ofxCvBlob* blob = new ofxCvBlob();
		if (getFarthestDisplacedBlob(blob, previousBlobs, blobs, mThreshold)) {
			ROI = blob->boundingRect;
		}
	}else{
		ofxCvBlob* blob = new ofxCvBlob();
		if (getFarthestDisplacedBlob(blob, previousBlobs, blobs, mThreshold)) {
			std::vector<ofRectangle> vect;
			vect.push_back(ROI);
			vect.push_back(blob->boundingRect);
			ROI = makeBoundingBox(vect);

		}
	}
	previousBlobs = blobs;
}

float ComponentTracker::calculateSliderProgress(std::vector<ofxCvBlob> blobs){
	
	std::vector<ofxCvBlob> sliderBlobs;
	sliderBlobs = keepInsideBlobs(blobs);
	if (sliderBlobs.size() != numBlobsNeeded) {
		cout<<"error blobs passed into calculateDialProgress: " << sliderBlobs.size() << endl;
		return -1.0f;
	} 
	ofxCvBlob blob;
	blob = sliderBlobs.front();
	
	return	distanceFormula(ROI.x, ROI.y, blob.centroid.x, blob.centroid.y)/max(ROI.height, ROI.width);
}

/*
 calculateDialProgress() returns the angle between the two blobs (which are points on the 'circumference' of the ROI (ROI is still a rectangle
 so 'circumference' isn't technically correct, but it's close enough for our purposes)). 
 The first blob is the 'initial point' from the background subtraction and the latter is the moving part.
 Formula from http://math.stackexchange.com/questions/185829/how-do-you-find-an-angle-between-two-points-on-the-edge-of-a-circle
 */
float ComponentTracker::calculateDialProgress(std::vector<ofxCvBlob> blobs){
	
	std::vector<ofxCvBlob> dialBlobs = keepInsideBlobs(blobs);
	if (dialBlobs.size() != numBlobsNeeded) {
		cout<<"error blobs passed into calculateDialProgress: " << dialBlobs.size() << endl;
		return -1.0f;
	} 
	ofPoint p1 = dialBlobs[0].centroid;
	ofPoint p2 = ROI.getCenter();
	float r = ROI.width/2;
	p2.y = p2.y + r;

	return acos((2*pow(r, 2)- pow(distanceFormula(p1.x, p1.y, p2.x, p2.y), 2))/(2*pow(r, 2)));
	
}

ComponentTracker::Direction ComponentTracker::calculateScrollWheelDirection(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> scrollBlobs = keepInsideBlobs(blobs);
	
	if (scrollBlobs.size() > numBlobsNeeded) {
		cout<<"error blobs passed into calculateScrollWheelDirection: " << scrollBlobs.size() << endl;
		return ComponentTracker::none;
	}else if (scrollBlobs.size() == 1 && scrollBlobs[0].area/ROI.getArea() > 0.5) {
		return mPreviousScrollWheelDirection;
	}
	
	float meanDistance = 0.0f;
	float totalDistance = 0.0f;
	int movementThreshhold = 1;//based on empirical evidence. 
	
	for(int i = 0; i < min(scrollBlobs.size(), previousBlobs.size()); i++) {
		float xDisp = scrollBlobs[i].centroid.x - previousBlobs[i].centroid.x;
		float yDisp = scrollBlobs[i].centroid.y - previousBlobs[i].centroid.y;
		if (((abs(xDisp) > abs(yDisp)) && xDisp > 0) || ((abs(yDisp) > abs(xDisp)) && yDisp > 0)) {//yes I know, hacky.
			totalDistance += distanceFormula(scrollBlobs[i].centroid.x, scrollBlobs[i].centroid.y, previousBlobs[i].centroid.x, previousBlobs[i].centroid.y);
			//I guess we don't technically need to do this here, but it'll set up nicely for measuring scrollwheel movement later.
		}else {
			totalDistance -= distanceFormula(scrollBlobs[i].centroid.x, scrollBlobs[i].centroid.y, previousBlobs[i].centroid.x, previousBlobs[i].centroid.y);
		}
	}
	
	meanDistance =  totalDistance /previousBlobs.size();	
	
	this->previousBlobs = scrollBlobs;//gotta replace our previous blobs
	
	if (meanDistance >movementThreshhold) {//for now, 'up' is reletively defined. 
		mPreviousScrollWheelDirection = ComponentTracker::up;
		return ComponentTracker::up;
	}else if (meanDistance < -movementThreshhold) {
		mPreviousScrollWheelDirection = ComponentTracker::down;
		return ComponentTracker::down;
	}else {

		return ComponentTracker::none;
	}
}

bool ComponentTracker::isButtonPressed(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> buttonBlobs = keepInsideBlobs(blobs);
	
	float precisionThreshhold = 0.5f;
	if (buttonBlobs.size() !=numBlobsNeeded) {
		cout<<"error blobs passed into isButtonPressed: " << buttonBlobs.size() << endl;
		return false;
	} 

	if(buttonBlobs[0].area/ROI.getArea() > 1-precisionThreshhold && buttonBlobs[0].area/ROI.getArea() < 1+precisionThreshhold){
		return true;
	}else{
		return false;
	}
}

ComponentTracker::Direction ComponentTracker::calculateDpadDirection(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> dpadBlobs = keepInsideBlobs(blobs);
	
	if (dpadBlobs.size() !=numBlobsNeeded) {
		cout<<"error blobs passed into calculateDpadDirection: " << dpadBlobs.size() << endl;
		return ComponentTracker::none;
	} 
	
		//assuming they are all the same size blobs (which may be a tricky 'hardware' problem (i.e. make sure the reflective tape is all the same size))
	ofxCvBlob largestBlob = getLargestBlob(dpadBlobs);
	
	return getRelativeDirection(largestBlob, dpadBlobs);
}

ofPoint ComponentTracker::measureJoystickLocation(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> joystickBlobs = keepInsideBlobs(blobs);

	if (joystickBlobs.size() !=numBlobsNeeded) {
		cout<<"error blobs passed into measureJoystickLocation: " << joystickBlobs.size() << endl;
		return ofPoint(-1, -1);
	} 
	
	ofxCvBlob* middle = new ofxCvBlob();
	ofxCvBlob* flank0 = new ofxCvBlob();
	ofxCvBlob* flank1 = new ofxCvBlob();
	
	distributeJoystickBlobs(blobs, middle, flank0, flank1, numBlobsNeeded);
	
	int x = (middle->centroid).x;
	int y = (flank0->centroid).y;
	return ofPoint(x, y);
}

string ComponentTracker::getDelta(){
	return delta;
}

void ComponentTracker::setDelta(string f){
	this->delta = f;
}

bool ComponentTracker::ROIUnset(){
	return ROI.width == 0 && ROI.height == 0;
}

std::vector<ofxCvBlob> ComponentTracker::keepInsideBlobs(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> result;
	if(ROIUnset()){
		cout<<"error in keepInnerBlobs"<<endl;
		return result;
	}
		//take only the blobs in ROI
	for(std::vector<ofxCvBlob>::iterator it = blobs.begin(); it != blobs.end();++it){
		ofxCvBlob blob = *it;
		if (ROI.inside(blob.centroid)) {
			result.push_back(blob);
			
		}
	}
	return result;
}

ComponentTracker::Direction ComponentTracker::getRelativeDirection(ofxCvBlob largestBlob, std::vector<ofxCvBlob> blobs){
//	blobs.erase(std::remove(blobs.begin(), blobs.end(), largestBlob), blobs.end());
	ofPoint p0 = blobs[0].centroid;
	ofPoint p1 = blobs[1].centroid;
	ofPoint p2 = blobs[2].centroid;
	
	ofPoint currentP = largestBlob.centroid;
	if (currentP.x >p0.x && currentP.x > p1.x && currentP.x > p2.x){
		return ComponentTracker::right;
	}else if (currentP.y >p0.y && currentP.y > p1.y && currentP.y > p2.y) {
		return ComponentTracker::up;
	}else if (currentP.y < p0.y && currentP.y < p1.y && currentP.y < p2.y) {
		return ComponentTracker::down;
	}else if (currentP.x < p0.x && currentP.x < p1.x && currentP.x < p2.x){
		return ComponentTracker::left;
	}else {
		return ComponentTracker::none;
	}
}

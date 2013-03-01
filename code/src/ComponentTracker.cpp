#include "ComponentTracker.h"

ComponentTracker::ComponentTracker() {
	id = -1;
	mIsRegistered = false;
	ROI = ofRectangle();
	mThreshold = 2.0f;//just a guess
	comptype = ComponentTracker::unknown;
}

ComponentTracker::ComponentTracker(ComponentType type, int id){
	mIsRegistered = false;
	ROI = ofRectangle();
	mThreshold = 2.0f;//just a guess
	
	this->id = id;
	this->comptype = type;
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
		case ComponentTracker::unknown:
			return "unknown_component";
			break;
		default:
			return "unknown_component";
			break;
	}
}

bool ComponentTracker::isRegistered(){
	return mIsRegistered;
}

bool ComponentTracker::finalizeRegistration(){
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
			vect.push_back((*blob).boundingRect);
			ROI = makeBoundingBox(vect);
		}
	}
}

float ComponentTracker::calculateSliderProgress(ofxCvBlob blob){
	return	distanceFormula(ROI.x, ROI.y, blob.centroid.x, blob.centroid.y)/max(ROI.height, ROI.width);
}

/*
 calculateDialProgress() returns the angle between the two blobs (which are points on the 'circumference' of the ROI (ROI is still a rectangle
 so 'circumference' isn't technically correct, but it's close enough for our purposes)). 
 The first blob is the 'initial point' from the background subtraction and the latter is the moving part.
 Formula from http://math.stackexchange.com/questions/185829/how-do-you-find-an-angle-between-two-points-on-the-edge-of-a-circle
 */
float ComponentTracker::calculateDialProgress(std::vector<ofxCvBlob> blobs){
		if (blobs.size() !=1) {
			cout<<"error blobs passed into calculateDialProgress: " << blobs.size() << endl;
			return -1.0f;
		} 
	ofPoint p1 = blobs[0].centroid;
	ofPoint p2 = ROI.getCenter();
	float r = ROI.width/2;
	p2.y = p2.y + r;

	return acos((2*pow(r, 2)- pow(distanceFormula(p1.x, p1.y, p2.x, p2.y), 2))/(2*pow(r, 2)));
	
}

ComponentTracker::Direction ComponentTracker::calculateScrollWheelDirection(std::vector<ofxCvBlob> blobs){
	float meanDistance = 0.0f;
	float totalDistance = 0.0f;
	int movementThreshhold = 2;//based on empirical evidence. 
	
	for(int i = 0; i < min(blobs.size(), previousBlobs.size()); i++) {
		float xDisp = blobs[i].centroid.x - previousBlobs[i].centroid.x;
		float yDisp = blobs[i].centroid.y - previousBlobs[i].centroid.y;
		if (((abs(xDisp) > abs(yDisp)) && xDisp > 0) || ((abs(yDisp) > abs(xDisp)) && yDisp > 0)) {//yes I know, hacky.
			totalDistance += distanceFormula(blobs[i].centroid.x, blobs[i].centroid.y, previousBlobs[i].centroid.x, previousBlobs[i].centroid.y);
			//I guess we don't technically need to do this here, but it'll set up nicely for measuring scrollwheel movement later.
		}else {
			totalDistance -= distanceFormula(blobs[i].centroid.x, blobs[i].centroid.y, previousBlobs[i].centroid.x, previousBlobs[i].centroid.y);
		}
	}
	
	meanDistance =  totalDistance /previousBlobs.size();	
	
	this->previousBlobs = blobs;//gotta replace our previous blobs
	
	if (meanDistance >movementThreshhold) {//for now, 'up' is reletively defined. 
		return ComponentTracker::up;
	}else if (meanDistance < -movementThreshhold) {
		return ComponentTracker::down;
	}else {
		return ComponentTracker::none;
	}
}

bool ComponentTracker::isButtonPressed(std::vector<ofxCvBlob> blobs){
	float precisionThreshhold = 0.5f;
	if (blobs.size() !=1) {
		cout<<"error blobs passed into isButtonPressed: " << blobs.size() << endl;
		return false;
	} 

	if(blobs[0].area/ROI.getArea() > 1-precisionThreshhold && blobs[0].area/ROI.getArea() < 1+precisionThreshhold){
		return true;
	}else{
		return false;
	}
}

ofPoint ComponentTracker::measureJoystickLocation(std::vector<ofxCvBlob> blobs){
	return ofPoint(1, 1);
}

float ComponentTracker::getDelta(){
	return delta;
}

void ComponentTracker::setDelta(float f){
	this->delta = f;
}

bool ComponentTracker::ROIUnset(){
	return ROI.width == 0 && ROI.height == 0;
}
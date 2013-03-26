#include "ComponentTracker.h"

ComponentTracker::ComponentTracker() {
	init(ComponentTracker::no_component, -1);
}

ComponentTracker::ComponentTracker(ComponentType type, int id){
	init(type, id);
}

void ComponentTracker::init(ComponentType type, int id){
	mIsRegistered = false;
	ROI = ofRectangle();
	mThreshold = 2.0f;//just a guess
	this->delta = "";
	this->prevDelta = "";
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
			numBlobsNeeded = 0;
			break;
	}
}
	
string ComponentTracker::getComponentTypeString(){
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
/*
 * setROI is called repeatedly from Sauron to set the ROI of a particular component. setROI starts to build an ROI by detecting the greatest difference in blob placement and building a ofRectangle to 
 * emcompass those differences.
 * setROI can get confused if other blobs (besides the one you are measuring) move significantly - like if the controller shakes or if another component is accidentally touched. When setROI
 * is confused in such a way, it will emcompass unintended blobs, in which case, you can finalize ROI setting, and restart it. Restarting this process will overwrite ROI.
 */
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
			
				//for buttons, keep the original blob
			if(comptype == ComponentTracker::button){
				mButtonOrigin = *blob;
			}
			
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

ofRectangle ComponentTracker::getROI(){
	return ROI;	
}

/*
 * measureComponent calls the appropriate component type measurement call with the perscribed number of blob
 * sets the delta
 * returns false if there is an error with measurement. returns true otherwise.
 */
bool ComponentTracker::measureComponent(std::vector<ofxCvBlob> blobs){
		//TODO: we like object orientedness
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
 calculateDialProgress() returns the angle of the blob passed
 Formula from http://math.stackexchange.com/questions/185829/how-do-you-find-an-angle-between-two-points-on-the-edge-of-a-circle
 */
float ComponentTracker::calculateDialProgress(std::vector<ofxCvBlob> blobs){
	bool degrees = true;//change to false if you want radians
	float rotation;
	if (degrees) {
		rotation = 360;
	}else {
		rotation = 2*PI;
	}
	
	std::vector<ofxCvBlob> dialBlobs = keepInsideBlobs(blobs);
	if (dialBlobs.size() != numBlobsNeeded) {
		cout<<"error blobs passed into calculateDialProgress: " << dialBlobs.size() << endl;
		return -1.0f;
	} 
	ofPoint p1 = dialBlobs[0].centroid;
	ofPoint p2 = ROI.getCenter();
	float r = ROI.height/2;
	p2.y = p2.y + r;
	
	float c = distanceFormula(p1.x, p1.y, p2.x, p2.y);
	float angle = acos((2*pow(r, 2)- pow(c, 2))/(2*pow(r, 2)));
	
	if (degrees) {
		angle = angle* 180/PI;
	}
	
	if (p1.x>ROI.getCenter().x) {
		return angle;
	}else return rotation-angle;
}

/*
 * calculateScrollWheelDirection takes the blobs from the view and returns the direction of the scroll wheel. The direction is determined by camera perspective up and down.
 * calculateScrollWheelDirection uses a variety of thresholding and past-diffing and miscellaneous strategies to return a intelligent/smooth directions for a scroll wheel.
 */
ComponentTracker::Direction ComponentTracker::calculateScrollWheelDirection(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> scrollBlobs = keepInsideBlobs(blobs);
	
	if (scrollBlobs.size() > numBlobsNeeded) {
		cout<<"error blobs passed into calculateScrollWheelDirection: " << scrollBlobs.size() << endl;
		return ComponentTracker::none;
	}else if (scrollBlobs.size() == 1) {
		return mPreviousScrollWheelDirection;
	}
	
	float totalDistance = 0.0f;
	float movementThreshhold = 1.5f;//based on empirical evidence. 
	float movementTolerance = 20.0f;//empirically, sometimes blobs would jump significantly
	

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
	
	float meanDistance =  totalDistance /min(scrollBlobs.size(), previousBlobs.size());	
	
	this->previousBlobs = scrollBlobs;//gotta replace our previous blobs
	
	if (meanDistance > movementTolerance || meanDistance < -movementTolerance) {//sometimes scroll wheel gets whacky (possibly due to blob id shifting). This is a bandaid to the symptoms.
		return mPreviousScrollWheelDirection;
	}
	
	if (meanDistance >movementThreshhold) {//for now, 'up' is reletively defined. 
		if (mPreviousScrollWheelDirection == ComponentTracker::down) {
			return ComponentTracker::down;
		}
		mPreviousScrollWheelDirection = ComponentTracker::up;
		return ComponentTracker::up;
	}else if (meanDistance < -movementThreshhold) {
		if (mPreviousScrollWheelDirection == ComponentTracker::up) {
			return ComponentTracker::up;
		}
		mPreviousScrollWheelDirection = ComponentTracker::down;
		return ComponentTracker::down;
	}else {
		mPreviousScrollWheelDirection = ComponentTracker::none;
		return ComponentTracker::none;
	}
}

bool ComponentTracker::isButtonPressed(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> buttonBlobs = keepInsideBlobs(blobs);
	
	if (buttonBlobs.size() !=numBlobsNeeded) {
		cout<<"error blobs passed into isButtonPressed: " << buttonBlobs.size() << endl;
		return false;
	} 
	float centerThreshold = 10.0f;
	
	ofRectangle* temp = new ofRectangle();
	temp->setFromCenter(buttonBlobs[0].centroid.x, buttonBlobs[0].centroid.y, centerThreshold, centerThreshold);
	if(buttonBlobs[0].area >mButtonOrigin.area || !temp->inside(mButtonOrigin.centroid)){//this checks whether the area is greater than start position or whether center is out of some thresholded origin area
		return true;
	}else return false;
}

/*
 *calculateDpadDirection takes all the blobs in the field and returns a ComponentTracker::Direction that corresponds to the direction that button faces relateive to the others.
 */
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

/*
 * measureJoystickLocation returns an ofPoint that is a coordinate representation of the joystick location. 
 * This function is meant for the iteration 1 joystick (the red one that has three blobs of measurement)
 */
ofPoint ComponentTracker::measureJoystickLocation(std::vector<ofxCvBlob> blobs){
	std::vector<ofxCvBlob> joystickBlobs = keepInsideBlobs(blobs);

	if (joystickBlobs.size() !=numBlobsNeeded) {
		cout<<"error blobs passed into measureJoystickLocation: " << joystickBlobs.size() << endl;
		return ofPoint(-1, -1);
	} 
	
	ofxCvBlob* middle = new ofxCvBlob();
	ofxCvBlob* flank0 = new ofxCvBlob();
	ofxCvBlob* flank1 = new ofxCvBlob();
	
		//of the joystick blobs, sets middle, flank0, flank1 to their corresponding semantic blobs. middle is the large rectangular piece, and the flanks are the surrounding pieces
	distributeJoystickBlobs(joystickBlobs, middle, flank0, flank1, numBlobsNeeded);
	
		//TODO: going to need something smarter than this to account for joystick orientation
	ofPoint p1 = middle->centroid;
	ofPoint p2 = flank0->centroid;
	int x = -1;
	int y = -1;
	if (adjustRelativePoint(&p1, ROI)) {
		x = p1.x;
	}
	if (adjustRelativePoint(&p2, ROI)) {
		y = p2.y;
	}

	return ofPoint(x, y);
}

string ComponentTracker::getDelta(){
	return delta;
}

void ComponentTracker::setDelta(string f){
	if (!isPrevDeltaInitialized()) {
		this->prevDelta = this->delta;
	}
	if (isDeltaSignificant()) {
		this->prevDelta = this->delta;
	}
	this->delta = f;
}

bool ComponentTracker::isPrevDeltaInitialized(){
	return strcmp(prevDelta.c_str(), "") != 0;
}

int ComponentTracker::getId(){
	return id;
}

ComponentTracker::ComponentType ComponentTracker::getComponentType(){
	return comptype;
}

/*
 *ROIUnset determines whether the ROI field has been set.
 */
bool ComponentTracker::ROIUnset(){
	return ROI.width == 0 && ROI.height == 0;
}
/*
 *keepInsideBlobs is a function that take a vector of blobs, blobs, and return blobs in the ofRectangle ROI. 
 *ROI should be set before keepInsideBlobs is called
 *This function should be called to start every measurement stage, in order to only play the the blobs of interest to your particular measurement
 */
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
/*
 * getRelativeDirection is a helper function for the calculateDpadDirection function. With four blob vector (presumably passed in the shape of a dpad), blobs,  and another blob, largestBlob,  which is a member of 
 * the four blob vector, it will return the relative position of largestBlob (a ComponentTracker direction).
 */
ComponentTracker::Direction ComponentTracker::getRelativeDirection(ofxCvBlob largestBlob, std::vector<ofxCvBlob> blobs){
	if (blobs.size() != 4) {
		cout << "error in getRelativeDirection()"<<endl;
		return ComponentTracker::none;
	}
	std:vector<ofPoint> p;
	for(std::vector<ofxCvBlob>::iterator it = blobs.begin(); it!= blobs.end(); ++it){
		ofxCvBlob b = *it;
		if (b.centroid != largestBlob.centroid) {
			p.push_back(b.centroid);
		}
	}
	
	ofPoint currentP = largestBlob.centroid;
		
	if (currentP.x >p[0].x && currentP.x > p[1].x && currentP.x > p[2].x){
		return ComponentTracker::right;
	}else if (currentP.y >p[0].y && currentP.y > p[1].y && currentP.y > p[2].y) {
		return ComponentTracker::up;
	}else if (currentP.y < p[0].y && currentP.y < p[1].y && currentP.y < p[2].y) {
		return ComponentTracker::down;
	}else if (currentP.x < p[0].x && currentP.x < p[1].x && currentP.x < p[2].x){
		return ComponentTracker::left;
	}else {
		return ComponentTracker::none;
	}
}
/*
 * isDeltaSignificant is what determines the amount of throttling in the OSC message passing stage. Throttling amounts were determined 'by eye' (read: non-algorithmically).
 */
bool ComponentTracker::isDeltaSignificant(){
	float significance;
	switch(this->comptype){
		case ComponentTracker::button:
			return true;//for now until we get some values
			break;
		case ComponentTracker::slider://significant on a numerical significance
			significance =  0.01f;
			if (abs(atof(delta.c_str())-atof(prevDelta.c_str()))>significance) {
				return true;
			}else return false;
			break;
		case ComponentTracker::dpad:
			return true;//for now until we implement
			break;
		case ComponentTracker::dial://significant on a numerical significance
			significance =  0.5f;
			if (abs(atof(delta.c_str())-atof(prevDelta.c_str()))>significance) {
				return true;
			}else return false;
			break;
		case ComponentTracker::scroll_wheel://significant when direction changes
			if(strcmp(delta.c_str(), prevDelta.c_str()) != 0){
				return true;
			}else return false;

			break;
		case ComponentTracker::joystick://significant on a point by point change significance
			if(strcmp(delta.c_str(), prevDelta.c_str()) != 0){//does a bit of throttling. but not to the extent we want. Probably need an aToOfPoint() in util
				return true;
			}else return false;
			break;
		default:
			return false;
			break;
	}
}

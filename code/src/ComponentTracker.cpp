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
		case ComponentTracker::trackball:
			numBlobsNeeded = 1;
			break;
		default:
			numBlobsNeeded = 0;
			break;
	}

	this->sliderStart = *(new ofPoint(std::numeric_limits<int>::max(), std::numeric_limits<int>::max()));
	this->sliderEnd = *(new ofPoint(std::numeric_limits<int>::min(), std::numeric_limits<int>::min()));

	this->joystickMiddleStart = *(new ofPoint(std::numeric_limits<int>::max(), std::numeric_limits<int>::max()));
	this->joystickMiddleEnd = *(new ofPoint(std::numeric_limits<int>::min(), std::numeric_limits<int>::min()));
	this->joystickFlankStart = *(new ofPoint(std::numeric_limits<int>::max(), std::numeric_limits<int>::max()));
	this->joystickFlankEnd = *(new ofPoint(std::numeric_limits<int>::min(), std::numeric_limits<int>::min()));
	this->idOfMiddleBlob = -1;

	this->trackballXDirection = *(new ofPoint(1,0));
	this->trackballYDirection = *(new ofPoint(0,1));

	this->jiggleThreshold = 5;
	
	this->buttonSentTrue = false;
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
		case ComponentTracker::trackball:
			return "trackball";
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
	return "";
}

bool ComponentTracker::isRegistered(){
	return mIsRegistered;
}

void ComponentTracker::finalizeRegistration(){
	mIsRegistered = true;

	atRestBlobs = this->keepInsideBlobs(previousBlobs);
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
 * setROI is called repeatedly from Sauron to set the ROI of a particular component. setROI starts to 
 * build an ROI by detecting the greatest difference in blob placement and building a ofRectangle to 
 * emcompass those differences.
 * setROI can get confused if other blobs (besides the one you are measuring) move significantly - 
 * like if the controller shakes or if another component is accidentally touched. When setROI
 * is confused in such a way, it will emcompass unintended blobs, in which case, you can finalize ROI setting, 
 * and restart it. Restarting this process will overwrite ROI.
 */
void ComponentTracker::setROI(std::vector<ofxCvBlob> blobs){
		//ok save all in previous blobs and replace it every time. the blob that changes the most beyond a threshhold is
	    // the one we are interested in.
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

	// here is some unfortunate casing
	if(this->comptype == ComponentTracker::slider) {
		// we want the start to be the min X or min Y WRT the camera
		ofxCvBlob* blob = new ofxCvBlob();
		if ( getFarthestDisplacedBlob(blob, previousBlobs, blobs, mThreshold)) {
			if(blob->centroid.x < this->sliderStart.x || blob->centroid.y < this->sliderStart.y) {
				this->sliderStart = blob->centroid;
			} else if(blob->centroid.x > this->sliderEnd.x || blob->centroid.y > this->sliderEnd.y) {
				this->sliderEnd = blob->centroid;
			}
		}
	}

	// here is some more unfortunate casing
	if(this->comptype == ComponentTracker::joystick) {
		// we want the start to be the min X or min Y WRT the camera
		ofxCvBlob* blob = new ofxCvBlob();
		if ( getFarthestDisplacedBlob(blob, previousBlobs, blobs, mThreshold)) {
			if(blobIsMiddle(*blob, blobs)) {
				if(blob->centroid.x < this->joystickMiddleStart.x || blob->centroid.y < this->joystickMiddleStart.y) {
					this->joystickMiddleStart = blob->centroid;
				} else if(blob->centroid.x > this->joystickMiddleEnd.x || blob->centroid.y > this->joystickMiddleEnd.y) {
					this->joystickMiddleEnd = blob->centroid;
				}
				if(idOfMiddleBlob == -1) {
					idOfMiddleBlob = blobIdInVector(*blob, blobs);
				}
			} else if(blob->area > ROI.getArea()/6) { // we need to make sure we're not looking at spurious blobs
				// if the farthest moved blob is not the middle blob, it is a flank blob
				// we want to be consistent in which blob we pick... so we'll always pick the one with an x or y lower
				// than the middle blob's x
				if(blob->centroid.x < this->joystickMiddleStart.x || blob->centroid.y < this->joystickMiddleStart.y) {
					// ok, now we know we have the right blob
					// so we want to do the same basic logic that we do for all the other tracks!
					if(blob->centroid.x < this->joystickFlankStart.x || blob->centroid.y < this->joystickFlankStart.y) {
						this->joystickFlankStart = blob->centroid;
					} else if(blob->centroid.x > this->joystickFlankEnd.x || blob->centroid.y > this->joystickFlankEnd.y) {
						this->joystickFlankEnd = blob->centroid;
					}
				}
			}
		}
	}

	// more casing, also unfortunate
	if(this->comptype == ComponentTracker::trackball) {
		ofxCvBlob* blob = new ofxCvBlob();
		if ( getFarthestDisplacedBlob(blob, previousBlobs, blobs, mThreshold)) {
			// we want to actually look at all the blobs' optical flow in the ROI.  because... we need to average it
			//ok, we got the blob that moved the most.  now figure out in what direction
			ofPoint opticalFlow = averageOpticalFlow(previousBlobs, blobs, ROI);
			
			// now we have the optical flow.  we asked the user to move in the x direction, so...
			this->trackballXDirection = opticalFlow;

			// we need a linearly independent vector to be the Y direction, so what we want to do is
			// take 1/slopeOfX to get slopeOfY
			double slopeOfX = trackballXDirection.y / trackballXDirection.x;
			cout<<"optical flow :" << ofPointToA(trackballXDirection)<<endl;
			// hopefully not 0!
			double slopeOfY = 1/slopeOfX;
			ofPoint yDirection;
			yDirection.x = 1;
			yDirection.y = slopeOfY;
			this->trackballYDirection = yDirection;

			this->trackballXDirection = normalize(this->trackballXDirection);
			this->trackballYDirection = normalize(this->trackballYDirection);
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
	char s[32];
		//keep blobs only in ROI
	std::vector<ofxCvBlob> componentBlobs = keepInsideBlobs(blobs);
		//error check for number of blobs in ROI for particular comptype
	/*if (!verifyNumBlobs(componentBlobs.size())) {
		cout<<"error blobs passed into measureComponent " << getComponentTypeString() << ": " << componentBlobs.size() << endl;
		sprintf(s, "error see console");
		setDelta(s);
		return false;
	}*/
		//delegate measurement to comptype
	switch (getComponentType()) {
		case ComponentTracker::slider:
			sprintf(s, "%f", calculateSliderProgress(componentBlobs));
			break;
		case ComponentTracker::dial:
			sprintf(s, "%f", (calculateDialProgress(componentBlobs)/360));
			break;
		case ComponentTracker::scroll_wheel:
			sprintf(s,"%s", EnumDirectionToString(calculateScrollWheelDirection(componentBlobs)));
			break;
		case ComponentTracker::button:
			sprintf(s, "%i", isButtonPressed(componentBlobs));
			break;
		case ComponentTracker::dpad:
			sprintf(s, "%s", EnumDirectionToString(calculateDpadDirection(componentBlobs)));
			break;
		case ComponentTracker::joystick:
			sprintf(s, "%s", ofPointToA(measureJoystickLocation(componentBlobs)).c_str());
			break;
		case ComponentTracker::trackball:
			sprintf(s, "%s", ofPointToA(calculateTrackballValue(componentBlobs)).c_str());
			break;
		default:
			return false;
	}	

	//set delta
	setDelta(s);	
}

float ComponentTracker::calculateSliderProgress(std::vector<ofxCvBlob> blobs){


	// we want to check if any of the blobs we have is basically linearly between our start and end
	double slopeOfLine = (this->sliderEnd.y - this->sliderStart.y)/(this->sliderEnd.x - this->sliderStart.x);
	double yIntercept = this->sliderEnd.y - this->sliderEnd.x*slopeOfLine;

	for(int i = 0; i < blobs.size(); i++) {
		ofxCvBlob blob = blobs.at(i);
		double expectedYAtXGivenSlope = blob.centroid.x*slopeOfLine + yIntercept;
		double offBy = expectedYAtXGivenSlope - blob.centroid.y;

		if(abs(offBy) < this->jiggleThreshold) {
			// we want to process this, it's good!
			return distanceFormula(this->sliderStart.x, this->sliderStart.y, blob.centroid.x, blob.centroid.y)/distanceFormula(this->sliderStart.x, this->sliderStart.y, this->sliderEnd.x, this->sliderEnd.y);
		}
	}

	// we didn't find anything along our line
	return -1.0f;
}

/*
 calculateDialProgress() returns the angle of the blob passed
 Formula from http://math.stackexchange.com/questions/185829/how-do-you-find-an-angle-between-two-points-on-the-edge-of-a-circle
 */
float ComponentTracker::calculateDialProgress(std::vector<ofxCvBlob> blobs){
	ofxCvBlob blob = blobs.at(0);

	// ok, so x = acost , y = bsint for ellipses
	// where a = width/2 and b = height/2
	// that means that in order to solve for t, we can do...
	double x = blob.centroid.x;
	double y = blob.centroid.y;
	
	double ROICentreX = ROI.x + ROI.width/2;
	double ROICentreY = ROI.y + ROI.height/2;

	double cost = (x - ROICentreX)/(this->ROI.width/2);
	double sint = (y - ROICentreY)/(this->ROI.height/2);

	float theta = atan2(x - ROICentreX, y - ROICentreY) + PI;
	float thetaInDegrees = theta*180/PI;
	
	return thetaInDegrees;
}

/*
 * calculateScrollWheelDirection takes the blobs from the view and returns the direction of the scroll wheel. The direction is determined by camera perspective up and down.
 * calculateScrollWheelDirection uses a variety of thresholding and past-diffing and miscellaneous strategies to return a intelligent/smooth directions for a scroll wheel.
 */
ComponentTracker::Direction ComponentTracker::calculateScrollWheelDirection(std::vector<ofxCvBlob> blobs){
	if (blobs.size() == 1) {
		return mPreviousScrollWheelDirection;
	}
	
	float totalDistance = 0.0f;
	float movementThreshhold = jiggleThreshold;//based on empirical evidence. 
	float movementTolerance = min(ROI.width/2, ROI.height/2);
	int blobsNotJumping = 0;
	
	for(int i = 0; i < min(blobs.size(), previousBlobs.size()); i++) {
		float xDisp = blobs[i].centroid.x - previousBlobs[i].centroid.x;
		float yDisp = blobs[i].centroid.y - previousBlobs[i].centroid.y;
		
		if(xDisp > movementTolerance || yDisp > movementTolerance) {
			continue;
		}
		
		if (((abs(xDisp) > abs(yDisp)) && xDisp > 0) || ((abs(yDisp) > abs(xDisp)) && yDisp > 0)) {//yes I know, hacky.
			totalDistance += distanceFormula(blobs[i].centroid.x, blobs[i].centroid.y, previousBlobs[i].centroid.x, previousBlobs[i].centroid.y);
				//I guess we don't technically need to do this here, but it'll set up nicely for measuring scrollwheel movement later.
		}else {
			totalDistance -= distanceFormula(blobs[i].centroid.x, blobs[i].centroid.y, previousBlobs[i].centroid.x, previousBlobs[i].centroid.y);
		}
		blobsNotJumping++;
	}
	
	float meanDistance =  totalDistance /blobsNotJumping;	
	
	this->previousBlobs = blobs;//gotta replace our previous blobs
	
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
	float centerThreshold = 10.0f;
	
	if(blobs.size() > 0) {
		ofPoint centreAtRest = averageOfBlobCentres(atRestBlobs);
		ofPoint currentCentre = averageOfBlobCentres(blobs);
		
		if (centreAtRest.distance(currentCentre) > jiggleThreshold) {
			return true;
		}
		return false;
	}
	// if there are no blobs, it means that the blob is outside.  so, pressed!
	return true;
}

/*
 *calculateDpadDirection takes all the blobs in the field and returns a ComponentTracker::Direction that corresponds to the direction that button faces relateive to the others.
 */
ComponentTracker::Direction ComponentTracker::calculateDpadDirection(std::vector<ofxCvBlob> blobs){
	ComponentTracker::Direction direction = ComponentTracker::none;

	// we want to know if we are off from "at rest", based on centre of centres
	ofPoint centreAtRest = averageOfBlobCentres(atRestBlobs);
	ofPoint currentCentre = averageOfBlobCentres(blobs);

	if(centreAtRest.distance(currentCentre) < 1) {
		// not really likely to have been moved
		return direction;
	}

	if(currentCentre.x < centreAtRest.x) {
		if(currentCentre.y < centreAtRest.y) {
			direction = ComponentTracker::right;
		} else {
			direction = ComponentTracker::down;
		}
	} else {
		if(currentCentre.y < centreAtRest.y) {
			direction = ComponentTracker::up;
		} else {
			direction = ComponentTracker::left;
		}
	}

	return direction;
}

/*
 * measureJoystickLocation returns an ofPoint that is a coordinate representation of the joystick location. 
 * This function is meant for the iteration 1 joystick (the red one that has three blobs of measurement)
 */
ofPoint ComponentTracker::measureJoystickLocation(std::vector<ofxCvBlob> blobs){
	/*ofPoint direction = *(new ofPoint(0,0));

	// we want to know if we are off from "at rest", based on centre of centres
	ofPoint centreAtRest = averageOfBlobCentres(atRestBlobs);
	ofPoint currentCentre = averageOfBlobCentres(blobs);

	if(centreAtRest.distance(currentCentre) < 1) {
		// not really likely to have been moved
		return direction;
	}

	direction.x = currentCentre.x - centreAtRest.x;
	direction.y = currentCentre.y - centreAtRest.y;

	return direction;*/
	ofPoint direction = *(new ofPoint(0,0));
	for(int i=0; i < blobs.size(); i++) {
		ofxCvBlob blob = blobs.at(i);
		if(blobIsMiddle(blob, blobs)) {
			direction.x = xJoystick(blob.centroid);	
		}
		else {//if(blob.area > this->ROI.getArea()/6) {
			// we might have spurious blobs in there
			direction.y = yJoystick(blob.centroid);
		}
	}
	
	prevJoystickLocation = joystickLocation;
	joystickLocation = direction;
	
	return direction;
}

bool ComponentTracker::blobIsMiddle(ofxCvBlob blob, std::vector<ofxCvBlob> blobs) {
	//if(blob.area > this->ROI.getArea()/4) {
	if(idOfMiddleBlob > -1) {
		return blobIdInVector(blob, blobs) == idOfMiddleBlob;
	}
	return blob.centroid.x > this->ROI.x + this->ROI.width/2;
}

float ComponentTracker::xJoystick(ofPoint middleCentroid) {
	// for the middle, we want to see how far it is along the middle track.
	ofPoint centreOfTrack = midpoint(joystickMiddleStart, joystickMiddleEnd);
	float distanceFromCentre = distanceFormula(middleCentroid.x, middleCentroid.y, centreOfTrack.x, centreOfTrack.y);
	//ok, so which end is it closer to?
	bool closerToStart = (distanceFormula(middleCentroid.x, middleCentroid.y, joystickMiddleStart.x, joystickMiddleStart.y) <
						  distanceFormula(middleCentroid.x, middleCentroid.y, joystickMiddleEnd.x, joystickMiddleEnd.y));

	if (closerToStart) {
		distanceFromCentre = -distanceFromCentre;
	}

	return distanceFromCentre;
}

float ComponentTracker::yJoystick(ofPoint flankCentroid) {
	// for the flank, we want to see how far it is along the middle track.
	ofPoint centreOfTrack = midpoint(joystickFlankStart, joystickFlankEnd);
	float distanceFromCentre = distanceFormula(flankCentroid.x, flankCentroid.y, centreOfTrack.x, centreOfTrack.y);
	//ok, so which end is it closer to?
	bool closerToStart = (distanceFormula(flankCentroid.x, flankCentroid.y, joystickFlankStart.x, joystickFlankStart.y) <
						  distanceFormula(flankCentroid.x, flankCentroid.y, joystickFlankEnd.x, joystickFlankEnd.y));

	if (closerToStart) {
		distanceFromCentre = -distanceFromCentre;
	}

	return distanceFromCentre;
}

ofPoint ComponentTracker::calculateTrackballValue(std::vector<ofxCvBlob> blobs) {
	ofPoint avgOpticalFlow = trackballer.latestFlow;
	
	// now decide what direction we are moving WRT the x-axis defined by the user
	return changeBasis(avgOpticalFlow, this->trackballXDirection, this->trackballYDirection);
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
			if (!strcmp(delta.c_str(), "1") == 0) {
				if(buttonSentTrue) {
					buttonSentTrue = false;
					return true;
				}
				buttonSentTrue = false;
			}
			if(!buttonSentTrue && strcmp(delta.c_str(), "1") == 0) {
				buttonSentTrue = true;
				return true;
			}
			return false;
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
			significance =  0.05f;
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
			if(prevJoystickLocation.distance(joystickLocation) > 1){
				return true;
			}else return false;
			break;
		case ComponentTracker::trackball: // did it move at all
			if(trackballer.getLatestFlow().distance(ofPoint(0,0)) > .1) {
				return true;
			} else return false;
		default:
			return false;
			break;
	}
}

bool ComponentTracker::verifyNumBlobs(int numBlobs){
	if (comptype == ComponentTracker::scroll_wheel){
		return numBlobs == 2 || numBlobs == 3 || numBlobs ==1;
	}else {
		return numBlobs == numBlobsNeeded;
	}

}

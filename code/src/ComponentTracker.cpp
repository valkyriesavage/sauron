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

	this->sliderStart = *(new ofPoint(std::numeric_limits<int>::max(), std::numeric_limits<int>::max()));
	this->sliderEnd = *(new ofPoint(std::numeric_limits<int>::min(), std::numeric_limits<int>::min()));

	this->dialEllipseCenter = *(new ofPoint(0,0));
	this->dialEllipseWidth = -1;
	this->dialEllipseHeight = -1;

	this->jiggleThreshold = 5;
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
				cout<<"dang";
			} if(blob->centroid.x > this->sliderEnd.x || blob->centroid.y > this->sliderEnd.y) {
				cout<<"also dang";
			}
			if(blob->centroid.x < this->sliderStart.x || blob->centroid.y < this->sliderStart.y) {
				this->sliderStart = blob->centroid;
			} else if(blob->centroid.x > this->sliderEnd.x || blob->centroid.y > this->sliderEnd.y) {
				this->sliderEnd = blob->centroid;
			}
		}
	}

	if(this->comptype == ComponentTracker::dial) {
		// TODO : VALKYRIE : MAKE A HOUGH TRANSFORM NONSENSE AROUND HERE!!!
		ofxCvBlob* blob = new ofxCvBlob();
		if (getFarthestDisplacedBlob(blob, previousBlobs, blobs, mThreshold)) {	
			if(distanceFormula(this->dialPoints.back().x, blob->centroid.x, this->dialPoints.back().y, blob->centroid.y) > 1) {
				// we want to make sure that we are actually progressing around the circle.  if not, don't save the blob.
				this->dialPoints.push_back(blob->centroid);
				if(this->dialPoints.size() > 6) {
					// the rest of this is only useful if we have actually gotten away from the original blob
					// otherwise we may terminate with just a couple blobs that happen to be close together.
					ofPoint firstPoint = dialPoints.at(0);
					if(distanceFormula(blob->centroid.x, blob->centroid.y, firstPoint.x, firstPoint.y) < this->jiggleThreshold) {
						// that means we are around the circle, so we need to figure out our ellipse
						determineDialEllipse();
					}
				}
			}
		}
	}
	
	previousBlobs = blobs;
}

void ComponentTracker::determineDialEllipse() {
	// so, we have a bunch of points in a dial ring.  probably they are an ellipse of some kind.  we gotta figure out what it is.
	// this code was copy/pasted (and slightly modified) from
	// http://opencv-extension-library.googlecode.com/svn/trunk/QtOpenCV/example/fitellipse/fitellipse.c
	CvMemStorage* stor;
    CvSeq* cont;
    CvBox2D32f* box;
    CvPoint* PointArray;
    CvPoint2D32f* PointArray2D32f;
    
    // Create dynamic structure and sequence.
    stor = cvCreateMemStorage(0);
    cont = cvCreateSeq(CV_SEQ_ELTYPE_POINT, sizeof(CvSeq), sizeof(CvPoint) , stor);
    
    // This cycle draw all contours and approximate it by ellipses.
    CvPoint center;
    CvSize size;
        
    // Alloc memory for contour point set.    
    PointArray = (CvPoint*)malloc( this->dialPoints.size()*sizeof(CvPoint) );
    PointArray2D32f= (CvPoint2D32f*)malloc( this->dialPoints.size()*sizeof(CvPoint2D32f) );
        
    // Alloc memory for ellipse data.
    box = (CvBox2D32f*)malloc(sizeof(CvBox2D32f));
        
    // Get contour point set.
    cvCvtSeqToArray(cont, PointArray, CV_WHOLE_SEQ);
        
    // Convert CvPoint set to CvBox2D32f set.
    for(int i=0; i<this->dialPoints.size(); i++)
    {
        PointArray2D32f[i].x = (float)PointArray[i].x;
        PointArray2D32f[i].y = (float)PointArray[i].y;
    }
        
    // Fits ellipse to current contour.
    cvFitEllipse(PointArray2D32f, this->dialPoints.size(), box);
        
    // Convert ellipse data from float to integer representation.
    center.x = cvRound(box->center.x);
    center.y = cvRound(box->center.y);
    size.width = cvRound(box->size.width*0.5);
    size.height = cvRound(box->size.height*0.5);
    box->angle = -box->angle;
    
	// actually store our values!
	this->dialEllipseCenter = *(new ofPoint(center.x, center.y));
	this->dialEllipseWidth = size.width;
	this->dialEllipseHeight = size.height;

    // Free memory.          
    free(PointArray);
    free(PointArray2D32f);
    free(box);
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
			sprintf(s, "%f", calculateDialProgress(componentBlobs));
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

	// let's decide first if we are "close" to some point around the registered ellipse.
	// if not, we are probably not looking at the right ROI
	ofxCvBlob closeBlob;
	bool closeToSomething = false;
	for(int j=0; j < blobs.size(); j++) {
		ofxCvBlob trackedBlob = blobs.at(j);
		for(int i=0; i < this->dialPoints.size(); i++) {
			ofPoint registeredPoint = dialPoints.at(i);
			if(distanceFormula(registeredPoint.x, trackedBlob.centroid.x, registeredPoint.y, trackedBlob.centroid.y) < this->jiggleThreshold) {
				closeToSomething = true;
				closeBlob = trackedBlob;
			}
		}
	}

	if(!closeToSomething) {
		// nawp
		return 0.0f;
	}

	// ok, so x = acost , y = bsint for ellipses
	// where a = width/2 and b = height/2
	// that means that in order to solve for t, we can do...
	double x = closeBlob.centroid.x;
	double y = closeBlob.centroid.y;

	double cost = x/(this->dialEllipseWidth/2);
	double sint = y/(this->dialEllipseHeight/2);

	float theta = acos(cost);
	float thetaInDegrees = theta*180/PI;

	// because of lameness, we only get theta in the [0, pi) range
	// so we need to figure out if we should be in the 3rd or 4th quadrant
	// which basically means this: is our y value below our center's y value
	if(y < dialEllipseCenter.y) {
		// ok, then rotate into those quadrants
		theta = theta + 180;
	}
	
	return theta;
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
	float movementThreshhold = 1.5f;//based on empirical evidence. 
	float movementTolerance = 20.0f;//empirically, sometimes blobs would jump significantly
	
	
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
	
	float meanDistance =  totalDistance /min(blobs.size(), previousBlobs.size());	
	
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
	
	ofRectangle* temp = new ofRectangle();
	temp->setFromCenter(blobs.front().centroid.x, blobs.front().centroid.y, centerThreshold, centerThreshold);
	if(blobs.front().area >mButtonOrigin.area || !temp->inside(mButtonOrigin.centroid)){//this checks whether the area is greater than start position or whether center is out of some thresholded origin area
		return true;
	}else return false;
}

/*
 *calculateDpadDirection takes all the blobs in the field and returns a ComponentTracker::Direction that corresponds to the direction that button faces relateive to the others.
 */
ComponentTracker::Direction ComponentTracker::calculateDpadDirection(std::vector<ofxCvBlob> blobs){
	ComponentTracker::Direction direction = ComponentTracker::none;

	// we want to look at the centre of the centres of the blobs. it points the opposite direction from where the user
	// pushed
    // determine whether the center of the four blobs' centers is off-center
    double xCenter = this->ROI.x + this->ROI.width/2;
    double yCenter = this->ROI.y + this->ROI.height/2;
    
    double xCenterBlobs = 0;
    double yCenterBlobs = 0;
    for (int i=0; i < blobs.size(); i++) {
        xCenterBlobs += blobs.at(i).centroid.x;
        yCenterBlobs += blobs.at(i).centroid.y;
    }
    xCenterBlobs = xCenterBlobs/blobs.size();
    yCenterBlobs = yCenterBlobs/blobs.size();

	if((abs(xCenterBlobs - xCenter) < jiggleThreshold) && (abs(yCenterBlobs - yCenter) < jiggleThreshold)) {
		// we didn't actually detect any movement
		return direction;
	}
    
    if(xCenterBlobs < xCenter) {
        if(yCenterBlobs < yCenter) {
            direction = ComponentTracker::up;
        } else {
            direction = ComponentTracker::right;
        }
    } else {
        if(yCenterBlobs < yCenter) {
            direction = ComponentTracker::left;
        } else {
            direction = ComponentTracker::down;
        }
    }

	return direction;
}

/*
 * measureJoystickLocation returns an ofPoint that is a coordinate representation of the joystick location. 
 * This function is meant for the iteration 1 joystick (the red one that has three blobs of measurement)
 */
ofPoint ComponentTracker::measureJoystickLocation(std::vector<ofxCvBlob> blobs){
	ofxCvBlob* middle = new ofxCvBlob();
	ofxCvBlob* flank0 = new ofxCvBlob();
	ofxCvBlob* flank1 = new ofxCvBlob();
	
		//of the joystick blobs, sets middle, flank0, flank1 to their corresponding semantic blobs. middle is the large rectangular piece, and the flanks are the surrounding pieces
	distributeJoystickBlobs(blobs, middle, flank0, flank1, numBlobsNeeded);
	
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

bool ComponentTracker::verifyNumBlobs(int numBlobs){
	if (comptype == ComponentTracker::scroll_wheel){
		return numBlobs == 2 || numBlobs == 3 || numBlobs ==1;
	}else {
		return numBlobs == numBlobsNeeded;
	}

}

#include "utilities.h"

ofVec3f centerOf(CvRect rect) {
	ofVec3f center = ofVec3f();

	center.x = rect.x + rect.width/2;
	center.y = rect.y + rect.height/2;
	center.z = 0;

	return center;
}

void wait_once(){
	string s;
	getline(cin, s);
}

/*
 makeBoundingBox takes a vector of ofRectangles and returns an ofRectangles that surrounds all of those in the vector
 */
ofRectangle makeBoundingBox(std::vector<ofRectangle> bounds){
	std::vector<ofRectangle>::iterator it = bounds.begin();
	ofRectangle boundingBox = *it;
	for (it; it != bounds.end(); ++it){
		boundingBox.growToInclude(*it);
	}
	return boundingBox;
}

float distanceFormula(float x1, float y1, float x2, float y2){
	return sqrt(pow(x2-x1, 2) + pow(y2-y1, 2));
}

ofPoint	midpoint(ofPoint p1, ofPoint p2){
	ofPoint result;
	result.set(p1.x+p2.x/2, p1.y+p2.y/2, 0.0f);
	return result;
}

bool getFarthestDisplacedBlob(ofxCvBlob* blob, std::vector<ofxCvBlob> oldBlobs, std::vector<ofxCvBlob> newBlobs, float threshold){
	int i;
	float maxDistance = FLT_MIN;
	
	float maxTolerance = 50.0f;//to account for roi jumping inclusively around, set a max
	
	for(i = 0; i<min(oldBlobs.size(), newBlobs.size()); i++){
		ofPoint oldBlobCenter = oldBlobs[i].centroid;
		ofPoint newBlobCenter = newBlobs[i].centroid;
		float blobsDistance = distanceFormula(oldBlobCenter.x, oldBlobCenter.y, newBlobCenter.x, newBlobCenter.y);
		if (blobsDistance > maxDistance ) {
			maxDistance	= blobsDistance;
			*blob = newBlobs[i];
		}
	}
	if(maxDistance > threshold && maxDistance < maxTolerance){
		return true;
	}else return false;
}

ofPoint getOpticalFlowOfBlob(ofxCvBlob blob, std::vector<ofxCvBlob> prevBlobs, std::vector<ofxCvBlob> newBlobs) {
	// sorry, that point it's returning should be a vector

	ofPoint ret;
	ret.x = 0;
	ret.y = 0;

	// first, we have to figure out which old blob we are dealing with
	for(int i =0; i< newBlobs.size(); i++) {
		if(blob.centroid == newBlobs.at(i).centroid) {
			// ok, this is our blob.  consider the blob in the same position at the previous frame
			// note that.... we don't know if these blobs will stick around.
			if(prevBlobs.size() > i) {
				ofxCvBlob prevBlob = prevBlobs.at(i);
				if(distanceFormula(prevBlob.centroid.x, prevBlob.centroid.y, blob.centroid.x, blob.centroid.y) < 50) {
					// we'll just assume that we have the right blob.
					ret.x = blob.centroid.x - prevBlob.centroid.x;
					ret.y = blob.centroid.y - prevBlob.centroid.y;
				}
			}
		}
	}

	return ret;
}

string ofPointToA(ofPoint pt){
	std::ostringstream temp;
    temp << "ofPoint(" << pt.x << ", " << pt.y << ")"; 
    return temp.str();
}

void distributeJoystickBlobs(std::vector<ofxCvBlob> blobs, ofxCvBlob* middle, ofxCvBlob* flank0, ofxCvBlob* flank1, int maxBlobs){
	/*for(std::vector<ofxCvBlob>::iterator it = blobs.begin(); it != blobs.end();++it){
		ofxCvBlob blob = *it;
		if(blob.area > middle->area){
			*flank0 = *flank1;
			*flank1 = *middle;
			*middle = blob;
		}else {
			*flank0 = *flank1;
			*flank1 = blob;
		}
	}*/

	*middle = getLargestBlob(blobs);
	// we will just take the flank to be the other blob
	for(std::vector<ofxCvBlob>::iterator it = blobs.begin(); it != blobs.end();++it){
		ofxCvBlob blob = *it;
		if(blob.centroid.x < middle->centroid.x || blob.centroid.y < middle->centroid.y) {
			*flank0 = blob;
		}
	}
}

ofxCvBlob getLargestBlob(std::vector<ofxCvBlob> blobs){
	ofxCvBlob result;
	float maxArea = FLT_MIN;
	for(std::vector<ofxCvBlob>::iterator it = blobs.begin(); it != blobs.end();++it){
		ofxCvBlob blob = *it;
		if (blob.area > result.area) {
			result = blob;
			maxArea = blob.area;
		}
	}
	return result;
}

/*
 adjustRelativePoint takes an ofPoint and ofRectangle and computes the point relative to it's position inside the rectangle.
 If the point isn't in the rectangle, we return false
 */
bool adjustRelativePoint(ofPoint* pt, ofRectangle rect){
	if (!rect.inside(*pt)) {
		return false;
	}else{
		pt->x = pt->x - rect.getX()-rect.getWidth()/2;
		pt->y = pt->y - rect.getY()-rect.getHeight()/2;
		return true;
	}
}

ofPoint normalize(ofPoint somePoint) {
	double length = sqrt(pow(somePoint.x, 2) + pow(somePoint.y, 2));
	somePoint.x = somePoint.x/length;
	somePoint.y = somePoint.y/length;

	return somePoint;
}

ofPoint changeBasis(ofPoint toBeChanged, ofPoint xDirection, ofPoint yDirection) {
	//our attitude matrix is
	// xDirection.x   xDirection.y
	// yDirection.x   yDirection.y
	// and our change of basis matrix is
	//  yDirection.y  -yDirection.x
	// -xDirection.y   xDirection.x
	// so when we multiply matrix-style, we get
	// newx = yDirection.y(oldx) - yDirection.x(oldy)
	// newY = -xDirection.y(oldx) + xDirection.x(oldy)
	ofPoint newPoint = *(new ofPoint(0,0));
	newPoint.x = yDirection.y*toBeChanged.x - yDirection.x*toBeChanged.y;
	newPoint.y = -xDirection.y*toBeChanged.x + xDirection.x*toBeChanged.y;

	return newPoint;
}

ofPoint averageOpticalFlow(std::vector<ofxCvBlob> previousBlobs, std::vector<ofxCvBlob> newBlobs, ofRectangle ROI) {
	float movementTolerance = max(3*ROI.width/4, 3*ROI.height/4);
	ofPoint avgOpticalFlow = *(new ofPoint());
	int numPointsConsidered = 0;

	for(int i = 0; i < newBlobs.size(); i++) {
		ofxCvBlob blob = newBlobs.at(i);
		if(previousBlobs.size() > i) {
			ofxCvBlob prevBlob = previousBlobs.at(i);
			if(ROI.inside(prevBlob.centroid) && ROI.inside(blob.centroid) && prevBlob.centroid.distance(blob.centroid) < movementTolerance) {
				// ok, those are probably the same blob, since we are in the movement threshold, and we are in the ROI
				avgOpticalFlow.x += blob.centroid.x - prevBlob.centroid.x; 
				avgOpticalFlow.y += blob.centroid.y - prevBlob.centroid.y;

				numPointsConsidered += 1;
			}
		}
	}

	cout << ofPointToA(avgOpticalFlow) << endl;

	// actually do the average
	if(numPointsConsidered > 0) {
		avgOpticalFlow.x /= numPointsConsidered;
		avgOpticalFlow.y /= numPointsConsidered;
	}

	return avgOpticalFlow;
}

ofPoint averageOfBlobCentres(std::vector<ofxCvBlob> blobs) {
	ofPoint centre = *(new ofPoint(0,0));

	for(int i = 0; i < blobs.size(); i++) {
		centre.x += blobs.at(i).centroid.x;
		centre.y += blobs.at(i).centroid.y;
	}

	centre.x /= blobs.size();
	centre.y /= blobs.size();

	return centre;
}

int blobIdInVector(ofxCvBlob blob, std::vector<ofxCvBlob> blobs) {
	for(int i = 0; i < blobs.size(); i++) {
		if(blobs.at(i) == blob) {
			return i;
		}
	}
	return -1;
}
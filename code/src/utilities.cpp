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
	result.set(abs(p1.x-p2.x), abs(p1.y-p2.y), 0.0f);
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

string ofPointToA(ofPoint pt){
	std::ostringstream temp;
    temp << "ofPoint(" << pt.x << ", " << pt.y << ")"; 
    return temp.str();
}

void distributeJoystickBlobs(std::vector<ofxCvBlob> blobs, ofxCvBlob* middle, ofxCvBlob* flank0, ofxCvBlob* flank1, int maxBlobs){
	if (blobs.size() != maxBlobs) {
		return;
	}
	for(std::vector<ofxCvBlob>::iterator it = blobs.begin(); it != blobs.end();++it){
		ofxCvBlob blob = *it;
		if(blob.area > middle->area){
			*flank0 = *flank1;
			*flank1 = *middle;
			*middle = blob;
		}else {
			*flank0 = *flank1;
			*flank1 = blob;
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


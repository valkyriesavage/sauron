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
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
	ofRectangle boundingBox = ofRectangle();
	for (std::vector<ofRectangle>::iterator it = bounds.begin(); it != bounds.end(); ++it){
		boundingBox.growToInclude(*it);
	}
	return boundingBox;
}

//ofRectangle CvRectToofRectangle(CvRect cv){
//	return new ofRectangle(cv.x, cv.y, cv.width, cv.height);
//}
//
//CvRect ofRectangleToCvRect(ofRectangle of){
//	return CvRect(of.x, of.y, of.width, of.height);
//}
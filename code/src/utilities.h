#include <math.h>
#include"ofxOpenCv.h"
#pragma once

ofVec3f centerOf(CvRect rect);
void wait_once();

ofRectangle makeBoundingBox(std::vector<ofRectangle> bounds);

ofRectangle CvRectToofRectangle(CvRect cv);

CvRect ofRectangleToCvRect(ofRectangle of);

float distanceFormula(float x1, float y1, float x2, float y2);

ofPoint	midpoint(ofPoint p1, ofPoint p2);

bool getFarthestDisplacedBlob(ofxCvBlob* blob, std::vector<ofxCvBlob> oldBlobs, std::vector<ofxCvBlob> newBlobs, float threshold);

string ofPointToA(ofPoint pt);
#include <math.h>
#include"ofxOpenCv.h"
#pragma once

ofVec3f centerOf(CvRect rect);
void wait_once();

ofRectangle makeBoundingBox(std::vector<ofRectangle> bounds);

ofRectangle CvRectToofRectangle(CvRect cv);

CvRect ofRectangleToCvRect(ofRectangle of);
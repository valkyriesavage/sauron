#include <math.h>
#include"ofxOpenCv.h"
#pragma once

ofVec3f centerOf(CvRect rect);
void wait_once();

ofRectangle makeBoundingBox(std::vector<ofRectangle> bounds);

ofRectangle CvRectToofRectangle(CvRect cv);

CvRect ofRectangleToCvRect(ofRectangle of);

float distanceFormula(float x1, float y1, float x2, float y2);

float distanceFormula(ofPoint p1, ofPoint p2);

ofPoint	midpoint(ofPoint p1, ofPoint p2);

bool getFarthestDisplacedBlob(ofxCvBlob* blob, std::vector<ofxCvBlob> oldBlobs, std::vector<ofxCvBlob> newBlobs, float threshold);

ofPoint getOpticalFlowOfBlob(ofxCvBlob blob, std::vector<ofxCvBlob> prevBlobs, std::vector<ofxCvBlob> newBlobs);

string ofPointToA(ofPoint pt);

void distributeJoystickBlobs(std::vector<ofxCvBlob> blobs, ofxCvBlob* middle, ofxCvBlob* flank0, ofxCvBlob* flank1, int maxBlobs);

ofxCvBlob getLargestBlob(std::vector<ofxCvBlob> blobs);

bool adjustRelativePoint(ofPoint* pt, ofRectangle rect);

ofPoint normalize(ofPoint somePoint);

ofPoint changeBasis(ofPoint toBeChanged, ofPoint xDirection, ofPoint yDirection);

ofPoint averageOpticalFlow(std::vector<ofxCvBlob> previousBlobs, std::vector<ofxCvBlob> newBlobs, ofRectangle ROI);

ofPoint averageOfBlobCentres(std::vector<ofxCvBlob> blobs);

int blobIdInVector(ofxCvBlob blob, std::vector<ofxCvBlob> blobs);
/*
 *  TrackballTracker.h
 *  allAddonsExample
 *
 *  Created by Valkyrie A. Savage on 2.4.13.
 *  Copyright 2013 UC Berkeley. All rights reserved.
 *
 */

#include "ofMain.h"

#include "ofxOpenCv.h"

#pragma once
class TrackballTracker {
public:
	IplImage *pyramid, *prev_pyramid, *swap_temp;
	char* status;

	static const int MAX_COUNT = 500;
	CvPoint2D32f* points[2];

	double quality;
	double min_distance;
	int count;
	int flags;
	int win_size;
	
	ofPoint latestFlow;
	
	TrackballTracker();
	void init();
	ofPoint getTrackballFlow(ofxCvColorImage colorImage, ofxCvGrayscaleImage grayImage, ofxCvGrayscaleImage prevGrayImage, ofRectangle ROI);

	ofPoint getLatestFlow();
};
/*
 *  TrackballTracker.cpp
 *  allAddonsExample
 *
 *  Created by Valkyrie A. Savage on 2.4.13.
 *  Copyright 2013 UC Berkeley. All rights reserved.
 *
 */

#include "TrackballTracker.h"

TrackballTracker::TrackballTracker() {
	this->init();
}

void TrackballTracker::init() {
points[0] = (CvPoint2D32f*)cvAlloc(MAX_COUNT*sizeof(points[0][0]));
points[1] = (CvPoint2D32f*)cvAlloc(MAX_COUNT*sizeof(points[0][0]));

status = (char*)cvAlloc(MAX_COUNT);

quality = 0.01;
min_distance = 10;
count = 350;
flags = 0;
win_size = 10;
}

ofPoint TrackballTracker::getTrackballFlow(ofxCvColorImage colorImage, ofxCvGrayscaleImage grayImage, ofxCvGrayscaleImage prevGrayImage, ofRectangle ROI) {
	// find features that we want to track between the images
	
	IplImage* eig = cvCreateImage( cvGetSize(grayImage.getCvImage()), 32, 1 );
	IplImage* temp = cvCreateImage( cvGetSize(grayImage.getCvImage()), 32, 1 );
	
	count = 350;
	
	cvGoodFeaturesToTrack( grayImage.getCvImage(), eig, temp, points[1], &count, quality, min_distance, 0, 3, 0, 0.04 );
	cvFindCornerSubPix( grayImage.getCvImage(), points[1], count, cvSize(win_size,win_size), cvSize(-1,-1), cvTermCriteria(CV_TERMCRIT_ITER|CV_TERMCRIT_EPS,20,0.03));
	
	cvReleaseImage( &eig );
	cvReleaseImage( &temp );
	
	pyramid = cvCreateImage( cvGetSize(grayImage.getCvImage()), 8, 1 );
	prev_pyramid = cvCreateImage( cvGetSize(grayImage.getCvImage()), 8, 1 );	
	
	cvCalcOpticalFlowPyrLK( prevGrayImage.getCvImage(), grayImage.getCvImage(), prev_pyramid, pyramid, points[1], points[0], count, cvSize(win_size, win_size), 3, status, 0, cvTermCriteria(CV_TERMCRIT_ITER|CV_TERMCRIT_EPS,20,0.03), flags );
	flags |= CV_LKFLOW_PYR_A_READY;
	
	CvPoint2D32f *swap_points;
	CV_SWAP( points[0], points[1], swap_points );
	CV_SWAP( prev_pyramid, pyramid, swap_temp );
	
	// ok, that should have done the calculation for us?  so we want to get the average optical flow
	float totalX = 0;
	float totalY = 0;
	for(int i=0; i<count; i++){
		float x1 = points[0][i].x;
		float y1 = points[0][i].y;
		float x2 = points[1][i].x;
		float y2 = points[1][i].y;
	
		totalX += x2-x1;
		totalY += y2-y1;
	}
	latestFlow = *(new ofPoint(totalX/count, totalY/count));
	
	return latestFlow;
}

ofPoint TrackballTracker::getLatestFlow() {
	return latestFlow;
}
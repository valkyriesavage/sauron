#include "ComponentTracker.h"

bool ComponentTracker::buttonEventDetected(ofxCvContourFinder contourFinder) {
	return false;
}

bool ComponentTracker::sliderEventDetected(ofxCvContourFinder contourFinder, int* sliderPosition) {
	if (contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	*sliderPosition = contourFinder.blobs.at(0).centroid.distance(contourFinder.blobs.at(1).centroid) / this->regionOfInterest.width;
	return true;
}

bool ComponentTracker::dialEventDetected(ofxCvContourFinder contourFinder, int* dialPosition) {
	return false;
}

bool ComponentTracker::joystickEventDetected(ofxCvContourFinder contourFinder, int* xPosition, int* yPosition) {
	return false;
}

bool ComponentTracker::dpadEventDetected(ofxCvContourFinder contourFinder, Direction* direction) {
	return false;
}
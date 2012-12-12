#include "ComponentTracker.h"

bool ComponentTracker::buttonEventDetected(ofxCvContourFinder contourFinder) {
	return contourFinder.blobs.size() > 0;
}

bool ComponentTracker::sliderEventDetected(ofxCvContourFinder contourFinder, int* sliderPosition) {
	if (contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	*sliderPosition = contourFinder.blobs.at(0).centroid.distance(contourFinder.blobs.at(1).centroid) / this->regionOfInterest.width;
	return true;
}

bool ComponentTracker::dialEventDetected(ofxCvContourFinder contourFinder, int* dialPosition) {
	if (contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	// get the dial position : we have a triangle with one side as half the rectangle (from the edge to the
	// center of the circle) and one side as the distance from the center of the circle to the moving blob.
	// so we need to find angle between the adjacent side and hypotenuse : thus arccos
	*dialPosition = acos((this->regionOfInterest.width/2)/contourFinder.blobs.at(0).centroid.distance(centerOf(this->regionOfInterest)) * 180/PI);
	return true;
}

bool ComponentTracker::joystickEventDetected(ofxCvContourFinder contourFinder, int* xPosition, int* yPosition) {
	if (contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	*xPosition = 0;
	*yPosition = 0;
	return false;
}

bool ComponentTracker::dpadEventDetected(ofxCvContourFinder contourFinder, Direction* direction) {
	if (contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	return false;
}
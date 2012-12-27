#include "ComponentTracker.h"

bool ComponentTracker::buttonEventDetected() {
	return this->contourFinder.blobs.size() > 0;
}

bool ComponentTracker::sliderEventDetected(int* sliderPosition) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	*sliderPosition = this->contourFinder.blobs.at(0).centroid.distance(contourFinder.blobs.at(1).centroid) / this->regionOfInterest.width;
	return true;
}

bool ComponentTracker::dialEventDetected(int* dialPosition) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	// get the dial position : we have a triangle with one side as half the rectangle (from the edge to the
	// center of the circle) and one side as the distance from the center of the circle to the moving blob.
	// so we need to find angle between the adjacent side and hypotenuse : thus arccos
	*dialPosition = acos((this->regionOfInterest.width/2)/contourFinder.blobs.at(0).centroid.distance(centerOf(this->regionOfInterest)) * 180/PI);
	return true;
}

bool ComponentTracker::scrollWheelEventDetected( Direction* scrollDirection) {
    if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
        return false;
    }
    
    return true;
}

bool ComponentTracker::dpadEventDetected(Direction* direction) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
	return false;
}

bool ComponentTracker::joystickEventDetected(int* xPosition, int* yPosition) {
	if (this->contourFinder.blobs.size() < this->numBlobsNeeded) {
		return false;
	}
    // TODO : finish this.  I don't have a printed joystick but can guess at stuff.
	*xPosition = 0;
	*yPosition = 0;
	return false;
}
#include "ComponentTracker.h"

bool ComponentTracker::buttonEventDetected() {
	return false;
}

bool ComponentTracker::sliderEventDetected(int* sliderPosition) {
	return false;
}

bool ComponentTracker::dialEventDetected(int* dialPosition) {
	return false;
}

bool ComponentTracker::joystickEventDetected(int* xPosition, int* yPosition) {
	return false;
}

bool ComponentTracker::dpadEventDetected(Direction* direction) {
	return false;
}
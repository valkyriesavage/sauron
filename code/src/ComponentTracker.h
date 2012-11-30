#include "ofxOpenCv.h"

class ComponentTracker {
	public:
		enum ComponentType {button, slider, dial, joystick, dpad, scroll_wheel};
		enum Direction {up, down, left, right};

		ComponentType comptype;
		ofxCvBlob component;
		ofPoint prevCentroid;

		bool buttonEventDetected();
		bool sliderEventDetected(int* sliderPosition);
		bool dialEventDetected(int* dialPosition);
		bool joystickEventDetected(int* xPosition, int* yPosition);
		bool dpadEventDetected(Direction* direction);
};
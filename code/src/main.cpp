#include "ofMain.h"
#include "ofAppGlutWindow.h"

#include "testing.h"
#include "Sauron.h"
#include "ComponentTracker.h"
#include "utilities.h"

//========================================================================
int main( ){
    
    bool doTesting = false;

    ofAppGlutWindow window;
	ofSetupOpenGL(&window, 1024,768, OF_WINDOW);			// <-------- setup the GL context

    if (doTesting) {
        // run the example app that shipped; this will help
        // find locations of objects to set ROIs in the other one
        ofRunApp(new testing());
    } else {
        ofRunApp(new Sauron());
    }
}

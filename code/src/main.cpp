#include "ofMain.h"
#include "ofAppGlutWindow.h"

#include "Sauron.h"
#include "ComponentTracker.h"
#include "utilities.h"

//========================================================================
int main( ){
    ofAppGlutWindow window;
	ofSetupOpenGL(&window, 700,500, OF_WINDOW);			// <-------- setup the GL context

    ofRunApp(new Sauron());
}

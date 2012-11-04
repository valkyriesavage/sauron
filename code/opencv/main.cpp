
// This code displays corners found by the opencv function
// GoodFeaturesToTrack (cvGoodFeaturesToTrack)
//
// Sample webcam code taken from
//   http://www.cs.iit.edu/~agam/cs512/lect-notes/
//   opencv-intro/opencv-intro.html#SECTION00070000000000000000
//
// compile with:
// gcc `pkg-config --cflags opencv` `pkg-config --libs opencv`
// -o good_features good_features.cpp
//
// Kristi Tsukida  <kristi.tsukida@gmail.com> Aug 20, 2008
//

#include <iostream>
#include <cv.h>
#include <highgui.h>
#include <stdio.h>
#include <sys/time.h>

#define VIDEO_WINDOW   "Webcam"
#define CORNER_EIG     "Eigenvalue Corner Detection"

#define USEC_PER_SEC 1000000L

CvScalar target_color[4] = { // in BGR order
	{{   0,   0, 255,   0 }},  // red
	{{   0, 255,   0,   0 }},  // green
	{{ 255,   0,   0,   0 }},  // blue
	{{   0, 255, 255,   0 }}   // yellow
};

// returns the number of usecs of (t2 - t1)
long time_elapsed (struct timeval &t1, struct timeval &t2) {
	
	long sec, usec;
	
	sec = t2.tv_sec - t1.tv_sec;
	usec = t2.tv_usec - t1.tv_usec;
	if (usec < 0) {
		--sec;
		usec = usec + USEC_PER_SEC;
	}
	return sec*USEC_PER_SEC + usec;
}

struct timeval start_time;
struct timeval end_time;
void start_timer() {
	struct timezone tz;
	gettimeofday (&start_time, &tz);
}
long end_timer() {
	struct timezone tz;
	gettimeofday (&end_time, &tz);
	return time_elapsed(start_time, end_time);
}

// A Simple Camera Capture Framework
int main(int argc, char *argv[]) {
	long eig_time;
	
	CvCapture* capture = 0;
	IplImage* curr_frame = 0; // current video frame
	IplImage* gray_frame = 0; // grayscale version of current frame
	int w, h; // video frame size
	IplImage* eig_image = 0;
	IplImage* temp_image = 0;
	
	// Capture from a webcam
	capture = cvCaptureFromCAM(CV_CAP_ANY);
	//capture = cvCaptureFromCAM(0); // capture from video device #0
	if ( !capture) {
		fprintf(stderr, "ERROR: capture is NULL... Exiting\n");
		//getchar();
		return -1;
	}
	
	// Create a window in which the captured images will be presented
	cvNamedWindow(VIDEO_WINDOW, 0); // allow the window to be resized
	
	cvNamedWindow(CORNER_EIG, 0); // allow the window to be resized
	cvMoveWindow(CORNER_EIG, 330, 0);
	
	// Show the image captured from the camera in the window and repeat
	while (true) {
		
		// Get one frame
		curr_frame = cvQueryFrame(capture);
		if ( !curr_frame) {
			fprintf(stderr, "ERROR: frame is null... Exiting\n");
			//getchar();
			break;
		}
		// Do not release the frame!
		
		// Get frame size
		w = curr_frame->width;
		h = curr_frame->height;
		
		// Convert the frame image to grayscale
		if( ! gray_frame ) {
			//fprintf(stderr, "Allocate gray_frame\n");
			int channels = 1;
			gray_frame = cvCreateImage(
																 cvGetSize(curr_frame),
																 IPL_DEPTH_8U, channels);
		}
		cvCvtColor(curr_frame, gray_frame, CV_BGR2GRAY);
		
		// ==== Allocate memory for corner arrays ====
		if ( !eig_image) {
			//fprintf(stderr, "Allocate eig_image\n");
			eig_image = cvCreateImage(cvSize(w, h),
																IPL_DEPTH_32F, 1);
		}
		if ( !temp_image) {
			//fprintf(stderr, "Allocate temp_image\n");
			temp_image = cvCreateImage(cvSize(w, h),
																 IPL_DEPTH_32F, 1);
		}
		
		// ==== Corner Detection: MinEigenVal method ====
		start_timer();
		const int MAX_CORNERS = 50;
		CvPoint2D32f corners[MAX_CORNERS] = {0};
		int corner_count = MAX_CORNERS;
		double quality_level = 0.1;
		double min_distance = 5;
		int eig_block_size = 3;
		int use_harris = false;
		
		cvGoodFeaturesToTrack(gray_frame,
													eig_image,                    // output
													temp_image,
													corners,
													&corner_count,
													quality_level,
													min_distance,
													NULL,
													eig_block_size,
													use_harris);
		cvScale(eig_image, eig_image, 100, 0.00);
		eig_time = end_timer();
		cvShowImage(CORNER_EIG, eig_image);
		
		// ==== Draw circles around detected corners in original image
		//fprintf(stderr, "corner[0] = (%f, %f)\n", corners[0].x, corners[0].y);
		for( int i = 0; i < corner_count; i++) {
			int radius = h/25;
			cvCircle(curr_frame,
							 cvPoint((int)(corners[i].x + 0.5f),(int)(corners[i].y + 0.5f)),
							 radius,
							 target_color[0]);
		}
		cvShowImage(VIDEO_WINDOW, curr_frame);
		
		// If ESC key pressed, Key=0x10001B under OpenCV 0.9.7(linux version),
		// remove higher bits using AND operator
		if ( (cvWaitKey(10) & 255) == 27)
			break;
	}
	
	// Release the capture device housekeeping
	cvReleaseCapture( &capture);
	cvDestroyWindow(VIDEO_WINDOW);
	cvDestroyWindow(CORNER_EIG);
	// Disable harris processing
	//	cvDestroyWindow(CORNER_HARRIS);
	return 0;
}

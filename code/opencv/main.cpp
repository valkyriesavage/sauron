// This is the main part of the computer vision algorithm for
// the Skuld project.  It has several parts:
//   * detect interesting edges, ideally those of the input component bases
//   * calculate optical flow along these edges
//   * report when thresholds have been exceeded, i.e. an event has occurred

#include <math.h>
#include <opencv2/opencv.hpp>

using namespace cv;

const int BUTTON = 1;
const int SLIDER = 2;
const int JOYSTICK = 3;

void drawOptFlowMap(const Mat& flow,
                    Mat& cflowmap,
                    int step,
                    const Scalar& color) {

	for(int y = 0; y < cflowmap.rows; y += step)
	{
		for(int x = 0; x < cflowmap.cols; x += step)
		{
			const Point2f& fxy = flow.at<Point2f>(y, x);
			line(cflowmap,
					 Point(x,y),
					 Point(cvRound(x+fxy.x),cvRound(y+fxy.y)),
					 color);
			circle(cflowmap, Point(x,y), 2, color, -1);
		}
	}
}

void detectThresholds(const Mat& flow,
											vector<Point>& thresholdsExceeded,
											int step) {
	const int thresholdButton = step*2;
	for(int y = 0; y < flow.rows; y += step)
	{
		for(int x = 0; x < flow.cols; x += step)
		{
			const Point2f& fxy = flow.at<Point2f>(y, x);
			if (sqrt(pow(fxy.x, 2) + pow(fxy.y, 2)) > thresholdButton) {
				thresholdsExceeded.push_back(Point(x*1.f, y*1.f));
			}
		}
	}
}

void determineButtons(vector<Point>& thresholdsExceeded,
											vector<Rect>& buttonLocations/*,
											add in return of activated button*/) {
	for (vector<Point>::iterator it=thresholdsExceeded.begin(); it<thresholdsExceeded.end(); it++) {
		for(vector<Rect>::iterator itRect=buttonLocations.begin(); itRect<buttonLocations.end(); itRect++) {
			if ((*it).inside(*itRect)) {
				//std::cout << "inside rect " << *itRect << "\n";
			}
		}
	}
}

int main(int argc, char **args) {
	
	VideoCapture cap(0); // open the default camera
	if(!cap.isOpened())  // check if we succeeded
		return -1;
	
	Mat newFrame, newGray, prevGray, thresholdsExceeded;
	
	cap >> newFrame; // get a new frame from camera
	cvtColor(newFrame, newGray, CV_BGR2GRAY);
	prevGray = newGray.clone();
	
	double pyr_scale = 0.5;
	int levels = 3;
	int winsize = 5;
	int iterations = 5;
	int poly_n = 5;
	double poly_sigma = 1.1;
	int flags = 0;
	int step = 20;
	
	while(true) {
		cap >> newFrame;
		if(newFrame.empty()) break;
		cvtColor(newFrame, newGray, CV_BGR2GRAY);
		
		Mat flow = Mat(newGray.size(), CV_32FC2);
		
		/* Do optical flow computation */
		calcOpticalFlowFarneback(prevGray,
														 newGray,
														 flow,
														 pyr_scale,
														 levels,
														 winsize,
														 iterations,
														 poly_n,
														 poly_sigma,
														 flags
														 );
		
		drawOptFlowMap(flow,
									 newFrame,
									 step,
									 CV_RGB(0,255,0));

		vector<Point> thresholdsExceeded;
		detectThresholds(flow, thresholdsExceeded, step);
		std::cout << thresholdsExceeded << "\n";
		//determineButtons(thresholdsExceeded);
		
		namedWindow("Output",1);
		imshow("Output", newFrame);
		waitKey(1);
		
		prevGray = newGray.clone();
	}
	
	return 0;
}
#include "CalibCoordinate.h"
using namespace cv;
using namespace Eigen;



int  CalibCoordinateMap(double* pixels, double* worlds, int n, const char* calibFile)
{
	std::vector<cv::Point2d> pixelsCV(n);
	std::vector<cv::Point2d> worldsCV(n);
	cv::Point2d midPointCV;

	memcpy(&pixelsCV[0].x, &pixels[0], sizeof(double) * n * 2);
	memcpy(&worldsCV[0].x, &worlds[0], sizeof(double) * n * 2);

	int ret = calib_pixel_2_world(pixelsCV, worldsCV, calibFile);
	if (ret != 0)
	{
		std::cout << "Calib error \n";
		return -1;
	}

	std::vector<cv::Point2d>  calculate_test;
	//std::cout << "rigid \n";

	Point2d mes = cv::Point2d(0, 0);
	map_pixels_2_worlds(calibFile, pixelsCV, calculate_test, false);
	for (size_t i = 0; i < calculate_test.size(); i++)
	{
		cv::Point2d  error = calculate_test[i] - worldsCV[i];
		std::cout << error.x << "\t" << error.y << "\n";
		mes += error;
	}

	mes.x /= calculate_test.size();
	mes.y /= calculate_test.size();
	std::cout << mes.x << "\t" << mes.y << "\n";
	std::cout << "\n";
	std::cout << "\n";
	return ret;

	/*    使用 opencv 的 仿射变换 刚体变换都很差。根本用不了
	std::cout << "affine2D \n";



	std::vector <cv::Point2f> src ( pixelsCV.begin(), pixelsCV.end());
	std::vector <cv::Point2f> dst(worldsCV.begin(), worldsCV.end());


	误差太大了，没眼看
	auto m  = cv::estimateRigidTransform(src, dst,false);

	std::vector<cv::Point2f> input = src;
	std::vector<cv::Point2f> output;

	cv::transform(input, output, m);

	for (size_t i = 0; i < calculate_test.size(); i++)
	{
		cv::Point2d  error = output[i] - dst[i];
		std::cout << error.x << "\t" << error.y << "\n";
		mes.x += error.x;
		mes.y += error.y;
	}
	mes.x /= calculate_test.size();
	mes.y /= calculate_test.size();
	//std::cout << mes.x << "\t" << mes.y << "\n";
	std::cout << "\n";
	std::cout << "\n";

	//std::vector<cali::Point2d> pixelsCVCali, worldsCVCali;

	//for (size_t i = 0; i < pixelsCV.size(); i++)
	//{
	//	pixelsCVCali.push_back(cali::Point2d(pixelsCV[i].x, pixelsCV[i].y));
	//	worldsCVCali.push_back(cali::Point2d(worldsCV[i].x, worldsCV[i].y));
	//}

	//CaliPixel2World(pixelsCVCali, worldsCVCali, "old_cv_calib.txt");
	//std::vector<cali::Point2d> test_old;

	//MapPixel2World("old_cv_calib.txt", pixelsCVCali, test_old,false	);
	//mes = cv::Point2d(0, 0);
	//for (size_t i = 0; i < test_old.size(); i++)
	//{
	//	cv::Point2d  error = Point2d(test_old[i].x, test_old[i].y) - worldsCV[i];
	//	std::cout << error.x << "\t" << error.y << "\n";
	//	mes += error;
	//}

	//mes.x /= test_old.size();
	//mes.y /= test_old.size();
	//std::cout << mes.x << "\t" << mes.y << "\n";
	//std::cout << "\n";
	//std::cout << "\n";

	return ret;
	*/

}

int CalculateDstPoints(const char* calibFile, double* pixels, int n, double* dstPoints, bool inverse)
{
	std::vector<cv::Point2d> pixelsCV(n);
	memcpy(&pixelsCV[0].x, &pixels[0], sizeof(double) * n * 2);

	std::vector<cv::Point2d> dstCV;
	int ret = map_pixels_2_worlds(calibFile, pixelsCV, dstCV, inverse);
	if (ret != 0)
	{
		std::cout << "CalculateDstPoints error \n";
		return -1;
	}

	memcpy(&dstPoints[0], &dstCV[0].x, sizeof(double) * n * 2);

	return 0;
}



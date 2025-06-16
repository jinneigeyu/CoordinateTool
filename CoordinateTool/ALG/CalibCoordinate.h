#pragma once
#include <opencv2/opencv.hpp>
#include <Eigen/Eigen>



#ifdef EXPORTING_DLL
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __declspec(dllimport)
#endif





// For cpp


int calib_pixel_2_world(const std::vector<cv::Point2d>& pixels, const std::vector<cv::Point2d> world, std::string calibfile);

int map_pixel_2_world(const std::string& calibfile, const cv::Point2d& pixel, cv::Point2d& world, bool inverse);

int map_pixels_2_worlds(const std::string& calibfile, const std::vector<cv::Point2d>& pixels, std::vector<cv::Point2d>& worlds, bool inverse);



// for c sharp
extern "C" 
{


	/// <summary>
	///  calib pixel coordinate to a world coordinate
	/// </summary>
	/// <param name="pixels"></param>
	/// <param name="worlds"></param>
	/// <param name="n"></param>
	/// <param name="calibFile"></param>
	/// <returns></returns>
	DLLEXPORT int  CalibCoordinateMap(double* pixels, double* worlds, int n,
		const char* calibFile);



	DLLEXPORT int CalculateDstPoints(const char* calibFile, double* pixels, int n, double* dstPoints,bool inverse=false);
}
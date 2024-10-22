#pragma once
#include <opencv2/opencv.hpp>
#include "core.h"

#ifdef EXPORTING_DLL
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __declspec(dllimport)
#endif


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
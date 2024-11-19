#pragma once
#include <Eigen/Eigen>
#include <opencv2/opencv.hpp>

cv::Mat getCVMatrix(const Eigen::MatrixXd& eigenMatrix);

Eigen::MatrixXd getEigenMatrix(const cv::Mat& cvMatrix);

struct CoordinateMapMatrix
{
	Eigen::MatrixXd R;
	Eigen::Vector3d c1;
	Eigen::Vector3d c2;
};

int load_coordinate_map_matrix(const std::string& filename, CoordinateMapMatrix& mapMatrix);

int save_coordinate_map_matrix(const std::string& filename, const CoordinateMapMatrix& mapMatrix);

int  calib_pixel_to_axis(
	const std::vector<Eigen::Vector3d>& pts1,
	const  std::vector<Eigen::Vector3d>& pts2,
	Eigen::MatrixXd& R,
	Eigen::Vector3d& c1,
	Eigen::Vector3d& c2);

Eigen::Vector3d  calc_pixel_to_axis(
	const Eigen::Vector3d& p1,
	const Eigen::MatrixXd& R,
	const  Eigen::Vector3d& c1,
	const Eigen::Vector3d& c2,
	bool inverse = false);

std::vector < Eigen::Vector3d > calc_pixel_to_axis(
	const std::vector< Eigen::Vector3d>& inputs,
	const Eigen::MatrixXd& R,
	const  Eigen::Vector3d& c1,
	const Eigen::Vector3d& c2,
	bool inverse = false);

int calib_pixel_2_world(const std::vector<cv::Point2d>& pixels, const std::vector<cv::Point2d> world, std::string calibfile);

int map_pixel_2_world(const std::string& calibfile, const cv::Point2d& pixel, cv::Point2d& world, bool inverse);

int map_pixels_2_worlds(const std::string& calibfile, const std::vector<cv::Point2d>& pixels, std::vector<cv::Point2d>& worlds, bool inverse);
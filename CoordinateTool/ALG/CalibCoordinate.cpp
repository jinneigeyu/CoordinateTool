#include "CalibCoordinate.h"
#include <filesystem>

using namespace cv;
using namespace Eigen;


#pragma region Core Impl

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

cv::Mat getCVMatrix(const Eigen::MatrixXd& eigenMatrix)
{
	cv::Mat cvMatrix(eigenMatrix.cols(), eigenMatrix.rows(), CV_64F);
	std::memcpy(cvMatrix.data, eigenMatrix.data(), sizeof(double) * eigenMatrix.size());
	cv::Mat cvMatrixT(cvMatrix.t());
	return cvMatrixT;
}


Eigen::MatrixXd getEigenMatrix(const cv::Mat& cvMatrix)
{
	int rows = cvMatrix.rows;
	int cols = cvMatrix.cols;
	cv::Mat cvT = cvMatrix.t();

	cvT.convertTo(cvT, CV_64F);

	Eigen::MatrixXd target(rows, cols);

	std::memcpy(target.data(), cvT.data, sizeof(double) * rows * cols);

	return target;
}


int load_coordinate_map_matrix(const std::string& filename, CoordinateMapMatrix& mapMatrix)
{
	try
	{
		cv::Mat cvMatrix, cvMatrixC1, cvMatrixC2;
		cv::FileStorage fs(filename, cv::FileStorage::READ);
		fs["R"] >> cvMatrix;
		fs["C1"] >> cvMatrixC1;
		fs["C2"] >> cvMatrixC2;

		mapMatrix.R = getEigenMatrix(cvMatrix);
		mapMatrix.c1 = getEigenMatrix(cvMatrixC1);
		mapMatrix.c2 = getEigenMatrix(cvMatrixC2);

		fs.release();
	}
	catch (const std::exception& ex)
	{
		std::cout << ex.what() << std::endl;
		return -1;
	}


	return 0;
}

int save_coordinate_map_matrix(const std::string& filename, const CoordinateMapMatrix& mapMatrix)
{
	try
	{
		// 将 Eigen Matrix 转换为 OpenCV Mat
		cv::Mat cvMatrix = getCVMatrix(mapMatrix.R);
		cv::Mat cvMatrixC1 = getCVMatrix(mapMatrix.c1);
		cv::Mat cvMatrixC2 = getCVMatrix(mapMatrix.c2);


		cv::FileStorage fs(filename, cv::FileStorage::WRITE);

		fs << "R" << cvMatrix;
		fs << "C1" << cvMatrixC1;
		fs << "C2" << cvMatrixC2;

		fs.release();
	}
	catch (const std::exception& ex)
	{
		std::cout << ex.what() << std::endl;

		return -1;
	}
	return 0;
}

/// <summary>
///  solve    A=[p1 ... ]  B = [p2 ....]
///  Map A-> B
///  AX=B
/// (p1 - c1).t() * R = (p2 - c2).t()
/// R.t() * (p1 - c1) = (p2 - c2)
/// </summary>
/// <param name="pts1"></param>
/// <param name="pts2"></param>
/// <param name="R"> (p1 - c1).t() * R = (p2 - c2).t() </param>
/// <param name="c1"> center of p1 </param>
/// <param name="c2"> center of p2 </param>
/// <returns></returns>
int  calib_pixel_to_axis(
	const std::vector<Eigen::Vector3d>& pts1,
	const  std::vector<Eigen::Vector3d>& pts2,
	Eigen::MatrixXd& R,
	Eigen::Vector3d& c1,
	Eigen::Vector3d& c2)
{
	std::cout << "---------------------------------------" << std::endl;
	using namespace Eigen;

	assert(pts1.size() == pts2.size());
	assert(pts1.size() >= 3);
	int N = pts1.size();

	Vector3d p1_center(0, 0, 0), p2_center(0, 0, 0);
	for (int i = 0; i < N; i++)
	{
		p1_center += pts1[i];
		p2_center += pts2[i];
	}
	p1_center /= N;
	p2_center /= N;

	MatrixXd A(N, 3);
	MatrixXd B(N, 3);
	std::vector<Vector3d> q1(N), q2(N);
	for (int i = 0; i < N; i++)
	{
		q1[i] = pts1[i] - p1_center;
		q2[i] = pts2[i] - p2_center;

		A.row(i) = q1[i].transpose();
		B.row(i) = q2[i].transpose();
	}

	MatrixXd R_;
	std::cout << "----------------Qr--------------------" << std::endl;
	R_ = A.colPivHouseholderQr().solve(B);  // 实际就是最小二乘解 AX=B       At*A*X=At*B ->   X = (At*A).invers() * At * B
	std::cout << "R is -- \n" << R_ << std::endl;


	std::cout << "----------------SVD--------------------" << std::endl;
	JacobiSVD<MatrixXd> svd(A, ComputeFullU | ComputeFullV);
	R_ = svd.solve(B);
	std::cout << "R is -- \n" << R_ << std::endl;

	R = R_;
	c1 = p1_center;
	c2 = p2_center;

	//计算rt
	//RowVector3d T = (- p1_center.transpose()* R_ + p2_center.transpose());
	//Eigen::Matrix3d r33 = R;

	//std::cout << r33 << std::endl;
	//std::cout << T << std::endl;
	/* equle with  svd.solve(B)
	MatrixXd k(A.Dot_Rows(), A.Dot_Cols());
	k.setZero();
	VectorXd singularValues = svd.singularValues();
	for (size_t i = 0; i < singularValues.Dot_Rows(); i++)
		k(i, i) = 1 / singularValues[i];

	cout << "Its singular values are:" << endl << svd.singularValues() << endl;
	cout << "Its left singular vectors are the columns of the thin U matrix:" << endl << svd.matrixU() << endl;
	cout << "Its right singular vectors are the columns of the thin V matrix:" << endl << svd.matrixV() << endl;

	std::cout << svd.matrixV() * k * svd.matrixU().transpose() * B << std::endl;
	std::cout << std::endl;
	std::cout << svd.solve(B) << std::endl;*/

	// 验证
	//for (int i = 0; i < N; i++)
	//{
	//	Vector3d  ss = (pts1[i] - p1_center).transpose() * R  - （pts2[i]- p2_center).transpose();
	//	Vector3d  ss =  R.transpose() * (pts1[i] - p1_center)  - （pts2[i]- p2_center）;
	//	std::cout << ss << std::endl << std::endl;
	//}

	return 0;
}


/// <summary>
/// (p1 - c1).t() * R = (p2 - c2).t()
/// R.t() * (p1 - c1) = (p2 - c2)
/// </summary>
/// <param name="p1"></param>
/// <param name="R"></param>
/// <param name="c1"></param>
/// <param name="c2"></param>
/// <returns></returns>
Eigen::Vector3d  calc_pixel_to_axis(
	const Eigen::Vector3d& p1,
	const Eigen::MatrixXd& R,
	const  Eigen::Vector3d& c1,
	const Eigen::Vector3d& c2,
	bool inverse)
{

	if (inverse)
	{
		Eigen::Vector3d p2 = p1;
		return   R.transpose().inverse() * (p2 - c2) + c1;
	}


	return  R.transpose() * (p1 - c1) + c2;

}

/// <summary>
/// (p1 - c1).t() * R = (p2 - c2).t()
/// R.t() * (p1 - c1) = (p2 - c2)
/// </summary>
/// <param name="p1"></param>
/// <param name="R"></param>
/// <param name="c1"></param>
/// <param name="c2"></param>
/// <returns></returns>
std::vector < Eigen::Vector3d > calc_pixel_to_axis(
	const std::vector< Eigen::Vector3d>& inputs,
	const Eigen::MatrixXd& R,
	const  Eigen::Vector3d& c1,
	const Eigen::Vector3d& c2,
	bool inverse)
{

	std::vector < Eigen::Vector3d > outputs;
	int n = inputs.size();


	if (inverse)
	{
		// construct of center matrix B
		MatrixXd B_of_center(3, n);
		for (size_t i = 0; i < inputs.size(); i++)
		{

			Vector3d of_c2 = inputs[i] - c2;
			B_of_center.col(i) = of_c2;
		}

		// get center matrix 
		MatrixXd c_1_matrix(3, n);
		for (int i = 0; i < n; ++i) {
			c_1_matrix.col(i) = c1;
		}

		MatrixXd dst = R.transpose().inverse() * B_of_center + c_1_matrix;

		for (size_t i = 0; i < n; i++)
			outputs.emplace_back(dst.col(i));
	}

	// construct of center matrix A
	MatrixXd A_of_center(3, n);
	for (size_t i = 0; i < inputs.size(); i++)
	{

		Vector3d of_c1 = inputs[i] - c1;
		A_of_center.col(i) = of_c1;
	}

	// get center matrix 
	MatrixXd c_2_matrix(3, n);
	for (int i = 0; i < n; ++i) {
		c_2_matrix.col(i) = c2;
	}

	MatrixXd dst = R.transpose() * A_of_center + c_2_matrix;

	for (size_t i = 0; i < n; i++)
		outputs.emplace_back(dst.col(i));

	return outputs;
}


int calib_pixel_2_world(const std::vector<cv::Point2d>& pixels, const std::vector<cv::Point2d> world, std::string calibfile)
{

	if (pixels.size() != world.size())
		return -1;

	if (pixels.size() < 3)
		return -1;

	std::vector<Eigen::Vector3d> pts1, pts2;

	for (size_t i = 0; i < pixels.size(); i++)
	{
		pts1.push_back(Vector3d(pixels[i].x, pixels[i].y, 0));
		pts2.push_back(Vector3d(world[i].x, world[i].y, 0));
	}

	CoordinateMapMatrix calibMatrix;
	int ret = calib_pixel_to_axis(pts1, pts2, calibMatrix.R, calibMatrix.c1, calibMatrix.c2);
	if (ret != 0)
		return ret;

	return save_coordinate_map_matrix(calibfile, calibMatrix);
}


int map_pixel_2_world(const std::string& calibfile, const cv::Point2d& pixel, cv::Point2d& world, bool inverse)
{

	if (!std::filesystem::exists(calibfile))
		return -1;

	cv::Mat cvMatrix, cvMatrixC1, cvMatrixC2;
	cv::FileStorage fs(calibfile, cv::FileStorage::READ);

	fs["R"] >> cvMatrix;
	fs["C1"] >> cvMatrixC1;
	fs["C2"] >> cvMatrixC2;

	Eigen::MatrixXd R = getEigenMatrix(cvMatrix);
	Eigen::MatrixXd C1 = getEigenMatrix(cvMatrixC1);
	Eigen::MatrixXd C2 = getEigenMatrix(cvMatrixC2);

	Eigen::Vector3d w_vec = calc_pixel_to_axis(Vector3d(pixel.x, pixel.y, 0), R, C1, C2, inverse);

	world.x = w_vec[0];
	world.y = w_vec[1];

	return 0;
}


int map_pixels_2_worlds(const std::string& calibfile, const std::vector<cv::Point2d>& pixels, std::vector<cv::Point2d>& worlds, bool inverse)
{
	if (!std::filesystem::exists(calibfile))
		return -1;

	CoordinateMapMatrix calibMatrix;
	if (load_coordinate_map_matrix(calibfile, calibMatrix) != 0)
		return -1;

	if (pixels.size() == 1)
	{
		worlds = std::vector<cv::Point2d>(1);

		return map_pixel_2_world(calibfile, pixels[0], worlds[0], inverse);
	}
	else
	{
		worlds = std::vector<cv::Point2d>(pixels.size());

		std::vector<Vector3d> inputs;
		for (size_t i = 0; i < pixels.size(); i++)
			inputs.push_back(Vector3d(pixels[i].x, pixels[i].y, 0));

		std::vector<Vector3d> outputs = calc_pixel_to_axis(inputs, calibMatrix.R, calibMatrix.c1, calibMatrix.c2, inverse);

		for (size_t i = 0; i < pixels.size(); i++)
			worlds[i] = cv::Point2d(outputs[i].x(), outputs[i].y());

	}



	return 0;
}


#pragma endregion



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
	Point2d mes = cv::Point2d(0, 0);
	double r = 0;
	map_pixels_2_worlds(calibFile, pixelsCV, calculate_test, false);
	for (size_t i = 0; i < calculate_test.size(); i++)
	{
		cv::Point2d  error = calculate_test[i] - worldsCV[i];
		std::cout << "error [" + std::to_string(i) << "] " << error.x << "\t" << error.y << "\n";
		mes += error;
		r = error.x * error.x + error.y * error.y;
	}

	mes.x /= calculate_test.size();
	mes.y /= calculate_test.size();
	double rms = sqrt(r / calculate_test.size());
	std::cout << "RMS  :" << rms << "\n";

	double length = 1000;
	map_pixels_2_worlds(calibFile, { cv::Point2d(length,0),cv::Point2d(0,length) }, calculate_test, false);
	cv::FileStorage fs(calibFile, cv::FileStorage::APPEND);
	fs << "RMS" << rms;
	fs << "X_Pixel2World" << (calculate_test[0]/ length);
	fs << "Y_Pixel2World" << (calculate_test[1]/ length);

	fs.release();

	std::cout << "X_Pixel2World  :" << (calculate_test[0] / length) << "\n";
	std::cout << "Y_Pixel2World  :" << (calculate_test[1] / length) << "\n";

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



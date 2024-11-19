#include "core.h"
#include <filesystem>
#include <Eigen/Eigen>
using namespace cv;
using namespace Eigen;


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

	//Eigen::MatrixXd R;
	//Vector3d C1, C2;

	CoordinateMapMatrix calibMatrix;
	int ret = calib_pixel_to_axis(pts1, pts2, calibMatrix.R, calibMatrix.c1, calibMatrix.c2);
	if (ret != 0)
		return ret;


	// 将 Eigen Matrix 转换为 OpenCV Mat
	//cv::Mat cvMatrix = getCVMatrix(R);
	//cv::Mat cvMatrixC1 = getCVMatrix(C1);
	//cv::Mat cvMatrixC2 = getCVMatrix(C2);


	//cv::FileStorage fs(calibfile, cv::FileStorage::WRITE);

	//fs << "R" << cvMatrix;
	//fs << "C1" << cvMatrixC1;
	//fs << "C2" << cvMatrixC2;

	//fs.release();

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
	if( load_coordinate_map_matrix(calibfile, calibMatrix) != 0)
		return -1;

	//cv::Mat cvMatrix, cvMatrixC1, cvMatrixC2;

	//cv::FileStorage fs(calibfile, cv::FileStorage::READ);

	//fs["R"] >> cvMatrix;
	//fs["C1"] >> cvMatrixC1;
	//fs["C2"] >> cvMatrixC2;

	//Eigen::MatrixXd R = getEigenMatrix(cvMatrix);
	//Eigen::MatrixXd C1 = getEigenMatrix(cvMatrixC1);
	//Eigen::MatrixXd C2 = getEigenMatrix(cvMatrixC2);

	if (pixels.size() == 1)
	{
		worlds = std::vector<cv::Point2d>(1);

		return map_pixel_2_world(calibfile, pixels[0], worlds[0], inverse);
	}
	else
	{


		worlds = std::vector<cv::Point2d>(pixels.size());

		for (size_t i = 0; i < pixels.size(); i++)
		{
			map_pixel_2_world(calibfile, pixels[i], worlds[i], inverse);
		}



		std::vector<Vector3d> inputs;
		for (size_t i = 0; i < pixels.size(); i++)
		{
			inputs.push_back(Vector3d(pixels[i].x, pixels[i].y, 0));
		}


		std::vector<Vector3d> outputs = calc_pixel_to_axis(inputs, calibMatrix.R, calibMatrix.c1, calibMatrix.c2, inverse);

		auto worlds_test = std::vector<cv::Point2d>(pixels.size());

		for (size_t i = 0; i < pixels.size(); i++)
		{
			worlds_test[i] = cv::Point2d(outputs[i].x(), outputs[i].y());


			if (worlds_test[i] != worlds[i])
			{
				return -1;
			}
		}



	}



	return 0;
}


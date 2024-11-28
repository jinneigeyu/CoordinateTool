# CoordinateTool
a tool of calibration for c1 to c2 . (2d) , and transform c1 to c2 . (2d)

## Dependence
* opencv
* Eigen

##  for dotNet project set no prefix output dir:
edit the .csproj file  :
insert  to  ``<PropertyGroup>``
```
	 <AppendTargetFrameworkToOutputPath>output</AppendTargetFrameworkToOutputPath>
	 <OutputPath>$(SolutionDir)\bin</OutputPath>
```

## How to use

1. Calibrate
   * test_data:t
   * load pixels  -> test_data/pixels.txt
   * load worlds  -> test_data/worlds.txt
   * Caculate the transformation matrix  -> test_data/calib.txt
  ![alt text](files/image.png)
  
2. Transform
   * load calib.txt
   * load pixels  -> test_data/pixels.txt
   * Transform the pixels , compare with raw worlds
  ![alt text](files/image-1.png)

### Reference
reference: https://github.com/jinneigeyu/CoordinateTool

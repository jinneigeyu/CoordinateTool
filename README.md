# CoordinateTool
a tool of calibration for c1 to c2 . (2d) , and transform c1 to c2 . (2d)

#  for doNet project set no prefix output dir:
edit the .csproj file  :
insert  to  ``<PropertyGroup>``
```
	 <AppendTargetFrameworkToOutputPath>output</AppendTargetFrameworkToOutputPath>
	 <OutputPath>$(SolutionDir)\bin</OutputPath>
```

## How to use

1. Calibrate
* test_data:
* load pixels  -> test_data/pixels.txt
* load worlds  -> test_data/worlds.txt
* Caculate the transformation matrix  -> test_data/calib.txt
  
2. Transform
* load calib.txt
* load pixels  -> test_data/pixels.txt
* Transform the pixels  , compare with raw worlds

## License
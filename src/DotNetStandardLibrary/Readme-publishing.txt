How to publish:

	1) Change the version in AFRocketScienceStandard project properties in the "Package" sections
	2) build debug or release
	3) Right-click on AFRocketScienceStandard and choose "Publish" and publish to a folder 
		- observe the pack location from the output window (probably ./DotNetStandardLibrary\bin\Release\netstandard2.0\publish)
	3) Adjust the following command to have the correct version and key, then run it:
				nuget push RocketScience.Azure.Functions.0.1.0.nupkg [key]  -Source https://api.nuget.org/v3/index.json

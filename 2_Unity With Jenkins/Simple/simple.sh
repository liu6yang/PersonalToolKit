RootDir=`pwd`
Unity='/cygdrive/d/program files/unity/editor/Unity'
ProjectF='/cygdrive/e/github/UntiyWithJenkins/UnityWithJenkins'
cd $ProjectF
ProjectPath=`pwd`

"$Unity" -quit -batchmode -projectPath $ProjectPath -executeMethod common.Build.PerformAndroidBuild -CustomArgs:Build_Dir=$TargetDir
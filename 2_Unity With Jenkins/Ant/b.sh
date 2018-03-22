export PATH=$PATH:/usr/local/bin:/Users/Shared/Jenkins/Desktop/apache-ant-1.9.7/bin
export ANDROID_SDK=/Users/Shared/Jenkins/Desktop/android-sdk-macosx
env

RootDir=`pwd`
Unity=/Applications/Unity/Unity.app/Contents/MacOS/Unity
Date=`date +%m-%d_%H-%M-%S`
ShareRootDir=/Users/Shared/Jenkins/Desktop/ProjectF_Build_Android
ShareDir=$ShareRootDir/\[$BUILD_NUMBER\]_$Date
if [ "$HG_TAG" != "" ]; then
  ShareDir="$ShareDir"_["$HG_TAG"]
fi


if [ "$GAMMA_VERSION" == "true" ]; then
  ShareDir="$ShareDir"_[Gamma]
elif [ "$RELEASE_VERSION" == "true" ]; then
  ShareDir="$ShareDir"_[Release]
fi

if [ "$BUILD_AND_UPLOAD_DLC" == "true" ]; then
  ShareDir="$ShareDir"_[DLC]
fi

ShareDir="$ShareDir"_ARMv7

Project=ProjectF
#Target=$ShareDir/SOT_ALL_v"$GAME_VERSION"_\[$BUILD_NUMBER\]_ARMv7.apk
cd ProjectF
ProjectPath=`pwd`

echo Creating rsp files...
python $RootDir/SharedLibraries/Libs/Jenkins/env2rsp.py
python $RootDir/SharedLibraries/Libs/Jenkins/env2rsp.py > ./Assets/gmcs.rsp
python $RootDir/SharedLibraries/Libs/Jenkins/env2rsp.py > ./Assets/smcs.rsp

#GAME_VERSION=1.0.0
Target=$ShareDir/BitcoinCatcher_ALL_v"$GAME_VERSION"_\[$BUILD_NUMBER\]_ARMv7.apk

echo Building apk...
"$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild_pre.log
"$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild_recompile.log -projectPath $ProjectPath -executeMethod common.BaseMacroEditor.RecompileScript
"$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild_AppendSdkMacros.log -projectPath $ProjectPath -executeMethod common.BaseMacroEditor.AppendSdkMacros

if [ "$JENKINS_UPDATE_CH_BUILD_LIST" == "true" ]; then
  "$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild.log -projectPath $ProjectPath -executeMethod common.Build.BuildChBuildList -CustomArgs:CH_BUILD_LIST_PATH=/Users/Shared/Jenkins/Desktop/CH_BUILD_LIST/ProjectF_Android_on_mac
  scp /Users/Shared/Jenkins/Desktop/CH_BUILD_LIST/ProjectF_Android_on_mac mac01:~/Desktop/CH_BUILD_LIST/
elif [ "$BUILD_AND_UPLOAD_DLC" == "true" ]; then
  "$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild_dlc.log -projectPath $ProjectPath -executeMethod dlc.DlcConfigEditor.DlcBuildAndUpload_Android
elif [ "$ECLIPSE_PRJ_NUMBER" != "" ]; then
  "$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild.log -projectPath $ProjectPath -executeMethod common.Build.BuildApkFromOtherEclipsePrj -CustomArgs:Build_Target=$Target\;ShareRootDir=$ShareRootDir
elif [ "$DEVELOPMENT_BUILD" == "true" ]; then
  "$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild.log -projectPath $ProjectPath -executeMethod common.Build.PerformAndroidBuild_Debug -CustomArgs:Build_Target=$Target\;Game_Version=$GAME_VERSION
else
  "$Unity" -quit -batchmode -logFile $ShareDir/UnityBuild.log -projectPath $ProjectPath -executeMethod common.Build.PerformAndroidBuild -CustomArgs:Build_Target=$Target\;Game_Version=$GAME_VERSION
fi


cd $RootDir
echo Done.

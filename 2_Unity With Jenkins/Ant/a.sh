pwd
export PATH=$PATH:/usr/local/bin
export ANDROID_SDK=/Users/Shared/Jenkins/Desktop/android-sdk-macosx
mount_dir=http://10.10.10.10:8040/

set +e
echo Checking project exist...
hg id
if [ $? != 0 ]; then
  set -e
  echo Cloning project from server...
  hg clone $mount_dir .
fi

set -e
find ProjectF/Assets/ -name "*.orig" -exec rm -f {} +
find SharedLibraries/ -name "*.orig" -exec rm -f {} +
if [ $FULL_CLEAN == "true" ]; then
  echo Full cleaning...
  hg purge --all --config extensions.purge=
else
  hg purge --config extensions.purge=
fi

set -e
echo Reverting changes...
hg revert -a
echo Pulling from server...
hg pull
if [ "$HG_TAG" == "" ]; then
  echo Updating to HEAD...
  hg update -C default
else
  echo Updating to tag $HG_TAG
  hg update -C $HG_TAG
fi
hg id

set +e
echo Done.

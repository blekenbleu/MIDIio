#! /bin/sh
if [ -z "$2" ] ; then
  ns="'""$1 Beta'"
else
  ns="'""$1 $2'"
fi
if [ -z "$1" ] ; then
  echo $0 v0.0.1
  echo gh release list
  gh release list
else
  cp bin/Release/net48/blekenbleu.MIDIio.dll .
  echo 7z u bin/Release/MIDIio.zip NCalcScripts/*ini blekenbleu.MIDIio.dll
  7z u bin/Release/MIDIio.zip NCalcScripts/*ini blekenbleu.MIDIio.dll
  echo gh release create $1 -n "$ns" -p bin/Release/MIDIio.zip
  gh release create $1 -n "$ns" -t 'Release by gh'  bin/Release/MIDIio.zip
  rm blekenbleu.MIDIio.dll
fi

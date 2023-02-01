#! /bin/sh
if [ -z "$2" ] ; then
  ns="'""$1 Beta'"
else
  ns="'""$1 $2'"
  if [ -z "$3" ] ; then
     ts='Release by gh'
  else
    ts="'$3'"
  fi
fi

if [ ! -r NCalcScripts/MIDIio.ini ] ; then
  echo $0 needs to be run from the project root directory as docs/release version note title
  exit
fi

if [ -z "$1" ] ; then
  echo $0 v0.0.1 'notes' 'title'
  echo gh release list
  gh release list
else
  cp obj/Release/net48/blekenbleu.MIDIio.dll .
  echo 7z u bin/Release/MIDIio.zip NCalcScripts/*ini blekenbleu.MIDIio.dll
  7z u bin/Release/MIDIio.zip NCalcScripts/*ini blekenbleu.MIDIio.dll
  echo gh release create $1 -n "$ns" -t "$ts"  bin/Release/MIDIio.zip
  gh release create $1 -n "$ns" -t "$ts"  bin/Release/MIDIio.zip
  rm blekenbleu.MIDIio.dll
fi

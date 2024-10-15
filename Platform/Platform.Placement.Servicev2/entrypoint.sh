#!/bin/bash

set -euo pipefail

if [[ -v MTS_USERNAME && ! -z $MTS_USERNAME ]]; then
  xmlstarlet ed -L -u '/configuration/mtsSdkSection/@username' -v $MTS_USERNAME Platform.Placement.Servicev2.dll.config
fi

if [[ -v MTS_PASSWORD && ! -z $MTS_PASSWORD ]]; then
  xmlstarlet ed -L -u '/configuration/mtsSdkSection/@password' -v $MTS_PASSWORD Platform.Placement.Servicev2.dll.config
fi

if [[ -v MTS_BOOKMAKERID && ! -z $MTS_BOOKMAKERID ]]; then
  xmlstarlet ed -L -u '/configuration/mtsSdkSection/@bookmakerId' -v $MTS_BOOKMAKERID Platform.Placement.Servicev2.dll.config
fi

if [[ -v MTS_HOST && ! -z $MTS_HOST ]]; then
  xmlstarlet ed -L -u '/configuration/mtsSdkSection/@host' -v $MTS_HOST Platform.Placement.Servicev2.dll.config
fi

if [[ -v MTS_NODE && ! -z $MTS_NODE ]]; then
  xmlstarlet ed -L -u '/configuration/mtsSdkSection/@node' -v $MTS_NODE Platform.Placement.Servicev2.dll.config
fi

dotnet Platform.Placement.Servicev2.dll

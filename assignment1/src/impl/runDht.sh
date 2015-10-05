#!/bin/bash

directory=`pwd` # current working directory
frontendExe="UiT.Inf3200/FrontendServer/bin/Debug/FrontendServer.exe"
storageNExe="UiT.Inf3200/StorageNodeServer/bin/Debug/StorageNodeServer.exe"

echo Starting Frontend Server on http://$HOSTNAME:8181/
nohup mono $frontendExe > $HOSTNAME.FrontendServer.out 2>&1 &

for nd in `cat hostfile`
do
	echo Starting storage node on $nd
	ssh $nd "cd \"$directory\" ; nohup mono $storageNExe http://$HOSTNAME:8181/ > $nd.StorageNodeServer.out 2>&1 & "
done


#!/bin/bash

directory=`pwd` # current working directory
availableNodesFile="src/impl/UiT.Inf3200/FrontendApp/wwwroot/availableNodes"
p2pNodeExe="src/impl/UiT.Inf3200/P2PNode/bin/Debug/P2PNode.exe"

rm -fv $availableNodesFile
touch $availableNodesFile

for nd in `sh /share/apps/bin/available-nodes.sh`
do
	echo Starting node on $nd
	nohup ssh $nd "cd \"$directory\" ; nohup mono $p2pNodeExe > $nd.p2p.out 2>&1 & " > /dev/null 2>&1 &
	echo $nd:8899 >> $availableNodesFile
done


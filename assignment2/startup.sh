#!/bin/bash

directory=`pwd` # current working directory
availableNodesFile="src/impl/UiT.Inf3200/FrontendApp/availableNodes"
p2pNodeExe="src/impl/UiT.Inf3200/P2PNode/bin/Debug/P2PNode.exe"

rm -fv $availableNodesFile
touch $availableNodesFile

for nd in `cat hostfile`
do
	echo Starting node on $nd
	nohup ssh $nd "cd \"$directory\" ; mono $p2pNodeExe" > /dev/null 2>&1 &
	echo $nd:8899 >> $availableNodesFile
done


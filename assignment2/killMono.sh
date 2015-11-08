#!/bin/bash

for nd in `sh /share/apps/bin/available-nodes.sh`
do
	echo Killing all mono processes on $nd
	nohup ssh $nd "pkill mono" > /dev/null 2>&1 &
done



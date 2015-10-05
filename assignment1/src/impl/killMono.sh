#!/bin/bash

echo Killing all local mono processes
pkill mono
for nd in `cat hostfile`
do
	echo Killing all mono processes on $nd
	nohup ssh $nd "pkill mono" > /dev/null 2>&1 &
done



#!/bin/bash

echo Killing all local mono processes
pkill mono
for nd in `cat hostfile`
do
	echo Killing all mono processes on $nd
	ssh $nd "pkill mono"
done



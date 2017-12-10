#!/bin/bash

# a=*cfg
a=*.new

echo $a

for i in $a; do
	#newname=${i}.new
	#sed  -e 's/ignitionsavailable/IgnitionsAvailable/gI' $i >$newname

	#sed -i 's/ignitortype/IgnitorType/gI' $newname
	#sed -i 's/autoignitiontemperature/AutoIgnitionTemperature/gI' $newname
	#sed -i 's/igniterange/IgniteRange/gI' $newname
	#sed -i 's/useullagesimulation/UseUllageSimulation/gI' $newname

	sed -i 's/chancewhenunstable/ChanceWhenUnstable/gI' $i

done

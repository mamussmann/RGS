#
# Copyright (c) 2024 Marc Mußmann
#
# Permission is hereby granted, free of charge, to any person obtaining a copy of 
# this software and associated documentation files (the "Software"), to deal in the 
# Software without restriction, including without limitation the rights to use, copy, 
# modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
# and to permit persons to whom the Software is furnished to do so, subject to the 
# following conditions:
#
# The above copyright notice and this permission notice shall be included in all 
# copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
# INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
# PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
# FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
# OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
# DEALINGS IN THE SOFTWARE.
#
import matplotlib.pyplot as plt 
import csv 
import sys
import numpy as np

csvFilePath = sys.argv[1]
outputPath = sys.argv[2] 
rootTypes = int(sys.argv[3]) 
rootTypeCSV = sys.argv[4]
showWindow = eval(sys.argv[5])
segmentLength = sys.argv[6]

typeColors = []

with open(rootTypeCSV,'r') as csvfile: 
    lines = csv.reader(csvfile, delimiter=';') 
    for row in lines: 
        typeColors.append([row[0], row[1]])
    
rootTypeCounts = []
y = [] 
for index in range(rootTypes): 
    y.append([])

with open(csvFilePath,'r') as csvfile: 
    lines = csv.reader(csvfile, delimiter=';')
    i = 0
    for row in lines: 
        if i == 0:
            for index in range(rootTypes): 
                rootTypeCounts.append(int(row[index])) 
        else:
            for index in range(rootTypes): 
                if i - 1 < rootTypeCounts[index]:
                    y[index].append(float(row[index])) 
        i += 1


for index in range(rootTypes): 
    valMin = sys.float_info.max
    valMax = sys.float_info.min
    if rootTypeCounts[index] == 0:
        continue
    if valMin > min(y[index]):
        valMin = min(y[index])

    if valMax < max(y[index]):
        valMax = max(y[index])
    len = valMax - valMin
    binCount = int(round(len))
    if binCount > 0:
        plt.hist(y[index], binCount, histtype = 'step', orientation = 'horizontal', color = typeColors[index][0], label= typeColors[index][1])

plt.xlabel('Segments') 
plt.ylabel('Root Depth (cm)') 
plt.title('Root segments per depth\n(segment length='+segmentLength+' cm)', fontsize = 15) 
plt.grid() 
plt.legend() 
plt.savefig(outputPath)
if showWindow:
    plt.show() 
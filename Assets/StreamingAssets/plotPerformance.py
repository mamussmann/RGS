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
import statistics as sta

csvFilePath = sys.argv[1]
outputPath = sys.argv[2] 
showWindow = eval(sys.argv[3])
agentType = sys.argv[4]
simMode = sys.argv[5]

   
h = []
x = []

with open(csvFilePath,'r') as csvfile: 
    lines = csv.reader(csvfile, delimiter=';') 
    lineIndex = 0
    for row in lines: 
        agentCount = int(row[0])
        count = int(row[2])
        x.append(agentCount)
        h.append([])
        for index in range(count): 
            h[lineIndex].append(float(row[3 + index])) 
        lineIndex += 1
    

med = []
minValues = []
maxValues = []
for lst in h:
    med.append(sta.median(lst))
for lst in h:
    minValues.append(min(lst))
for lst in h:
    maxValues.append(max(lst))

plt.plot(x, med, linestyle = 'solid', 
        marker = '', label = 'median') 
plt.plot(x, minValues, linestyle = 'dashed', 
        marker = '', label = 'min') 
plt.plot(x, maxValues, linestyle = 'dashed', 
        marker = '', label = 'max') 
    

plt.xlabel(''+agentType+' agents') 
plt.ylabel('FPS') 
plt.title('FPS by agent count\n('+agentType+' agents in '+simMode+' mode)', fontsize = 15) 
plt.grid() 
plt.legend() 
plt.savefig(outputPath)
if showWindow:
    plt.show() 

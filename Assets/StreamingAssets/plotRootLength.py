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

csvFilePath = sys.argv[1]
outputPath = sys.argv[2] 
rootTypes = int(sys.argv[3]) 
rootTypeCSV = sys.argv[4]
showWindow = eval(sys.argv[5])


typeColors = []

with open(rootTypeCSV,'r') as csvfile: 
    lines = csv.reader(csvfile, delimiter=';') 
    for row in lines: 
        typeColors.append([row[0], row[1]])
    

x = [] 
y = [] 
for index in range(rootTypes): 
    y.append([])

with open(csvFilePath,'r') as csvfile: 
    lines = csv.reader(csvfile, delimiter=';') 
    for row in lines: 
        x.append(float(row[0]))
        for index in range(rootTypes): 
            y[index].append(float(row[1 + index])) 

for index in range(rootTypes): 
    plt.plot(x, y[index], color = typeColors[index][0], linestyle = 'solid', 
            marker = '',label = typeColors[index][1]) 
    

plt.xlabel('Time') 
plt.ylabel('Length (m)') 
plt.title('Root length over time', fontsize = 20) 
plt.grid() 
plt.legend() 
plt.savefig(outputPath)
if showWindow:
    plt.show() 

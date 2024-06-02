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
showWindow = eval(sys.argv[3])

   
h = [[],[],[]]
x = [0,1,2]
label = []
counts = []

with open(csvFilePath,'r') as csvfile: 
    lines = csv.reader(csvfile, delimiter=';') 
    lineIndex = 0
    for row in lines: 
        if lineIndex == 0:
            label.append(str(row[0]))
            label.append(str(row[1]))
            label.append(str(row[2]))
        elif lineIndex == 1:
            counts.append(int(row[0]))
            counts.append(int(row[1]))
            counts.append(int(row[2]))
        else:
            for i in range(3):
                if lineIndex < counts[i]:
                    h[i].append(int(row[i]))
        lineIndex += 1
    
plt.boxplot(h, positions=x, labels=label, meanline=True, showmeans= True, showfliers=False)

plt.xlabel('Modes') 
plt.ylabel('FPS') 
plt.title('FPS by simulation mode', fontsize = 20) 
plt.grid() 
plt.legend() 
plt.savefig(outputPath)
if showWindow:
    plt.show() 

/* 
* Copyright (c) 2024 Marc Mußmann
*
* Permission is hereby granted, free of charge, to any person obtaining a copy of 
* this software and associated documentation files (the "Software"), to deal in the 
* Software without restriction, including without limitation the rights to use, copy, 
* modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
* and to permit persons to whom the Software is furnished to do so, subject to the 
* following conditions:
*
* The above copyright notice and this permission notice shall be included in all 
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
* INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
* PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
* FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
* DEALINGS IN THE SOFTWARE.
*/
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PCMTool.Tree.Tracking
{
    /// <summary>
    /// Class used for tracking time and calculating the average as well as the median.
    /// </summary>
    public class AverageValueTracker
    {
        private List<float> m_lastTimes = new List<float>();
        private int m_nextIndex;
        private int m_maxTimes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AverageValueTracker()
        {
            m_maxTimes = 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AverageValueTracker(int maxTimes)
        {
            m_maxTimes = maxTimes;
        }

        /// <summary>
        /// Add a measured time to the list. When the specified maximum amount of 
        /// tracked times is reached the first records will be replaced.
        /// </summary>
        public void AddTime(float time)
        {
            if (m_lastTimes.Count < m_maxTimes)
            {
                m_lastTimes.Add(time);
            }
            else
            {
                m_lastTimes[m_nextIndex] = time;
            }
            m_nextIndex = (m_nextIndex + 1) % m_maxTimes;
        }

        /// <summary>
        /// Calculates the average of recorded times.
        /// </summary>
        /// <returns> The average of recorded times.</returns>
        public float GetAverageTime()
        {
            float sum = 0.0f;
            foreach (var time in m_lastTimes)
            {
                sum += time;
            }
            return sum / m_lastTimes.Count;
        }

        /// <summary>
        /// Calculates the median of recorded times.
        /// </summary>
        /// <returns> The median of recorded times.</returns>
        public float GetMedian()
        {
            if (m_lastTimes.Count == 1) return m_lastTimes[0];
            var list = m_lastTimes.OrderBy(x => x).ToList();
            if (list.Count % 2 == 0)
            {
                return list[m_lastTimes.Count / 2];
            }
            else
            {
                return (list[m_lastTimes.Count / 2] + list[(m_lastTimes.Count / 2) + 1]) / 2.0f;
            }
        }
    }
}
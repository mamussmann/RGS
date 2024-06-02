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
using System;
using UnityEngine;
using System.Linq;

namespace RGS.Configurations.Root
{
    [CreateAssetMenu(fileName = "RootSGConfiguration", menuName = "SGR/Configurations/RootSGConfiguration", order = 1)]
    public class RootSGConfiguration : ScriptableObject
    {
        public WaterAgentConfig WaterAgentConfig;
        public float SegmentLength;
        public RootSGAgent[] RootSGAgents;

        public List<Tuple<Color,int>> GetPossibleRootTypesForAxiom(string axiom)
        {
            List<Tuple<Color,int>> result = new List<Tuple<Color,int>>();
            RecursiveGetPossibleRootTypesForAxiom(axiom, result);
            return result;
        }

        private void RecursiveGetPossibleRootTypesForAxiom(string axiom, List<Tuple<Color,int>> result)
        {
            foreach (var character in axiom)
            {
                int agentType = GetAgentTypeOfChar(character);
                if(result.Where(x => x.Item2 == agentType).Count() > 0) continue;
                result.Add(new Tuple<Color, int>(RootSGAgents[agentType].UnityColor, agentType));
                foreach (var rule in RootSGAgents[agentType].ProductionRules)
                {
                    RecursiveGetPossibleRootTypesForAxiom(rule.Output, result);
                }
            }
        }

        public int GetAgentTypeOfChar(char character)
        {
            for (int i = 0; i < RootSGAgents.Length; i++)
            {
                if(RootSGAgents[i].AgentType == character)
                {
                    return i;
                }
            }
            return -1;
        }
    }

}

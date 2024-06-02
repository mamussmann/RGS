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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace RGS.Agents
{

    [StructLayout(LayoutKind.Sequential)]
    public struct RootSGAgentData 
    {
        public float3 Position;
        public float3 Velocity;
        public float3 LookDirection;
        public float3 Acceleration;
        public int AgentType;
        public float ParentEmergenceDistance;
        public float LastBranchingDistance;
        public float LastPointPlaceDistance;
        public float LastSegmentPlaceDistance;
        public float TravelDistance;
        public float DeltaTravelDistance;
        public int PlantId;
        public long UniqueAgentId;
        public float EmergenceTime;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData(
            float3 position, float3 velocity, float3 acceleration,
            int agentType, float lastPointPlaceDistance, float lastSegmentPlaceDistance,
            float parentEmergenceDistance, float lastBranchingDistance,
            float travelDistance, float deltaTravelDistance,
            int plantId, long uniqueAgentId, float emergenceTime)
        {
            this.Position = position;
            this.Velocity = velocity;
            this.Acceleration = acceleration;
            this.AgentType = agentType;
            this.LastPointPlaceDistance = lastPointPlaceDistance;
            this.LastSegmentPlaceDistance = lastSegmentPlaceDistance;
            this.ParentEmergenceDistance = parentEmergenceDistance;
            this.LastBranchingDistance = lastBranchingDistance;
            this.TravelDistance = travelDistance;
            this.DeltaTravelDistance = deltaTravelDistance;
            this.PlantId = plantId;
            this.UniqueAgentId = uniqueAgentId;
            this.EmergenceTime = emergenceTime;
            this.LookDirection = velocity;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData MoveAgent(float3 newPosition, float3 newVelocity, float deltaTravelDistance)
        {
            this.Position = newPosition;
            this.Velocity = newVelocity;
            this.LookDirection = math.isnan(newVelocity).x || !math.any(newVelocity) ? this.LookDirection : newVelocity;
            this.TravelDistance += deltaTravelDistance;
            this.DeltaTravelDistance = deltaTravelDistance;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RootSGAgentData NewAgent(float3 position, float3 startVelocity,int agentType, int plantId, long uniqueAgentId, float emergenceTime)
        {
            return new RootSGAgentData(
                    position, startVelocity, float3.zero, 
                     agentType, 0.0f, 
                    0.0f, 0.0f, 0.0f, 
                    0.0f, 0.0f, plantId, 
                    uniqueAgentId, emergenceTime); 
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData SetAcceleration(float3 acceleration)
        {
            this.Acceleration = acceleration;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData SetLastBranchingDistance()
        {
            this.LastBranchingDistance = this.TravelDistance;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData SetPointPlaceDistance()
        {
            this.LastPointPlaceDistance = this.TravelDistance;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData SetSegmentPlaceDistance()
        {
            this.LastSegmentPlaceDistance = this.TravelDistance;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData SetEmerganceTime(float time)
        {
            this.EmergenceTime = time;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RootSGAgentData ApplyRule(int newAgentType, float3 velocity, long uniqueAgentId, float parentEmergenceDistance)
        {
            this.LastBranchingDistance = 0.0f;
            this.TravelDistance = 0.0f;
            this.AgentType = newAgentType;
            this.LastPointPlaceDistance = 0.0f;
            this.LastSegmentPlaceDistance = 0.0f;
            this.Velocity = velocity;
            //this.LookDirection = math.isnan(velocity).x || math.any(velocity) ? this.LookDirection : velocity;
            //this.LookDirection = math.select(velocity, this.LookDirection, math.isnan(velocity).x || math.all(velocity));
            this.Acceleration = float3.zero;
            this.UniqueAgentId = uniqueAgentId;
            this.ParentEmergenceDistance = parentEmergenceDistance;
            return this;
        }
    }

}
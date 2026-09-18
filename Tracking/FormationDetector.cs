using System.Collections.Generic;
using UnityEngine;

namespace AdminHelper
{
    internal static class FormationDetector
    {
        private const int MaxScratch = 16;

        private static readonly float[] ScratchX = new float[MaxScratch];
        private static readonly float[] ScratchZ = new float[MaxScratch];
        private static readonly float[] ScratchDistanceSquared = new float[MaxScratch];

        public static bool IsInFormation(PlayerSnapshot self, List<PlayerSnapshot> friendlies)
        {
            if (GameAccess.IsInsideOfficerLine(self.Player)) return true;

            int nearby = CollectNearest(self, friendlies, Settings.LineRadius.Value, Settings.LineMaxMates.Value);
            if (CountWithin(nearby, Settings.ClusterFormationRadius.Value) >= Settings.ClusterMinMates.Value)
                return true;

            if (nearby < Settings.LineMinMates.Value) return false;
            return PerpendicularSpread(self, nearby) <= Settings.LineResidual.Value;
        }

        private static int CollectNearest(PlayerSnapshot self, List<PlayerSnapshot> friendlies, float radius, int max)
        {
            if (max > MaxScratch) max = MaxScratch;

            float radiusSquared = radius * radius;
            int count = 0;

            for (int i = 0; i < friendlies.Count; i++)
            {
                PlayerSnapshot other = friendlies[i];
                if (other.PlayerId == self.PlayerId) continue;

                float dx = other.Position.x - self.Position.x;
                float dz = other.Position.z - self.Position.z;
                float distanceSquared = dx * dx + dz * dz;
                if (distanceSquared > radiusSquared) continue;

                if (count == max && distanceSquared >= ScratchDistanceSquared[count - 1]) continue;

                int slot = (count < max) ? count : count - 1;
                while (slot > 0 && ScratchDistanceSquared[slot - 1] > distanceSquared)
                {
                    ScratchDistanceSquared[slot] = ScratchDistanceSquared[slot - 1];
                    ScratchX[slot] = ScratchX[slot - 1];
                    ScratchZ[slot] = ScratchZ[slot - 1];
                    slot--;
                }

                ScratchDistanceSquared[slot] = distanceSquared;
                ScratchX[slot] = other.Position.x;
                ScratchZ[slot] = other.Position.z;
                if (count < max) count++;
            }

            return count;
        }

        private static int CountWithin(int count, float radius)
        {
            float radiusSquared = radius * radius;
            for (int i = 0; i < count; i++)
            {
                if (ScratchDistanceSquared[i] > radiusSquared) return i;
            }
            return count;
        }

        private static float PerpendicularSpread(PlayerSnapshot self, int count)
        {
            int total = count + 1;
            float sumX = self.Position.x;
            float sumZ = self.Position.z;
            for (int i = 0; i < count; i++)
            {
                sumX += ScratchX[i];
                sumZ += ScratchZ[i];
            }

            float meanX = sumX / total;
            float meanZ = sumZ / total;

            float xx = 0f;
            float xz = 0f;
            float zz = 0f;
            AccumulateCovariance(self.Position.x - meanX, self.Position.z - meanZ, ref xx, ref xz, ref zz);
            for (int i = 0; i < count; i++)
            {
                AccumulateCovariance(ScratchX[i] - meanX, ScratchZ[i] - meanZ, ref xx, ref xz, ref zz);
            }

            float trace = xx + zz;
            float difference = xx - zz;
            float root = Mathf.Sqrt(difference * difference + 4f * xz * xz);
            float minorEigenvalue = Mathf.Max(0f, (trace - root) * 0.5f);

            return Mathf.Sqrt(minorEigenvalue / total);
        }

        private static void AccumulateCovariance(float dx, float dz, ref float xx, ref float xz, ref float zz)
        {
            xx += dx * dx;
            xz += dx * dz;
            zz += dz * dz;
        }
    }
}

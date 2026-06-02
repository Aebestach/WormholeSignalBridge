using System;
using System.Reflection;
using RealAntennas;

namespace WormholeSignalBridge
{
    internal static class RealAntennasReflection
    {
        private static readonly Type PhysicsType = typeof(RACommNetwork).Assembly.GetType("RealAntennas.Physics");
        private static readonly MethodInfo PathLossMethod = PhysicsType?.GetMethod("PathLoss", new[] { typeof(double), typeof(double) });
        private static readonly MethodInfo NoiseTemperatureMethod = PhysicsType?.GetMethod("NoiseTemperature", new[] { typeof(RealAntenna), typeof(Vector3d) });
        private static readonly MethodInfo NoiseSpectralDensityMethod = PhysicsType?.GetMethod("NoiseSpectralDensity", new[] { typeof(float) });
        private static readonly MethodInfo MakeLinkMethod = typeof(RACommNetwork).GetMethod("MakeLink", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        internal static bool IsReady =>
            PhysicsType != null &&
            PathLossMethod != null &&
            NoiseTemperatureMethod != null &&
            NoiseSpectralDensityMethod != null &&
            MakeLinkMethod != null;

        internal static float PathLoss(double distance, double frequency)
        {
            return ToSingle(PathLossMethod.Invoke(null, new object[] { distance, frequency }));
        }

        internal static float NoiseTemperature(RealAntenna rx, Vector3d origin)
        {
            return ToSingle(NoiseTemperatureMethod.Invoke(null, new object[] { rx, origin }));
        }

        internal static float NoiseSpectralDensity(float noiseTemp)
        {
            return ToSingle(NoiseSpectralDensityMethod.Invoke(null, new object[] { noiseTemp }));
        }

        private static float ToSingle(object value) => Convert.ToSingle(value);

        internal static void MakeLink(
            RACommNetwork network,
            RealAntenna fwdTx,
            RealAntenna fwdRx,
            RealAntenna revTx,
            RealAntenna revRx,
            RACommNode a,
            RACommNode b,
            double distance,
            double fwdDataRate,
            double revDataRate,
            double fwdBestDataRate,
            double fwdMetric,
            double revMetric)
        {
            MakeLinkMethod.Invoke(network, new object[]
            {
                fwdTx, fwdRx, revTx, revRx, a, b, distance,
                fwdDataRate, revDataRate, fwdBestDataRate, fwdMetric, revMetric
            });
        }
    }
}

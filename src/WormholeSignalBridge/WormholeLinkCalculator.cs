using System;
using RealAntennas;
using RealAntennas.Antenna;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal readonly struct DirectionalLink
    {
        internal readonly RealAntenna Tx;
        internal readonly RealAntenna Rx;
        internal readonly double DataRate;
        internal readonly double MaxDataRate;
        internal readonly double Metric;

        internal DirectionalLink(RealAntenna tx, RealAntenna rx, double dataRate, double maxDataRate, double metric)
        {
            Tx = tx;
            Rx = rx;
            DataRate = dataRate;
            MaxDataRate = maxDataRate;
            Metric = metric;
        }
    }

    internal static class WormholeLinkCalculator
    {
        internal static DirectionalLink? BestDirectionalLink(RealAntenna tx, RealAntenna rx, WormholeLinkSettings settings)
        {
            if (tx == null || rx == null || !tx.Compatible(rx))
                return null;

            if (tx.Shape != AntennaShape.Omni || rx.Shape != AntennaShape.Omni)
                return null;

            if (!(tx is RealAntennaDigital txDigital) || !(rx is RealAntennaDigital rxDigital))
                return null;

            if (!txDigital.modulator.Compatible(rxDigital.modulator))
                return null;

            LinkBudget budget = ComputeBudget(tx, rx, settings);
            if (budget.DataRate <= 0)
                return null;

            return new DirectionalLink(tx, rx, budget.DataRate, budget.MaxDataRate, budget.Metric);
        }

        private static LinkBudget ComputeBudget(RealAntenna tx, RealAntenna rx, WormholeLinkSettings settings)
        {
            float pathLoss = RealAntennasReflection.PathLoss(settings.EffectiveDistance, tx.Frequency);
            pathLoss += (float)settings.InsertionLoss;

            float rxPower = tx.TxPower + tx.Gain - pathLoss + rx.Gain;
            float noiseTemp = RealAntennasReflection.NoiseTemperature(rx, tx.Position);
            float n0 = RealAntennasReflection.NoiseSpectralDensity(noiseTemp);

            Encoder encoder = Encoder.BestMatching(tx.Encoder, rx.Encoder);
            float minEb = encoder.RequiredEbN0 + n0;

            float maxBitRateLog = rxPower - minEb;
            float maxBitRate = RATools.LinearScale(maxBitRateLog);

            RealAntennaDigital txDigital = (RealAntennaDigital)tx;
            RealAntennaDigital rxDigital = (RealAntennaDigital)rx;

            float maxSymbolRate = Mathf.Min((float)txDigital.SymbolRate, (float)rxDigital.SymbolRate);
            float minSymbolRate = Mathf.Max((float)txDigital.MinSymbolRate, (float)rxDigital.MinSymbolRate);
            int maxModulationBits = Mathf.Min(txDigital.modulator.ModulationBits, rxDigital.modulator.ModulationBits);

            if (minSymbolRate > maxSymbolRate)
                return default;

            float maxDataRate = maxSymbolRate * encoder.CodingRate * (1 << (maxModulationBits - 1));
            float minDataRate = minSymbolRate * encoder.CodingRate;
            int maxSteps = minDataRate > 0 ? (int)Mathf.Floor(Mathf.Log(maxDataRate / minDataRate, 2f)) : 0;

            float targetRate;
            int negotiatedBits;
            if (maxBitRate < minSymbolRate)
            {
                targetRate = 0;
                negotiatedBits = 0;
            }
            else if (maxBitRate <= maxSymbolRate)
            {
                float ratio = maxBitRate / maxSymbolRate;
                int log2 = (int)Mathf.Floor(Mathf.Log(ratio, 2f));
                targetRate = maxSymbolRate * Mathf.Pow(2f, log2);
                negotiatedBits = 1;
            }
            else
            {
                float margin = rxPower - minEb - RATools.LogScale(maxSymbolRate);
                margin = Mathf.Clamp(margin, 0f, 100f);
                negotiatedBits = Mathf.Min(maxModulationBits, 1 + (int)Mathf.Floor(margin / 3f));
                targetRate = maxSymbolRate;
            }

            float dataRate = targetRate * encoder.CodingRate * (1 << (negotiatedBits - 1));
            int rateSteps = dataRate > 0 && maxDataRate > 0
                ? (int)Mathf.Floor(Mathf.Log((float)(maxDataRate / dataRate), 2f))
                : 0;
            float metric = 1f - (rateSteps / (maxSteps + 1f));

            return new LinkBudget(dataRate, maxDataRate, metric);
        }

        private readonly struct LinkBudget
        {
            internal readonly float DataRate;
            internal readonly float MaxDataRate;
            internal readonly float Metric;

            internal LinkBudget(float dataRate, float maxDataRate, float metric)
            {
                DataRate = dataRate;
                MaxDataRate = maxDataRate;
                Metric = metric;
            }
        }
    }
}

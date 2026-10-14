using System;

namespace Coastline
{
    public static class RaceClassification
    {
        public static int Compare(RaceEntrant a, RaceEntrant b)
        {
            if (a.Progress.Finished != b.Progress.Finished)
                return a.Progress.Finished ? -1 : 1;
            int order = a.Progress.Finished
                ? a.AdjustedTime.CompareTo(b.AdjustedTime)
                : b.Progress.AbsoluteDistance.CompareTo(a.Progress.AbsoluteDistance);
            if (order == 0 && a.Progress.Finished)
                order = a.FinishTime.CompareTo(b.FinishTime);
            return order != 0 ? order : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }
    }
}

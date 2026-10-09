/*
 * Code Attribution:
 * The order status constants and lifecycle validation logic were
 * implemented for the CLDV6212 CoffeeNChill project to control the
 * order progression from Received to Preparing, Ready and Collected.
 *
 * CLDV6212 POE brief.
 */

using System.Collections.Generic;

namespace CoffeeNChillFunctions.Models
{
    public static class OrderStatus
    {
        public const string Received = "Received";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string Collected = "Collected";

        public static readonly string[] Lifecycle = { Received, Preparing, Ready, Collected };

        public static bool IsValid(string status) => Lifecycle.Contains(status);

        // True only when 'next' is exactly one step after 'current' (no skipping, no going back)
        public static bool IsNextStep(string current, string next)
        {
            int c = Array.IndexOf(Lifecycle, current);
            int n = Array.IndexOf(Lifecycle, next);
            return c >= 0 && n == c + 1;
        }
    }
}
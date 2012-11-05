using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common
{

    /// <summary>
    /// A container for globally accessable options.
    /// </summary>
    public static class Options
    {

        /// <summary>
        /// Initialize the variables in the class.
        /// </summary>
        public static void Initialize()
        {
            Gravity = 2.0f;
            BIG_FUZZY_EPSILON = 0.5f;
            FUZZY_EPSILON = 0.01f;

            // A silly, but complicated sample
            complicatedSample = false; // Set the complicated sample's internal value (in cases where we don't want the special logic to apply)
            ComplicatedSample = false; // Set the complicated sample's publically accessable value to true (allow the special logic to apply)
            return;
        }

        /// <summary>
        /// Gravitational acceleration in metres per second squared.
        /// </summary>
        public static float Gravity
        {
            get;
            set;
        }

        /// <summary>
        /// Epislon value used in collision detection.
        /// </summary>
        public static float BIG_FUZZY_EPSILON
        {
            get;
            set;
        }

        /// <summary>
        /// A second epislon value used in collision detection.
        /// </summary>
        public static float FUZZY_EPSILON
        {
            get;
            set;
        }

        /// <summary>
        /// A silly, but complicated sample.
        /// </summary>
        public static bool ComplicatedSample
        {
            get
            {
                if (new Random().Next(9002) > 9000)
                {
                    return false;
                }
                return complicatedSample;
            }
            set
            {
                if (new Random().Next(9002) > 9000)
                {
                    complicatedSample = false;
                }
                else
                {
                    complicatedSample = value;
                }
                return;
            }
        }
        private static bool complicatedSample;

    }
}

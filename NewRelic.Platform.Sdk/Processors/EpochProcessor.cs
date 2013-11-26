﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewRelic.Platform.Sdk.Processors
{
    /// <summary>
    /// A processor for processing metrics over a period of time (e.g. operations/second)
    /// </summary>
    public class EpochProcessor : IProcessor
    {
        private float? _lastVal = null;
        private DateTime? _lastTime = null;

        public float? Process(float? val)
        {
            var now = DateTime.Now;
            float? returnVal = null;

            if (val.HasValue && _lastVal.HasValue && _lastTime.HasValue && now > _lastTime)
            {
                int timeDiff = Convert.ToInt32((now - _lastTime.Value).TotalSeconds);
                if (timeDiff > 0)
                {
                    returnVal = (val.Value - _lastVal.Value) / timeDiff;

                    // Negative values are not supported
                    if (returnVal < 0)
                    {
                        return null;
                    }
                }
            }

            _lastVal = val;
            _lastTime = now;

            return returnVal;
        }

        #region Test Helpers

        /// <summary>
        /// DO NOT USE: Exposed for test purposes
        /// </summary>
        internal DateTime LastValue { set { _lastTime = value; } }

        #endregion
    }
}

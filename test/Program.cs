using System;
using StreamUtilities;

namespace test
{
    internal class Program
    {
        #region Private Methods

        private static void Main(string[] args)
        {
            var stream = new TemporaryStream(5);
            stream.Dispose();
            stream.Dispose();
        }

        #endregion Private Methods
    }
}
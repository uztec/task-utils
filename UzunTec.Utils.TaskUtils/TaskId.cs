using System;

namespace UzunTec.Utils.TaskUtils
{
    internal static class TaskId
    {
        private static readonly char[] availablesChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly int acLenght;
        private static readonly char lastAc;
        private static string currentId;
        private static readonly object threadSafeLock = new object();

        static TaskId()
        {
            currentId = "";
            acLenght = availablesChars.Length;
            lastAc = availablesChars[acLenght - 1];
        }

        internal static string GetNextId()
        {
            lock (threadSafeLock)
            {
                currentId = CalcNextId(currentId);
                return currentId;
            }
        }

        private static string CalcNextId(string currrentId)
        {
            int len = currrentId.Length;
            if (len == 0)
            {
                return "" + availablesChars[0];
            }
            else
            {
                char lastChar = currrentId[len - 1];

                if (lastChar == lastAc)
                {
                    return CalcNextId(currrentId.Substring(0, len - 1)) + availablesChars[0];
                }
                else
                {
                    int index = Array.IndexOf(availablesChars, lastChar);
                    return currrentId.Substring(0, len - 1) + availablesChars[index + 1];
                }
            }
        }
    }
}

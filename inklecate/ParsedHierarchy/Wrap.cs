﻿
namespace Ink.Parsed
{
    internal class Wrap<T> : Parsed.Object where T : Runtime.Object
    {
        public Wrap (T objToWrap)
        {
            _objToWrap = objToWrap;
        }

        public override Runtime.Object GenerateRuntimeObject ()
        {
            return _objToWrap;
        }

        T _objToWrap;
    }
}


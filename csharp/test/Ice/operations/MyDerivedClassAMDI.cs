//
// Copyright (c) ZeroC, Inc. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Test;

namespace ZeroC.Ice.Test.Operations
{
    public sealed class MyDerivedClassAsync : IMyDerivedClassAsync
    {
        private readonly object _mutex = new object();
        internal class Thread_opVoid : TaskCompletionSource<object?>
        {
            private readonly object _mutex = new object();

            public void Start()
            {
                lock (_mutex)
                {
                    _thread = new Thread(new ThreadStart(Run));
                    _thread.Start();
                }
            }

            public void Run() => SetResult(null);

            public void Join()
            {
                lock (_mutex)
                {
                    _thread!.Join();
                }
            }

            private Thread? _thread;
        }

        //
        // Override the Object "pseudo" operations to verify the operation mode.
        //
        public bool IceIsA(string id, Current current)
        {
            TestHelper.Assert(current.IsIdempotent);
            return typeof(IMyDerivedClass).GetAllIceTypeIds().Contains(id);
        }

        public void IcePing(Current current) => TestHelper.Assert(current.IsIdempotent);

        public IEnumerable<string> IceIds(Current current)
        {
            TestHelper.Assert(current.IsIdempotent);
            return typeof(IMyDerivedClass).GetAllIceTypeIds();
        }

        public string IceId(Current current)
        {
            TestHelper.Assert(current.IsIdempotent);
            return typeof(IMyDerivedClass).GetIceTypeId()!;
        }

        public ValueTask shutdownAsync(Current current)
        {
            while (_opVoidThread != null)
            {
                _opVoidThread.Join();
                _opVoidThread = null;
            }

            current.Adapter.Communicator.ShutdownAsync();
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask<bool> supportsCompressAsync(Current current) => new ValueTask<bool>(true);

        public ValueTask opVoidAsync(Current current)
        {
            TestHelper.Assert(!current.IsIdempotent);

            while (_opVoidThread != null)
            {
                _opVoidThread.Join();
                _opVoidThread = null;
            }

            _opVoidThread = new Thread_opVoid();
            _opVoidThread.Start();
            return new ValueTask(_opVoidThread.Task);
        }

        public ValueTask<(bool, bool)> opBoolAsync(bool p1, bool p2, Current current) =>
            new ValueTask<(bool, bool)>((p2, p1));

        public ValueTask<(ReadOnlyMemory<bool>, ReadOnlyMemory<bool>)> opBoolSAsync(bool[] p1, bool[] p2,
            Current current)
        {
            bool[] p3 = new bool[p1.Length + p2.Length];
            Array.Copy(p1, p3, p1.Length);
            Array.Copy(p2, 0, p3, p1.Length, p2.Length);

            bool[] r = new bool[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }

            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<bool[]>, IEnumerable<bool[]>)>
        opBoolSSAsync(bool[][] p1, bool[][] p2, Current current)
        {
            bool[][] p3 = new bool[p1.Length + p2.Length][];
            Array.Copy(p1, p3, p1.Length);
            Array.Copy(p2, 0, p3, p1.Length, p2.Length);

            bool[][] r = new bool[p1.Length][];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }

            return ToReturnValue(r, p3);
        }

        public ValueTask<(byte, byte)> opByteAsync(byte p1, byte p2, Current current) =>
            new ValueTask<(byte, byte)>((p1, (byte)(p1 ^ p2)));

        public ValueTask<(IReadOnlyDictionary<byte, bool>, IReadOnlyDictionary<byte, bool>)>
        opByteBoolDAsync(Dictionary<byte, bool> p1, Dictionary<byte, bool> p2, Current current)
        {
            Dictionary<byte, bool> p3 = p1;
            var r = new Dictionary<byte, bool>();
            foreach (KeyValuePair<byte, bool> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<byte, bool> e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> opByteSAsync(byte[] p1, byte[] p2,
            Current current)
        {
            byte[] p3 = new byte[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                p3[i] = p1[p1.Length - (i + 1)];
            }

            byte[] r = new byte[p1.Length + p2.Length];
            Array.Copy(p1, r, p1.Length);
            Array.Copy(p2, 0, r, p1.Length, p2.Length);

            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<byte[]>, IEnumerable<byte[]>)>
        opByteSSAsync(byte[][] p1, byte[][] p2, Current current)
        {
            byte[][] p3 = new byte[p1.Length][];
            for (int i = 0; i < p1.Length; i++)
            {
                p3[i] = p1[p1.Length - (i + 1)];
            }

            byte[][] r = new byte[p1.Length + p2.Length][];
            Array.Copy(p1, r, p1.Length);
            Array.Copy(p2, 0, r, p1.Length, p2.Length);

            return ToReturnValue(r, p3);
        }

        public ValueTask<(double, float, double)> opFloatDoubleAsync(float p1, double p2, Current current) =>
            new ValueTask<(double, float, double)>((p2, p1, p2));

        public ValueTask<(ReadOnlyMemory<double>, ReadOnlyMemory<float>, ReadOnlyMemory<double>)> opFloatDoubleSAsync(
            float[] p1, double[] p2, Current current)
        {
            float[] p3 = p1;

            double[] p4 = new double[p2.Length];
            for (int i = 0; i < p2.Length; i++)
            {
                p4[i] = p2[p2.Length - (i + 1)];
            }

            double[] r = new double[p2.Length + p1.Length];
            Array.Copy(p2, r, p2.Length);
            for (int i = 0; i < p1.Length; i++)
            {
                r[p2.Length + i] = p1[i];
            }

            return new ValueTask<(ReadOnlyMemory<double>, ReadOnlyMemory<float>, ReadOnlyMemory<double>)>((r, p3, p4));
        }

        public ValueTask<(IEnumerable<double[]>, IEnumerable<float[]>, IEnumerable<double[]>)>
        opFloatDoubleSSAsync(float[][] p1, double[][] p2, Current current)
        {
            var p3 = p1;

            var p4 = new double[p2.Length][];
            for (int i = 0; i < p2.Length; i++)
            {
                p4[i] = p2[p2.Length - (i + 1)];
            }

            var r = new double[p2.Length + p2.Length][];
            Array.Copy(p2, r, p2.Length);
            for (int i = 0; i < p2.Length; i++)
            {
                r[p2.Length + i] = new double[p2[i].Length];
                for (int j = 0; j < p2[i].Length; j++)
                {
                    r[p2.Length + i][j] = p2[i][j];
                }
            }

            return new ValueTask<(IEnumerable<double[]>, IEnumerable<float[]>, IEnumerable<double[]>)>((r, p3, p4 ));
        }

        public ValueTask<(IReadOnlyDictionary<long, float>, IReadOnlyDictionary<long, float>)>
        opLongFloatDAsync(Dictionary<long, float> p1, Dictionary<long, float> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<long, float>();
            foreach (KeyValuePair<long, float> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<long, float> e in p2)
            {
                r[e.Key] = e.Value;
            }

            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<ulong, float>, IReadOnlyDictionary<ulong, float>)>
        opULongFloatDAsync(Dictionary<ulong, float> p1, Dictionary<ulong, float> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<ulong, float>();
            foreach (KeyValuePair<ulong, float> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<ulong, float> e in p2)
            {
                r[e.Key] = e.Value;
            }

            return ToReturnValue(r, p3);
        }

        public ValueTask<(IMyClassPrx?, IMyClassPrx?, IMyClassPrx?)>
        opMyClassAsync(IMyClassPrx? p1, Current current)
        {
            var p2 = p1;
            var p3 = current.Adapter.CreateProxy("noSuchIdentity", IMyClassPrx.Factory);
            return new ValueTask<(IMyClassPrx?, IMyClassPrx?, IMyClassPrx?)>((
                current.Adapter.CreateProxy(current.Identity, IMyClassPrx.Factory), p2, p3));
        }

        public ValueTask<(MyEnum, MyEnum)> opMyEnumAsync(MyEnum p1, Current current) =>
            new ValueTask<(MyEnum, MyEnum)>((MyEnum.enum3, p1));

        public ValueTask<(IReadOnlyDictionary<short, int>, IReadOnlyDictionary<short, int>)>
        opShortIntDAsync(Dictionary<short, int> p1, Dictionary<short, int> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<short, int>();
            foreach (KeyValuePair<short, int> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<short, int> e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<ushort, uint>, IReadOnlyDictionary<ushort, uint>)>
        opUShortUIntDAsync(Dictionary<ushort, uint> p1, Dictionary<ushort, uint> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<ushort, uint>();
            foreach (KeyValuePair<ushort, uint> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<ushort, uint> e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(long, short, int, long)> opShortIntLongAsync(short p1, int p2, long p3, Current current) =>
            new ValueTask<(long, short, int, long)>((p3, p1, p2, p3));

        public ValueTask<(ulong, ushort, uint, ulong)> opUShortUIntULongAsync(
            ushort p1, uint p2, ulong p3, Current current) =>
            new ValueTask<(ulong, ushort, uint, ulong)>((p3, p1, p2, p3));

        public ValueTask<int> opVarIntAsync(int v, Current current) => new ValueTask<int>(v);
        public ValueTask<uint> opVarUIntAsync(uint v, Current current) => new ValueTask<uint>(v);
        public ValueTask<long> opVarLongAsync(long v, Current current) => new ValueTask<long>(v);
        public ValueTask<ulong> opVarULongAsync(ulong v, Current current) => new ValueTask<ulong>(v);

        public ValueTask<(ReadOnlyMemory<long>, ReadOnlyMemory<short>, ReadOnlyMemory<int>, ReadOnlyMemory<long>)>
        opShortIntLongSAsync(short[] p1, int[] p2, long[] p3, Current current)
        {
            var p4 = p1;
            var p5 = new int[p2.Length];
            for (int i = 0; i < p2.Length; i++)
            {
                p5[i] = p2[p2.Length - (i + 1)];
            }
            var p6 = new long[p3.Length + p3.Length];
            Array.Copy(p3, p6, p3.Length);
            Array.Copy(p3, 0, p6, p3.Length, p3.Length);
            return new ValueTask<(
                ReadOnlyMemory<long>,
                ReadOnlyMemory<short>,
                ReadOnlyMemory<int>,
                ReadOnlyMemory<long>)>((p3, p4, p5, p6));
        }

        public ValueTask<(ReadOnlyMemory<ulong>, ReadOnlyMemory<ushort>, ReadOnlyMemory<uint>, ReadOnlyMemory<ulong>)>
        opUShortUIntULongSAsync(ushort[] p1, uint[] p2, ulong[] p3, Current current)
        {
            var p4 = p1;
            var p5 = new uint[p2.Length];
            for (int i = 0; i < p2.Length; i++)
            {
                p5[i] = p2[p2.Length - (i + 1)];
            }
            var p6 = new ulong[p3.Length + p3.Length];
            Array.Copy(p3, p6, p3.Length);
            Array.Copy(p3, 0, p6, p3.Length, p3.Length);
            return new ValueTask<(
                ReadOnlyMemory<ulong>,
                ReadOnlyMemory<ushort>,
                ReadOnlyMemory<uint>,
                ReadOnlyMemory<ulong>)>((p3, p4, p5, p6));
        }

        public ValueTask<(IEnumerable<long>, IEnumerable<int>, IEnumerable<long>)>
        opVarIntVarLongSAsync(int[] p1, long[] p2, Current current)
        {
            var p4 = new int[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                p4[i] = p1[p1.Length - (i + 1)];
            }

            var p5 = new long[p2.Length + p2.Length];
            Array.Copy(p2, p5, p2.Length);
            Array.Copy(p2, 0, p5, p2.Length, p2.Length);

            return new ValueTask<(IEnumerable<long>, IEnumerable<int>, IEnumerable<long>)>((p2, p4, p5));
        }

        public ValueTask<(IEnumerable<ulong>, IEnumerable<uint>, IEnumerable<ulong>)>
        opVarUIntVarULongSAsync(uint[] p1, ulong[] p2, Current current)
        {
            var p4 = new uint[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                p4[i] = p1[p1.Length - (i + 1)];
            }

            var p5 = new ulong[p2.Length + p2.Length];
            Array.Copy(p2, p5, p2.Length);
            Array.Copy(p2, 0, p5, p2.Length, p2.Length);

            return new ValueTask<(IEnumerable<ulong>, IEnumerable<uint>, IEnumerable<ulong>)>((p2, p4, p5));
        }

        public ValueTask<(IEnumerable<long[]>, IEnumerable<short[]>, IEnumerable<int[]>, IEnumerable<long[]>)>
        opShortIntLongSSAsync(short[][] p1, int[][] p2, long[][] p3, Current current)
        {
            var p4 = p1;

            var p5 = new int[p2.Length][];
            for (int i = 0; i < p2.Length; i++)
            {
                p5[i] = p2[p2.Length - (i + 1)];
            }

            var p6 = new long[p3.Length + p3.Length][];
            Array.Copy(p3, p6, p3.Length);
            Array.Copy(p3, 0, p6, p3.Length, p3.Length);
            return new ValueTask<(IEnumerable<long[]>, IEnumerable<short[]>, IEnumerable<int[]>, IEnumerable<long[]>)>(
                (p3, p4, p5, p6));
        }

        public ValueTask<(IEnumerable<ulong[]>, IEnumerable<ushort[]>, IEnumerable<uint[]>, IEnumerable<ulong[]>)>
        opUShortUIntULongSSAsync(ushort[][] p1, uint[][] p2, ulong[][] p3, Current current)
        {
            var p4 = p1;

            var p5 = new uint[p2.Length][];
            for (int i = 0; i < p2.Length; i++)
            {
                p5[i] = p2[p2.Length - (i + 1)];
            }

            var p6 = new ulong[p3.Length + p3.Length][];
            Array.Copy(p3, p6, p3.Length);
            Array.Copy(p3, 0, p6, p3.Length, p3.Length);
            return new ValueTask<(
                IEnumerable<ulong[]>,
                IEnumerable<ushort[]>,
                IEnumerable<uint[]>,
                IEnumerable<ulong[]>)>((p3, p4, p5, p6));
        }

        public ValueTask<(string, string)>
        opStringAsync(string p1, string p2, Current current) =>
            new ValueTask<(string, string)>(($"{p1} {p2}", $"{p2} {p1}"));

        public ValueTask<(IReadOnlyDictionary<string, MyEnum>, IReadOnlyDictionary<string, MyEnum>)>
        opStringMyEnumDAsync(Dictionary<string, MyEnum> p1, Dictionary<string, MyEnum> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<string, MyEnum>();
            foreach (KeyValuePair<string, MyEnum> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<string, MyEnum> e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<MyEnum, string>, IReadOnlyDictionary<MyEnum, string>)>
        opMyEnumStringDAsync(Dictionary<MyEnum, string> p1, Dictionary<MyEnum, string> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<MyEnum, string>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<MyEnum, string> e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<MyStruct, MyEnum>, IReadOnlyDictionary<MyStruct, MyEnum>)>
        opMyStructMyEnumDAsync(Dictionary<MyStruct, MyEnum> p1,
                               Dictionary<MyStruct, MyEnum> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<MyStruct, MyEnum>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<byte, bool>>, IEnumerable<Dictionary<byte, bool>>)>
        opByteBoolDSAsync(Dictionary<byte, bool>[] p1, Dictionary<byte, bool>[] p2, Current current)
        {
            var p3 = new Dictionary<byte, bool>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<byte, bool>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<short, int>>, IEnumerable<Dictionary<short, int>>)>
        opShortIntDSAsync(Dictionary<short, int>[] p1, Dictionary<short, int>[] p2, Current current)
        {
            var p3 = new Dictionary<short, int>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<short, int>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<ushort, uint>>, IEnumerable<Dictionary<ushort, uint>>)>
        opUShortUIntDSAsync(Dictionary<ushort, uint>[] p1, Dictionary<ushort, uint>[] p2, Current current)
        {
            var p3 = new Dictionary<ushort, uint>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<ushort, uint>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<long, float>>, IEnumerable<Dictionary<long, float>>)>
        opLongFloatDSAsync(Dictionary<long, float>[] p1, Dictionary<long, float>[] p2, Current current)
        {
            var p3 = new Dictionary<long, float>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<long, float>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<ulong, float>>, IEnumerable<Dictionary<ulong, float>>)>
        opULongFloatDSAsync(Dictionary<ulong, float>[] p1, Dictionary<ulong, float>[] p2, Current current)
        {
            var p3 = new Dictionary<ulong, float>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<ulong, float>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<string, string>>, IEnumerable<Dictionary<string, string>>)>
        opStringStringDSAsync(Dictionary<string, string>[] p1, Dictionary<string, string>[] p2, Current current)
        {
            var p3 = new Dictionary<string, string>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<string, string>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<string, MyEnum>>, IEnumerable<Dictionary<string, MyEnum>>)>
        opStringMyEnumDSAsync(Dictionary<string, MyEnum>[] p1, Dictionary<string, MyEnum>[] p2, Current current)
        {
            var p3 = new Dictionary<string, MyEnum>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<string, MyEnum>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<MyEnum, string>>, IEnumerable<Dictionary<MyEnum, string>>)>
        opMyEnumStringDSAsync(Dictionary<MyEnum, string>[] p1, Dictionary<MyEnum, string>[] p2, Current current)
        {
            var p3 = new Dictionary<MyEnum, string>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<MyEnum, string>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<Dictionary<MyStruct, MyEnum>>, IEnumerable<Dictionary<MyStruct, MyEnum>>)>
        opMyStructMyEnumDSAsync(Dictionary<MyStruct, MyEnum>[] p1,
                                Dictionary<MyStruct, MyEnum>[] p2,
                                Current current)
        {
            var p3 = new Dictionary<MyStruct, MyEnum>[p1.Length + p2.Length];
            Array.Copy(p2, p3, p2.Length);
            Array.Copy(p1, 0, p3, p2.Length, p1.Length);

            var r = new Dictionary<MyStruct, MyEnum>[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<byte, byte[]>, IReadOnlyDictionary<byte, byte[]>)>
        opByteByteSDAsync(Dictionary<byte, byte[]> p1, Dictionary<byte, byte[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<byte, byte[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<bool, bool[]>, IReadOnlyDictionary<bool, bool[]>)>
        opBoolBoolSDAsync(Dictionary<bool, bool[]> p1, Dictionary<bool, bool[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<bool, bool[]>();
            foreach (KeyValuePair<bool, bool[]> e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (KeyValuePair<bool, bool[]> e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<short, short[]>, IReadOnlyDictionary<short, short[]>)>
        opShortShortSDAsync(Dictionary<short, short[]> p1, Dictionary<short, short[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<short, short[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<ushort, ushort[]>, IReadOnlyDictionary<ushort, ushort[]>)>
        opUShortUShortSDAsync(Dictionary<ushort, ushort[]> p1, Dictionary<ushort, ushort[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<ushort, ushort[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<int, int[]>, IReadOnlyDictionary<int, int[]>)>
        opIntIntSDAsync(Dictionary<int, int[]> p1, Dictionary<int, int[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<int, int[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<uint, uint[]>, IReadOnlyDictionary<uint, uint[]>)>
        opUIntUIntSDAsync(Dictionary<uint, uint[]> p1, Dictionary<uint, uint[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<uint, uint[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<long, long[]>, IReadOnlyDictionary<long, long[]>)>
        opLongLongSDAsync(Dictionary<long, long[]> p1, Dictionary<long, long[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<long, long[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<ulong, ulong[]>, IReadOnlyDictionary<ulong, ulong[]>)>
        opULongULongSDAsync(Dictionary<ulong, ulong[]> p1, Dictionary<ulong, ulong[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<ulong, ulong[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<string, float[]>, IReadOnlyDictionary<string, float[]>)>
        opStringFloatSDAsync(Dictionary<string, float[]> p1, Dictionary<string, float[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<string, float[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<string, double[]>, IReadOnlyDictionary<string, double[]>)>
        opStringDoubleSDAsync(Dictionary<string, double[]> p1, Dictionary<string, double[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<string, double[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<string, string[]>, IReadOnlyDictionary<string, string[]>)>
        opStringStringSDAsync(Dictionary<string, string[]> p1, Dictionary<string, string[]> p2, Current current)
        {
            var p3 = p2;
            var r = new Dictionary<string, string[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<MyEnum, MyEnum[]>, IReadOnlyDictionary<MyEnum, MyEnum[]>)>
        opMyEnumMyEnumSDAsync(Dictionary<MyEnum, MyEnum[]> p1, Dictionary<MyEnum, MyEnum[]> p2,
            Current current)
        {
            var p3 = p2;
            var r = new Dictionary<MyEnum, MyEnum[]>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<ReadOnlyMemory<int>> opIntSAsync(int[] s, Current current)
        {
            var r = new int[s.Length];
            for (int i = 0; i < s.Length; ++i)
            {
                r[i] = -s[i];
            }
            return new ValueTask<ReadOnlyMemory<int>>(r);
        }

        public ValueTask<IReadOnlyDictionary<string, string>> opContextAsync(Current current) =>
            new ValueTask<IReadOnlyDictionary<string, string>>(current.Context);

        public ValueTask
        opByteSOnewayAsync(byte[] s, Current current)
        {
            lock (_mutex)
            {
                ++_opByteSOnewayCallCount;
            }
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask<int>
        opByteSOnewayCallCountAsync(Current current)
        {
            lock (_mutex)
            {
                var count = _opByteSOnewayCallCount;
                _opByteSOnewayCallCount = 0;
                return new ValueTask<int>(count);
            }
        }

        public ValueTask
        opDoubleMarshalingAsync(double p1, double[] p2, Current current)
        {
            var d = 1278312346.0 / 13.0;
            TestHelper.Assert(p1 == d);
            for (int i = 0; i < p2.Length; ++i)
            {
                TestHelper.Assert(p2[i] == d);
            }
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask<(IEnumerable<string>, IEnumerable<string>)>
        opStringSAsync(string[] p1, string[] p2, Current current)
        {
            var p3 = new string[p1.Length + p2.Length];
            Array.Copy(p1, p3, p1.Length);
            Array.Copy(p2, 0, p3, p1.Length, p2.Length);

            var r = new string[p1.Length];
            for (int i = 0; i < p1.Length; i++)
            {
                r[i] = p1[p1.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<string[]>, IEnumerable<string[]>)>
        opStringSSAsync(string[][] p1, string[][] p2, Current current)
        {
            var p3 = new string[p1.Length + p2.Length][];
            Array.Copy(p1, p3, p1.Length);
            Array.Copy(p2, 0, p3, p1.Length, p2.Length);
            var r = new string[p2.Length][];
            for (int i = 0; i < p2.Length; i++)
            {
                r[i] = p2[p2.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IEnumerable<string[][]>, IEnumerable<string[][]>)>
        opStringSSSAsync(string[][][] p1, string[][][] p2, Current current)
        {
            var p3 = new string[p1.Length + p2.Length][][];
            Array.Copy(p1, p3, p1.Length);
            Array.Copy(p2, 0, p3, p1.Length, p2.Length);

            var r = new string[p2.Length][][];
            for (int i = 0; i < p2.Length; i++)
            {
                r[i] = p2[p2.Length - (i + 1)];
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(IReadOnlyDictionary<string, string>, IReadOnlyDictionary<string, string>)>
        opStringStringDAsync(Dictionary<string, string> p1, Dictionary<string, string> p2, Current current)
        {
            var p3 = p1;
            var r = new Dictionary<string, string>();
            foreach (var e in p1)
            {
                r[e.Key] = e.Value;
            }
            foreach (var e in p2)
            {
                r[e.Key] = e.Value;
            }
            return ToReturnValue(r, p3);
        }

        public ValueTask<(Structure, Structure)>
        opStructAsync(Structure p1, Structure p2, Current current)
        {
            var p3 = p1;
            p3.s.s = "a new string";
            return new ValueTask<(Structure, Structure)>((p2, p3));
        }

        public ValueTask
        opIdempotentAsync(Current current)
        {
            TestHelper.Assert(current.IsIdempotent);
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask opOnewayAsync(Current current)
        {
            // "return" exception when called two-way, otherwise succeeds.
            throw new SomeException();
        }

        // "return" exception when called two-way, otherwise succeeds.
        public ValueTask opOnewayMetadataAsync(Current current) => throw new SomeException();

        public ValueTask
        opDerivedAsync(Current current) => new ValueTask(Task.CompletedTask);

        public ValueTask<byte>
        opByte1Async(byte value, Current current) => new ValueTask<byte>(value);

        public ValueTask<short> opShort1Async(short value, Current current) => new ValueTask<short>(value);
        public ValueTask<int> opInt1Async(int value, Current current) => new ValueTask<int>(value);
        public ValueTask<long> opLong1Async(long value, Current current) => new ValueTask<long>(value);

        public ValueTask<ushort> opUShort1Async(ushort value, Current current) => new ValueTask<ushort>(value);
        public ValueTask<uint> opUInt1Async(uint value, Current current) => new ValueTask<uint>(value);
        public ValueTask<ulong> opULong1Async(ulong value, Current current) => new ValueTask<ulong>(value);

        public ValueTask<float>
        opFloat1Async(float value, Current current) => new ValueTask<float>(value);

        public ValueTask<double>
        opDouble1Async(double value, Current current) => new ValueTask<double>(value);

        public ValueTask<string>
        opString1Async(string value, Current current) => new ValueTask<string>(value);

        public ValueTask<IEnumerable<string>>
        opStringS1Async(string[] value, Current current) => new ValueTask<IEnumerable<string>>(value);

        public ValueTask<IReadOnlyDictionary<byte, bool>>
        opByteBoolD1Async(Dictionary<byte, bool> value, Current current) =>
            new ValueTask<IReadOnlyDictionary<byte, bool>>(value);

        public ValueTask<IEnumerable<string>>
        opStringS2Async(string[] value, Current current) => new ValueTask<IEnumerable<string>>(value);

        public ValueTask<IReadOnlyDictionary<byte, bool>>
        opByteBoolD2Async(Dictionary<byte, bool> value, Current current) =>
            new ValueTask<IReadOnlyDictionary<byte, bool>>(value);

        public ValueTask<MyClass1?>
        opMyClass1Async(MyClass1? value, Current current) => new ValueTask<MyClass1?>(value);

        public ValueTask<MyStruct1>
        opMyStruct1Async(MyStruct1 value, Current current) => new ValueTask<MyStruct1>(value);

        public ValueTask<IEnumerable<string>>
        opStringLiteralsAsync(Current current)
        {
            return new ValueTask<IEnumerable<string>>(new string[]
                {
                        Constants.s0,
                        Constants.s1,
                        Constants.s2,
                        Constants.s3,
                        Constants.s4,
                        Constants.s5,
                        Constants.s6,
                        Constants.s7,
                        Constants.s8,
                        Constants.s9,
                        Constants.s10,
                        Constants.sw0,
                        Constants.sw1,
                        Constants.sw2,
                        Constants.sw3,
                        Constants.sw4,
                        Constants.sw5,
                        Constants.sw6,
                        Constants.sw7,
                        Constants.sw8,
                        Constants.sw9,
                        Constants.sw10,

                        Constants.ss0,
                        Constants.ss1,
                        Constants.ss2,
                        Constants.ss3,
                        Constants.ss4,
                        Constants.ss5,

                        Constants.su0,
                        Constants.su1,
                        Constants.su2
                });
        }

        public ValueTask<IEnumerable<string>>
        opWStringLiteralsAsync(Current current)
        {
            return new ValueTask<IEnumerable<string>>(new string[]
                {
                        Constants.s0,
                        Constants.s1,
                        Constants.s2,
                        Constants.s3,
                        Constants.s4,
                        Constants.s5,
                        Constants.s6,
                        Constants.s7,
                        Constants.s8,
                        Constants.s9,
                        Constants.s10,

                        Constants.sw0,
                        Constants.sw1,
                        Constants.sw2,
                        Constants.sw3,
                        Constants.sw4,
                        Constants.sw5,
                        Constants.sw6,
                        Constants.sw7,
                        Constants.sw8,
                        Constants.sw9,
                        Constants.sw10,

                        Constants.ss0,
                        Constants.ss1,
                        Constants.ss2,
                        Constants.ss3,
                        Constants.ss4,
                        Constants.ss5,

                        Constants.su0,
                        Constants.su1,
                        Constants.su2
                });
        }

        public async ValueTask<IMyClass.OpMStruct1MarshaledReturnValue>
        opMStruct1Async(Current current)
        {
            await Task.Delay(0);
            return new IMyClass.OpMStruct1MarshaledReturnValue(
                new Structure(null, MyEnum.enum1, new AnotherStruct("")), current);
        }

        public async ValueTask<IMyClass.OpMStruct2MarshaledReturnValue>
        opMStruct2Async(Structure p1, Current current)
        {
            await Task.Delay(0);
            return new IMyClass.OpMStruct2MarshaledReturnValue(p1, p1, current);
        }

        public async ValueTask<IMyClass.OpMSeq1MarshaledReturnValue>
        opMSeq1Async(Current current)
        {
            await Task.Delay(0);
            return new IMyClass.OpMSeq1MarshaledReturnValue(Array.Empty<string>(), current);
        }

        public async ValueTask<IMyClass.OpMSeq2MarshaledReturnValue>
        opMSeq2Async(string[] p1, Current current)
        {
            await Task.Delay(0);
            return new IMyClass.OpMSeq2MarshaledReturnValue(p1, p1, current);
        }

        public async ValueTask<IMyClass.OpMDict1MarshaledReturnValue>
        opMDict1Async(Current current)
        {
            await Task.Delay(0);
            return new IMyClass.OpMDict1MarshaledReturnValue(new Dictionary<string, string>(), current);
        }

        public async ValueTask<IMyClass.OpMDict2MarshaledReturnValue>
        opMDict2Async(Dictionary<string, string> p1, Current current)
        {
            await Task.Delay(0);
            return new IMyClass.OpMDict2MarshaledReturnValue(p1, p1, current);
        }

        private static ValueTask<(ReadOnlyMemory<T>, ReadOnlyMemory<T>)> ToReturnValue<T>(T[] input1,
            T[] input2) where T : struct =>
                new ValueTask<(ReadOnlyMemory<T>, ReadOnlyMemory<T>)>((input1, input2));

        private static ValueTask<(IEnumerable<T>, IEnumerable<T>)> ToReturnValue<T>(IEnumerable<T> input1,
            IEnumerable<T> input2) => new ValueTask<(IEnumerable<T>, IEnumerable<T>)>((input1, input2));

        private static ValueTask<(IReadOnlyDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>)>
        ToReturnValue<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> input1,
            IReadOnlyDictionary<TKey, TValue> input2) where TKey : notnull =>
            new ValueTask<(IReadOnlyDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>)>((input1, input2));

        private Thread_opVoid? _opVoidThread;
        private int _opByteSOnewayCallCount = 0;
    }
}

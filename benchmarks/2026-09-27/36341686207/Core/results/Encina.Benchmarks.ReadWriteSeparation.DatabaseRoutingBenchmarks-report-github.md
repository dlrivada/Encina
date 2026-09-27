```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  4,023.8 ns |   1,743.2 ns |     95.55 ns | 4,027.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    216.7 ns |     557.4 ns |     30.55 ns |   210.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 11,944.8 ns | 263,843.2 ns | 14,462.14 ns | 3,806.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    170.7 ns |     182.7 ns |     10.02 ns |   170.0 ns |         - |
| &#39;Read HasIntent&#39;                       |    220.5 ns |     316.0 ns |     17.32 ns |   210.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    286.3 ns |   2,122.9 ns |    116.36 ns |   270.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    214.3 ns |     586.5 ns |     32.15 ns |   201.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  3,659.3 ns |   4,477.5 ns |    245.42 ns | 3,796.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  4,080.8 ns |  13,508.7 ns |    740.46 ns | 3,746.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,240.3 ns |   2,788.4 ns |    152.84 ns | 2,213.0 ns |      96 B |

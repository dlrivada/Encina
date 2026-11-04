```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,902.5 ns |  7,585.5 ns |   415.79 ns | 5,079.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   973.5 ns | 24,900.1 ns | 1,364.86 ns |   185.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,449.3 ns | 11,228.4 ns |   615.47 ns | 4,272.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   429.2 ns |  3,610.2 ns |   197.89 ns |   335.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   275.5 ns |    182.4 ns |    10.00 ns |   275.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   272.2 ns |  1,967.7 ns |   107.86 ns |   225.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   257.2 ns |  6,710.2 ns |   367.81 ns |    60.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,663.8 ns | 13,578.4 ns |   744.28 ns | 4,393.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,909.7 ns |  8,680.3 ns |   475.80 ns | 3,702.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,408.8 ns |  2,061.6 ns |   113.01 ns | 2,468.50 ns |      96 B |

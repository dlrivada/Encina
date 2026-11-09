```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 1,930.33 ns | 21,192.30 ns | 1,161.62 ns | 1,583.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    65.00 ns |  1,424.88 ns |    78.10 ns |    25.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,767.67 ns |  9,494.11 ns |   520.40 ns | 2,584.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   137.00 ns |  2,424.91 ns |   132.92 ns |    71.00 ns |         - |
| &#39;Read HasIntent&#39;                       |    81.17 ns |  1,695.13 ns |    92.92 ns |    54.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   103.33 ns |  1,530.01 ns |    83.86 ns |    60.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    88.67 ns |  1,891.58 ns |   103.68 ns |    55.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,590.67 ns |  3,749.22 ns |   205.51 ns | 2,694.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,594.00 ns | 16,644.43 ns |   912.34 ns | 1,187.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,831.83 ns |  5,337.98 ns |   292.59 ns | 1,791.50 ns |      96 B |

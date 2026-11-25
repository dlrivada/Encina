```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,347.3 ns | 20,328.3 ns | 1,114.26 ns | 4,881.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   221.3 ns |  2,341.1 ns |   128.32 ns |   172.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,083.0 ns | 21,776.3 ns | 1,193.63 ns | 2,883.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   166.3 ns |  1,824.4 ns |   100.00 ns |   192.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   153.7 ns |  2,348.7 ns |   128.74 ns |    88.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   137.3 ns |  1,693.3 ns |    92.81 ns |   100.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   154.3 ns |  2,710.0 ns |   148.54 ns |    97.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,008.7 ns | 26,929.2 ns | 1,476.08 ns | 2,472.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,375.0 ns | 20,699.2 ns | 1,134.59 ns | 3,205.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,973.3 ns | 12,927.2 ns |   708.58 ns | 1,641.00 ns |      96 B |

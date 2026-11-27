```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,394.83 ns | 1,501.13 ns |  82.28 ns | 3,414.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   110.67 ns |   191.63 ns |  10.50 ns |   111.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,693.33 ns | 2,593.58 ns | 142.16 ns | 2,743.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    72.17 ns | 1,172.91 ns |  64.29 ns |    45.50 ns |         - |
| &#39;Read HasIntent&#39;                       |    87.00 ns |   766.89 ns |  42.04 ns |   100.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   116.67 ns |   759.55 ns |  41.63 ns |   130.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   137.33 ns |   369.56 ns |  20.26 ns |   131.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,846.67 ns |   382.83 ns |  20.98 ns | 2,854.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,950.83 ns | 4,244.18 ns | 232.64 ns | 2,863.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,748.83 ns | 3,909.45 ns | 214.29 ns | 1,702.50 ns |      96 B |

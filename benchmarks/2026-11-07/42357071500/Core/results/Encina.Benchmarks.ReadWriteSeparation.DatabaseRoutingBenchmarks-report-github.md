```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |-----------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,465.3 ns |   8,916.0 ns |    488.71 ns | 4,278.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   217.7 ns |   1,519.1 ns |     83.27 ns |   191.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,609.3 ns |   1,114.7 ns |     61.10 ns | 3,596.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   180.0 ns |     182.4 ns |     10.00 ns |   180.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 9,724.7 ns | 292,764.1 ns | 16,047.39 ns |   671.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   214.0 ns |     872.5 ns |     47.82 ns |   231.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   233.7 ns |     288.7 ns |     15.82 ns |   230.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,707.0 ns |   2,936.0 ns |    160.93 ns | 3,657.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,125.0 ns |   1,845.0 ns |    101.13 ns | 4,138.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,413.7 ns |   6,668.1 ns |    365.50 ns | 2,294.0 ns |      96 B |

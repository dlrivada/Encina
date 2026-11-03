```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,049.7 ns |  4,666.3 ns |   255.77 ns | 2,990.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   112.3 ns |  1,795.9 ns |    98.44 ns |    56.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,926.0 ns | 20,195.1 ns | 1,106.96 ns | 1,963.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   541.0 ns | 11,708.7 ns |   641.80 ns |   281.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   110.0 ns |  1,621.5 ns |    88.88 ns |    80.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   190.7 ns |  2,376.6 ns |   130.27 ns |   120.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   101.5 ns |  1,943.4 ns |   106.52 ns |    84.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,696.0 ns | 19,880.7 ns | 1,089.73 ns | 1,272.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,592.3 ns | 19,379.1 ns | 1,062.23 ns | 1,382.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   971.3 ns | 10,976.5 ns |   601.66 ns |   691.00 ns |      96 B |

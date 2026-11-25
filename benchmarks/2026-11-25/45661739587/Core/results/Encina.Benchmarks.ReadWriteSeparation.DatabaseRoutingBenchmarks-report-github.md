```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error         | StdDev      | Median        | Allocated |
|--------------------------------------- |------------:|--------------:|------------:|--------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,894.50 ns |  40,382.38 ns | 2,213.49 ns | 3,676.5000 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   139.33 ns |   2,997.34 ns |   164.29 ns |    97.5000 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,135.67 ns |  30,506.92 ns | 1,672.19 ns | 1,350.0000 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   173.33 ns |   3,694.45 ns |   202.51 ns |    85.0000 ns |         - |
| &#39;Read HasIntent&#39;                       |   225.67 ns |   1,608.90 ns |    88.19 ns |   209.0000 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    68.33 ns |   2,159.27 ns |   118.36 ns |     0.0000 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    94.00 ns |   2,970.31 ns |   162.81 ns |     0.0000 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 8,367.50 ns | 131,621.77 ns | 7,214.63 ns | 6,813.5000 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,817.00 ns |  40,735.63 ns | 2,232.86 ns | 2,859.0000 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,119.17 ns |  10,092.76 ns |   553.22 ns |   919.5000 ns |      96 B |

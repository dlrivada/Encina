```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,050.0 ns |  6,348.2 ns |   347.97 ns | 5,130.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   264.3 ns |  1,417.1 ns |    77.67 ns |   241.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,716.7 ns |  2,764.4 ns |   151.53 ns | 3,737.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   393.3 ns |  3,622.8 ns |   198.58 ns |   310.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   212.7 ns |    100.5 ns |     5.51 ns |   210.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   223.3 ns |    278.7 ns |    15.28 ns |   220.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   | 1,232.7 ns | 31,493.9 ns | 1,726.28 ns |   241.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,884.3 ns |  9,955.1 ns |   545.68 ns | 3,697.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,889.2 ns |  6,093.9 ns |   334.03 ns | 3,782.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,411.7 ns |  1,393.4 ns |    76.38 ns | 2,395.0 ns |      96 B |

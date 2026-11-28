```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,244.0 ns |  8,339.5 ns |   457.12 ns | 4,037.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   198.8 ns |  1,281.4 ns |    70.24 ns |   205.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,670.3 ns |  3,802.1 ns |   208.41 ns | 3,627.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,038.7 ns | 26,801.9 ns | 1,469.11 ns |   201.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   244.3 ns |  1,172.9 ns |    64.29 ns |   271.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   253.7 ns |    532.0 ns |    29.16 ns |   270.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   237.3 ns |    732.1 ns |    40.13 ns |   260.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,718.7 ns |  1,004.8 ns |    55.08 ns | 3,722.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,950.5 ns |  9,522.2 ns |   521.95 ns | 3,716.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,225.0 ns |  2,219.4 ns |   121.66 ns | 2,285.0 ns |      96 B |

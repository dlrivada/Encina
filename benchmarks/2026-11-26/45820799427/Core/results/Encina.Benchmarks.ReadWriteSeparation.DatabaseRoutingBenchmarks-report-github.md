```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,912.2 ns | 24,743.8 ns | 1,356.29 ns | 5,481.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   106.3 ns |  1,681.6 ns |    92.17 ns |   155.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,427.2 ns | 25,303.6 ns | 1,386.97 ns | 2,779.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   207.2 ns |  3,580.7 ns |   196.27 ns |   157.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   126.7 ns |  2,921.3 ns |   160.13 ns |    47.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   232.3 ns |  2,338.9 ns |   128.20 ns |   205.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   723.7 ns | 20,025.7 ns | 1,097.67 ns |   107.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,537.2 ns | 31,999.3 ns | 1,753.99 ns | 2,525.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,173.7 ns | 24,193.3 ns | 1,326.12 ns | 2,716.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,788.7 ns | 12,889.8 ns |   706.53 ns | 1,473.00 ns |      96 B |

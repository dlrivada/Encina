```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,268.3 ns | 11,124.2 ns | 609.75 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   213.8 ns |  1,069.0 ns |  58.59 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,853.0 ns |  3,959.4 ns | 217.03 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   244.3 ns |    278.7 ns |  15.28 ns |         - |
| &#39;Read HasIntent&#39;                       |   181.0 ns |    912.2 ns |  50.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   197.3 ns |  1,010.9 ns |  55.41 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   232.2 ns |  1,172.9 ns |  64.29 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,776.3 ns |  3,333.7 ns | 182.73 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,579.0 ns | 16,304.2 ns | 893.69 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,339.5 ns |  2,006.8 ns | 110.00 ns |      96 B |

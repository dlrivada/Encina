```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,404.3 ns | 14,795.4 ns | 810.99 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   226.7 ns |    278.7 ns |  15.28 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,810.2 ns |  2,601.3 ns | 142.58 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   237.7 ns |    862.2 ns |  47.26 ns |         - |
| &#39;Read HasIntent&#39;                       |   214.3 ns |    379.8 ns |  20.82 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   191.0 ns |    182.4 ns |  10.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   223.7 ns |  1,048.1 ns |  57.45 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,125.8 ns | 17,200.4 ns | 942.81 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,821.0 ns |  3,701.6 ns | 202.90 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,474.0 ns | 11,252.6 ns | 616.79 ns |      96 B |

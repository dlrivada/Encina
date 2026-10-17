```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,212.3 ns |  1,393.4 ns |  76.38 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   244.3 ns |    278.7 ns |  15.28 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,253.2 ns |  1,970.9 ns | 108.03 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   205.5 ns |    948.0 ns |  51.96 ns |         - |
| &#39;Read HasIntent&#39;                       |   234.3 ns |    737.3 ns |  40.41 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   240.5 ns |    795.2 ns |  43.59 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   222.5 ns |  1,195.9 ns |  65.55 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,018.3 ns |  7,224.6 ns | 396.01 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,835.7 ns |    910.4 ns |  49.90 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,870.7 ns | 15,490.3 ns | 849.08 ns |      96 B |

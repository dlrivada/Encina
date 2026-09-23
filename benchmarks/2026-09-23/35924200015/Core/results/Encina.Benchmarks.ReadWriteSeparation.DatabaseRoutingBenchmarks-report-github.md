```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,167.5 ns | 1,493.3 ns |  81.85 ns | 4,187.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   191.7 ns | 1,790.6 ns |  98.15 ns |   135.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,004.7 ns |   379.8 ns |  20.82 ns | 3,998.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   260.3 ns | 1,287.5 ns |  70.57 ns |   230.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   261.0 ns |   836.0 ns |  45.83 ns |   251.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   283.7 ns | 2,010.2 ns | 110.18 ns |   291.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   260.0 ns | 1,277.1 ns |  70.00 ns |   230.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,891.0 ns | 1,385.4 ns |  75.94 ns | 3,908.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,860.0 ns |   823.4 ns |  45.13 ns | 3,856.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,248.0 ns | 1,882.5 ns | 103.18 ns | 2,275.0 ns |      96 B |

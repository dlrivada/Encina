```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,973.2 ns | 2,206.9 ns | 120.97 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   214.3 ns |   759.5 ns |  41.63 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,593.0 ns | 1,534.8 ns |  84.12 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   211.0 ns |   657.8 ns |  36.06 ns |         - |
| &#39;Read HasIntent&#39;                       |   227.3 ns |   458.0 ns |  25.11 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   210.7 ns |   321.4 ns |  17.62 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   226.7 ns |   842.6 ns |  46.19 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,787.3 ns | 2,222.5 ns | 121.82 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,506.0 ns | 2,056.0 ns | 112.69 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,293.7 ns | 2,756.0 ns | 151.06 ns |      96 B |

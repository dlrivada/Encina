```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,795.5 ns | 19,353.2 ns | 1,060.82 ns | 4,361.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   170.2 ns |  1,749.9 ns |    95.92 ns |   180.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,463.7 ns |  5,756.2 ns |   315.52 ns | 3,460.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   382.7 ns |  3,695.6 ns |   202.57 ns |   446.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   220.3 ns |  1,195.0 ns |    65.50 ns |   211.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   214.0 ns |  1,705.3 ns |    93.47 ns |   241.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   158.8 ns |  1,695.1 ns |    92.92 ns |   115.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,611.8 ns |  6,866.8 ns |   376.39 ns | 3,574.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,702.5 ns | 10,043.3 ns |   550.51 ns | 3,495.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,276.8 ns |  1,836.5 ns |   100.66 ns | 2,263.5 ns |      96 B |

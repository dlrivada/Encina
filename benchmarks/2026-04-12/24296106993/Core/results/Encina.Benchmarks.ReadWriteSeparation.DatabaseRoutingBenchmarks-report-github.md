```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,219.3 ns | 356.92 ns | 500.35 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   184.7 ns |  14.98 ns |  21.96 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,420.1 ns |  82.72 ns | 113.23 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   187.5 ns |  16.81 ns |  24.11 ns |         - |
| &#39;Read HasIntent&#39;                       |   205.0 ns |  19.26 ns |  27.62 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   235.7 ns |  31.97 ns |  45.85 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   232.9 ns |  18.17 ns |  26.63 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,378.7 ns |  55.55 ns |  77.87 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,759.6 ns | 211.04 ns | 309.34 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,253.8 ns |  55.75 ns |  81.72 ns |      96 B |

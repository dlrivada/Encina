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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,686.1 ns | 215.85 ns | 316.39 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   165.1 ns |  11.62 ns |  16.67 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,937.7 ns | 139.71 ns | 209.11 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   167.3 ns |  20.84 ns |  28.53 ns |         - |
| &#39;Read HasIntent&#39;                       |   221.1 ns |  18.49 ns |  26.52 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   233.1 ns |  19.70 ns |  28.88 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   258.1 ns |  23.84 ns |  31.82 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,523.0 ns |  65.81 ns |  96.47 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,147.1 ns | 124.38 ns | 182.32 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,291.2 ns |  38.71 ns |  57.94 ns |      96 B |

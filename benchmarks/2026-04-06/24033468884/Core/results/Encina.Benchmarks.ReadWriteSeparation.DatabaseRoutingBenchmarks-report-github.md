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
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   213.7 ns |  31.34 ns |  45.93 ns |         - |
| &#39;Read HasIntent&#39;                       |   195.9 ns |  16.66 ns |  24.42 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   215.7 ns |  19.42 ns |  28.46 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   197.2 ns |  20.62 ns |  30.23 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,756.3 ns | 253.06 ns | 370.93 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,712.8 ns | 244.58 ns | 358.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,243.2 ns |  75.94 ns | 111.32 ns |      96 B |
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,011.2 ns |  65.67 ns |  96.25 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   186.4 ns |   7.46 ns |  10.46 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,645.0 ns |  59.00 ns |  82.71 ns |     392 B |

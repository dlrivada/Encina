```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,021.1 ns | 102.91 ns | 144.26 ns | 4,043.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   163.1 ns |  16.34 ns |  23.43 ns |   165.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,043.1 ns | 316.81 ns | 474.19 ns | 3,998.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   202.1 ns |  18.76 ns |  28.08 ns |   200.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   239.6 ns |  25.17 ns |  34.46 ns |   236.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   182.3 ns |  26.82 ns |  36.71 ns |   185.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   233.1 ns |  29.95 ns |  42.95 ns |   230.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,025.1 ns | 311.15 ns | 446.24 ns | 4,043.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,674.2 ns |  57.62 ns |  86.24 ns | 3,662.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,609.2 ns | 251.44 ns | 368.56 ns | 2,404.0 ns |      96 B |

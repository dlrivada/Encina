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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,260.3 ns | 2,490.34 ns | 136.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   204.3 ns |   640.70 ns |  35.12 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,614.0 ns | 2,148.35 ns | 117.76 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   307.0 ns | 1,047.55 ns |  57.42 ns |         - |
| &#39;Read HasIntent&#39;                       |   260.0 ns |   482.68 ns |  26.46 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   179.7 ns |    10.53 ns |   0.58 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   200.7 ns |   173.40 ns |   9.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,367.0 ns |   947.97 ns |  51.96 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,552.7 ns | 1,572.92 ns |  86.22 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,186.7 ns | 3,881.69 ns | 212.77 ns |      96 B |

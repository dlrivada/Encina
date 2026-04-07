```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,976.8 ns | 206.05 ns | 302.02 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   151.0 ns |  34.78 ns |  50.97 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,305.5 ns | 148.81 ns | 213.42 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   210.9 ns |  23.50 ns |  32.94 ns |         - |
| &#39;Read HasIntent&#39;                       |   194.0 ns |  30.42 ns |  44.59 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   154.1 ns |  16.11 ns |  23.11 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   134.2 ns |  29.75 ns |  42.67 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,263.6 ns | 150.00 ns | 210.28 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,197.5 ns | 113.48 ns | 162.74 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,978.0 ns |  97.40 ns | 145.78 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean        | Error     | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|----------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,088.46 ns |  57.34 ns |  74.55 ns | 2,093.25 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    61.63 ns |  17.36 ns |  24.33 ns |    57.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,943.74 ns | 604.30 ns | 885.77 ns | 2,541.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    54.69 ns |  10.61 ns |  14.87 ns |    54.50 ns |         - |
| &#39;Read HasIntent&#39;                       |    59.64 ns |  17.38 ns |  23.20 ns |    61.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    66.89 ns |  30.67 ns |  43.99 ns |    87.75 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   113.79 ns |  37.43 ns |  53.68 ns |   108.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,089.41 ns | 374.03 ns | 548.25 ns | 3,177.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,743.74 ns |  71.55 ns |  95.52 ns | 1,740.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,656.83 ns | 555.63 ns | 778.92 ns | 1,083.50 ns |      96 B |

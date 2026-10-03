```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,009.5 ns | 31,719.6 ns | 1,738.66 ns | 3,427.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   203.8 ns |  2,302.4 ns |   126.20 ns |   134.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,486.0 ns | 24,325.8 ns | 1,333.38 ns | 3,124.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   159.2 ns |  3,117.8 ns |   170.90 ns |    61.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   139.0 ns |    818.1 ns |    44.84 ns |   149.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   906.8 ns | 12,579.6 ns |   689.53 ns |   723.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   178.3 ns |  1,406.7 ns |    77.11 ns |   207.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,454.0 ns |  2,940.1 ns |   161.16 ns | 4,496.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,534.7 ns | 25,150.8 ns | 1,378.60 ns | 3,037.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,831.5 ns | 19,051.0 ns | 1,044.25 ns | 1,286.50 ns |      96 B |

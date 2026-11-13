```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,612.0 ns | 22,161.8 ns | 1,214.76 ns | 3,050.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   150.2 ns |    755.4 ns |    41.40 ns |   143.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,770.2 ns | 21,311.8 ns | 1,168.17 ns | 2,314.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   172.8 ns |  1,812.0 ns |    99.32 ns |   117.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   160.7 ns |  1,853.0 ns |   101.57 ns |   165.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   147.3 ns |  2,455.1 ns |   134.57 ns |    83.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   200.5 ns |  2,448.5 ns |   134.21 ns |   138.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,372.8 ns | 21,745.7 ns | 1,191.96 ns | 2,845.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,862.8 ns | 24,691.8 ns | 1,353.44 ns | 2,204.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,821.2 ns | 13,830.9 ns |   758.12 ns | 1,520.50 ns |      96 B |

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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,662.7 ns | 16,895.3 ns | 926.09 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   197.7 ns |    737.3 ns |  40.41 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,699.3 ns |    278.7 ns |  15.28 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   283.0 ns |    766.9 ns |  42.04 ns |         - |
| &#39;Read HasIntent&#39;                       |   197.7 ns |    586.5 ns |  32.15 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   374.7 ns |  2,020.1 ns | 110.73 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   207.3 ns |    100.5 ns |   5.51 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,784.3 ns |  2,122.9 ns | 116.36 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,884.0 ns |  7,585.5 ns | 415.79 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,397.7 ns |  2,486.8 ns | 136.31 ns |      96 B |

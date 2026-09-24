```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,811.0 ns | 25,968.6 ns | 1,423.43 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   166.5 ns |  1,516.2 ns |    83.11 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,492.0 ns | 11,091.3 ns |   607.95 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   237.7 ns |  1,381.4 ns |    75.72 ns |         - |
| &#39;Read HasIntent&#39;                       |   190.7 ns |  1,378.6 ns |    75.57 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   172.2 ns |  1,827.4 ns |   100.17 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   166.7 ns |  1,933.6 ns |   105.99 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,640.5 ns |  7,043.8 ns |   386.10 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,012.0 ns |  2,305.3 ns |   126.36 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,162.0 ns |  3,222.3 ns |   176.63 ns |      96 B |

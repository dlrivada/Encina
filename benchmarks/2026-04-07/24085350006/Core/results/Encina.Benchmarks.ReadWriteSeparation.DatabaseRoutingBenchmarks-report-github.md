```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,588.5 ns |  3,397.0 ns |   186.20 ns | 4,578.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   195.2 ns |    486.2 ns |    26.65 ns |   184.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,601.5 ns |  1,759.4 ns |    96.44 ns | 3,561.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   172.7 ns |    893.8 ns |    48.99 ns |   149.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   245.0 ns |  2,714.6 ns |   148.80 ns |   174.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   191.0 ns |    182.4 ns |    10.00 ns |   191.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   230.0 ns |    364.9 ns |    20.00 ns |   230.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,132.0 ns | 22,578.3 ns | 1,237.59 ns | 3,428.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,556.5 ns |  6,385.3 ns |   350.00 ns | 3,406.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,601.2 ns |  1,695.1 ns |    92.92 ns | 2,574.5 ns |      96 B |

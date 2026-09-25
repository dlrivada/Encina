```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,415.0 ns |  5,208.8 ns |   285.51 ns | 4,418.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   227.2 ns |    278.7 ns |    15.28 ns |   230.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,688.5 ns |  4,122.7 ns |   225.98 ns | 3,671.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   291.0 ns |    729.7 ns |    40.00 ns |   291.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   170.0 ns |    182.4 ns |    10.00 ns |   170.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   347.7 ns |  1,724.3 ns |    94.52 ns |   381.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   | 1,005.7 ns | 24,952.9 ns | 1,367.75 ns |   221.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,586.7 ns |  3,830.3 ns |   209.95 ns | 3,537.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,887.3 ns |  4,398.6 ns |   241.10 ns | 3,907.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,228.0 ns |  2,747.9 ns |   150.62 ns | 2,235.0 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,210.3 ns |  1,474.6 ns |    80.83 ns | 4,197.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      | 1,036.8 ns | 26,462.5 ns | 1,450.50 ns |   255.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,502.7 ns |    379.8 ns |    20.82 ns | 3,496.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   211.0 ns |    547.3 ns |    30.00 ns |   211.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,038.0 ns | 26,796.0 ns | 1,468.78 ns |   190.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   260.3 ns |    556.5 ns |    30.50 ns |   260.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   212.2 ns |    278.7 ns |    15.28 ns |   215.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,092.0 ns |  7,608.1 ns |   417.03 ns | 3,982.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,685.5 ns | 30,876.0 ns | 1,692.42 ns | 3,857.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 4,072.8 ns | 37,396.3 ns | 2,049.82 ns | 3,572.5 ns |      96 B |

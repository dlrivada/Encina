```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,232.0 ns |  1,793.7 ns |    98.32 ns | 4,279.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   995.2 ns | 26,061.2 ns | 1,428.50 ns |   190.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,630.2 ns |  3,912.0 ns |   214.43 ns | 3,516.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   223.0 ns |  1,042.8 ns |    57.16 ns |   190.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   210.2 ns |  1,501.1 ns |    82.28 ns |   190.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   343.7 ns |  4,698.6 ns |   257.55 ns |   200.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   209.3 ns |    472.3 ns |    25.89 ns |   219.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,596.3 ns |    479.3 ns |    26.27 ns | 3,587.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,781.7 ns |  3,042.9 ns |   166.79 ns | 3,712.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,291.7 ns |  2,966.1 ns |   162.58 ns | 2,325.0 ns |      96 B |

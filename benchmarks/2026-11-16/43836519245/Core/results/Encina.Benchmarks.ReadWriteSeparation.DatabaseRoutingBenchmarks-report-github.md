```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,485.7 ns | 10,815.8 ns | 592.85 ns | 4,159.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   238.0 ns |  2,592.7 ns | 142.12 ns |   161.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,737.7 ns |  4,740.2 ns | 259.83 ns | 3,658.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   245.0 ns |    482.7 ns |  26.46 ns |   235.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   400.3 ns |  3,491.2 ns | 191.36 ns |   300.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   307.2 ns |  1,369.3 ns |  75.06 ns |   310.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   259.7 ns |    667.9 ns |  36.61 ns |   270.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,095.0 ns | 10,116.1 ns | 554.50 ns | 3,931.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,127.5 ns |  8,190.2 ns | 448.93 ns | 4,307.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,272.5 ns |    823.4 ns |  45.13 ns | 2,268.5 ns |      96 B |

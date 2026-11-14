```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  4,030.8 ns |   1,214.7 ns |     66.58 ns | 4,047.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    188.5 ns |   1,005.4 ns |     55.11 ns |   184.5 ns |         - |
| DatabaseRoutingScope.ForRead()         |  3,910.5 ns |   9,187.1 ns |    503.58 ns | 3,676.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    259.2 ns |     697.2 ns |     38.21 ns |   275.5 ns |         - |
| &#39;Read HasIntent&#39;                       |    253.7 ns |     458.0 ns |     25.11 ns |   251.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    228.5 ns |     390.0 ns |     21.38 ns |   235.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    263.3 ns |     278.7 ns |     15.28 ns |   260.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  3,594.8 ns |   4,398.0 ns |    241.07 ns | 3,601.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 11,664.8 ns | 252,131.6 ns | 13,820.18 ns | 3,876.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,173.8 ns |   1,981.0 ns |    108.58 ns | 2,203.5 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,516.8 ns | 10,053.3 ns | 551.06 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   227.3 ns |    650.2 ns |  35.64 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,488.5 ns |  3,541.4 ns | 194.12 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   536.2 ns |  2,710.3 ns | 148.56 ns |         - |
| &#39;Read HasIntent&#39;                       |   218.8 ns |    278.7 ns |  15.28 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   382.2 ns |  1,916.3 ns | 105.04 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   239.3 ns |  1,655.4 ns |  90.74 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,267.8 ns | 10,025.1 ns | 549.51 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,840.5 ns |  9,208.8 ns | 504.77 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,382.5 ns |  2,959.0 ns | 162.19 ns |      96 B |

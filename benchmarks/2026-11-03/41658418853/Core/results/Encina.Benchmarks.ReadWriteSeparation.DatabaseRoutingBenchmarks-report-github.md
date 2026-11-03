```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,325.0 ns |  8,259.4 ns | 452.73 ns | 3,165.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   749.2 ns | 16,268.0 ns | 891.70 ns |   425.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,146.2 ns |  2,639.6 ns | 144.68 ns | 3,219.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   102.3 ns |    842.6 ns |  46.19 ns |   129.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   113.3 ns |    421.3 ns |  23.09 ns |   100.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   131.2 ns |    210.7 ns |  11.55 ns |   124.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   140.0 ns |    316.0 ns |  17.32 ns |   150.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,796.0 ns |  7,949.7 ns | 435.75 ns | 2,779.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,895.8 ns |  3,940.0 ns | 215.96 ns | 2,879.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,299.8 ns | 17,972.6 ns | 985.14 ns | 1,802.5 ns |      96 B |

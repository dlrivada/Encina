```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,352.0 ns |  3,070.5 ns | 168.31 ns | 4,439.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   209.2 ns |  1,856.1 ns | 101.74 ns |   155.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,013.8 ns | 13,523.0 ns | 741.24 ns | 3,626.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   213.5 ns |  2,166.1 ns | 118.73 ns |   149.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   235.0 ns |  1,869.5 ns | 102.47 ns |   215.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   237.0 ns |  1,072.7 ns |  58.80 ns |   261.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   287.7 ns |  1,771.9 ns |  97.13 ns |   311.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,613.2 ns |  4,766.3 ns | 261.26 ns | 3,626.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,887.2 ns |  3,806.8 ns | 208.66 ns | 3,997.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,440.3 ns |  2,011.8 ns | 110.27 ns | 2,504.0 ns |      96 B |

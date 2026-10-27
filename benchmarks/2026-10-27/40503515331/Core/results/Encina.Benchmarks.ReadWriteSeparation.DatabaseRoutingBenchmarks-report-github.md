```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,393.7 ns |  1,572.9 ns |    86.22 ns | 4,377.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   141.5 ns |  1,785.4 ns |    97.86 ns |    85.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 5,011.3 ns | 35,398.7 ns | 1,940.32 ns | 4,087.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   236.3 ns |  2,587.2 ns |   141.81 ns |   159.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   193.7 ns |    893.8 ns |    48.99 ns |   170.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   330.7 ns |  4,429.1 ns |   242.78 ns |   191.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   167.0 ns |  1,645.7 ns |    90.21 ns |   120.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,669.0 ns |  1,945.9 ns |   106.66 ns | 3,725.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,592.3 ns |  2,285.2 ns |   125.26 ns | 3,535.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,082.7 ns |  3,954.3 ns |   216.75 ns | 2,023.00 ns |      96 B |

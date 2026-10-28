```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,487.7 ns | 8,046.5 ns | 441.06 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   120.0 ns |   316.0 ns |  17.32 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,737.7 ns | 1,665.1 ns |  91.27 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   110.7 ns |   310.9 ns |  17.04 ns |         - |
| &#39;Read HasIntent&#39;                       |   133.7 ns |   200.1 ns |  10.97 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   135.0 ns |   795.2 ns |  43.59 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   149.5 ns |   482.7 ns |  26.46 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,744.3 ns | 2,476.7 ns | 135.76 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,891.0 ns | 8,927.6 ns | 489.35 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,668.7 ns | 2,886.5 ns | 158.22 ns |      96 B |

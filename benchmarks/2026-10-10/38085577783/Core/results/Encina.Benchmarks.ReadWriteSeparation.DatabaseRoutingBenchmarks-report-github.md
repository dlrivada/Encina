```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,545.0 ns | 8,129.1 ns | 445.59 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   231.0 ns |   657.8 ns |  36.06 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,626.0 ns | 3,036.4 ns | 166.43 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   197.3 ns |   650.2 ns |  35.64 ns |         - |
| &#39;Read HasIntent&#39;                       |   243.5 ns | 1,072.7 ns |  58.80 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   206.5 ns |   927.9 ns |  50.86 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   180.7 ns |   472.3 ns |  25.89 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,754.3 ns | 3,476.9 ns | 190.58 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,680.3 ns | 2,281.1 ns | 125.03 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,238.0 ns | 1,302.5 ns |  71.39 ns |      96 B |

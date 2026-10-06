```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median      | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,197.3 ns | 2,620.6 ns | 143.64 ns | 3,254.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   137.3 ns |   288.7 ns |  15.82 ns |   141.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,662.3 ns | 5,474.1 ns | 300.06 ns | 2,599.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   137.3 ns |   458.0 ns |  25.11 ns |   140.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   144.8 ns |   374.0 ns |  20.50 ns |   144.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   132.3 ns | 1,010.9 ns |  55.41 ns |   106.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   131.2 ns | 1,474.6 ns |  80.83 ns |    84.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,744.0 ns | 3,299.0 ns | 180.83 ns | 2,724.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,777.3 ns | 4,068.5 ns | 223.01 ns | 2,864.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,812.7 ns | 3,329.7 ns | 182.51 ns | 1,913.00 ns |      96 B |

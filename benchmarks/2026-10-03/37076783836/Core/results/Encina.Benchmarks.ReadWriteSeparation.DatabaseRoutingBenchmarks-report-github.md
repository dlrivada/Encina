```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,102.8 ns | 1,927.3 ns | 105.64 ns | 4,063.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   183.8 ns | 1,695.1 ns |  92.92 ns |   140.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,667.7 ns |   828.1 ns |  45.39 ns | 3,678.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   224.3 ns | 1,069.0 ns |  58.59 ns |   201.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   237.3 ns | 1,040.1 ns |  57.01 ns |   220.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   257.0 ns |   853.2 ns |  46.77 ns |   230.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   293.0 ns |   742.6 ns |  40.71 ns |   270.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,947.0 ns | 5,278.1 ns | 289.31 ns | 3,827.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,734.0 ns | 2,166.1 ns | 118.73 ns | 3,798.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,321.0 ns | 2,598.5 ns | 142.43 ns | 2,294.0 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,265.8 ns | 1,369.3 ns |  75.06 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   228.8 ns | 1,241.8 ns |  68.07 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,379.8 ns | 8,200.4 ns | 449.49 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   149.3 ns |   862.2 ns |  47.26 ns |         - |
| &#39;Read HasIntent&#39;                       |   191.0 ns | 1,169.2 ns |  64.09 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   270.0 ns | 1,139.3 ns |  62.45 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   330.3 ns |   828.1 ns |  45.39 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,773.3 ns | 7,282.0 ns | 399.15 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,772.2 ns | 7,451.9 ns | 408.46 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,430.8 ns | 2,008.5 ns | 110.09 ns |      96 B |

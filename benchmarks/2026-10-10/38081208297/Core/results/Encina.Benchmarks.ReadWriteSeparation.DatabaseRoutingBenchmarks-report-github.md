```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,499.0 ns | 14,093.5 ns | 772.51 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   218.5 ns |    742.6 ns |  40.71 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,624.0 ns |  3,386.0 ns | 185.60 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   197.3 ns |    565.4 ns |  30.99 ns |         - |
| &#39;Read HasIntent&#39;                       |   247.7 ns |    379.8 ns |  20.82 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   224.0 ns |    906.1 ns |  49.67 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   229.3 ns |  1,214.7 ns |  66.58 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,744.3 ns |  2,655.1 ns | 145.53 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,676.5 ns |  3,299.0 ns | 180.83 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,065.0 ns |    836.0 ns |  45.83 ns |      96 B |

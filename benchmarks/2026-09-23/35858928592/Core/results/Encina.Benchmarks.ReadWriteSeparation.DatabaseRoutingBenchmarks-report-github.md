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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,309.8 ns | 1,591.5 ns |  87.24 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   254.3 ns |   278.7 ns |  15.28 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,763.0 ns | 4,505.8 ns | 246.98 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   195.2 ns |   968.9 ns |  53.11 ns |         - |
| &#39;Read HasIntent&#39;                       |   244.3 ns | 1,214.7 ns |  66.58 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   262.2 ns | 1,158.6 ns |  63.51 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   236.8 ns | 1,063.8 ns |  58.31 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,775.0 ns | 1,330.3 ns |  72.92 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,913.3 ns | 3,449.5 ns | 189.08 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,568.0 ns | 5,972.9 ns | 327.40 ns |      96 B |

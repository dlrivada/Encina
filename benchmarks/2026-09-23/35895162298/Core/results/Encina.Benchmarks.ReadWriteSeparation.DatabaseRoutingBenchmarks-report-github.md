```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,074.5 ns | 4,232.7 ns | 232.01 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   169.8 ns | 1,682.0 ns |  92.20 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,730.7 ns | 7,131.4 ns | 390.90 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   208.5 ns | 1,552.0 ns |  85.07 ns |         - |
| &#39;Read HasIntent&#39;                       |   203.0 ns | 1,601.6 ns |  87.79 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   193.2 ns |   732.1 ns |  40.13 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   257.3 ns |   579.9 ns |  31.79 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,502.3 ns | 7,161.7 ns | 392.56 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,595.7 ns | 5,394.7 ns | 295.70 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,074.5 ns | 1,742.9 ns |  95.54 ns |      96 B |

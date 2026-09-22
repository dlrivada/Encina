```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,557.2 ns | 8,662.6 ns | 474.83 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   198.0 ns |   701.1 ns |  38.43 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,834.3 ns | 7,174.5 ns | 393.26 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   247.3 ns | 1,010.9 ns |  55.41 ns |         - |
| &#39;Read HasIntent&#39;                       |   251.0 ns |   836.0 ns |  45.83 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   171.8 ns | 1,217.1 ns |  66.71 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   307.7 ns | 1,836.5 ns | 100.66 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,857.7 ns | 1,981.0 ns | 108.58 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,691.8 ns | 2,697.4 ns | 147.85 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,149.7 ns | 1,004.8 ns |  55.08 ns |      96 B |

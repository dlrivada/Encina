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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,374.2 ns | 3,213.9 ns | 176.16 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   230.7 ns |   801.5 ns |  43.94 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,091.0 ns | 9,245.8 ns | 506.79 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   223.3 ns |   542.5 ns |  29.74 ns |         - |
| &#39;Read HasIntent&#39;                       |   238.8 ns |   640.7 ns |  35.12 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   217.3 ns |   276.9 ns |  15.18 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   218.3 ns | 1,214.7 ns |  66.58 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,532.3 ns | 6,884.1 ns | 377.34 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,605.2 ns | 3,275.3 ns | 179.53 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,204.3 ns |   374.0 ns |  20.50 ns |      96 B |

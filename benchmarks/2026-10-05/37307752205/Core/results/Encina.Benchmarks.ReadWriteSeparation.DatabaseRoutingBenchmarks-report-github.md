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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,005.8 ns |   586.5 ns |  32.15 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   210.0 ns |   482.7 ns |  26.46 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,012.2 ns | 4,791.9 ns | 262.66 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   154.3 ns |   899.9 ns |  49.33 ns |         - |
| &#39;Read HasIntent&#39;                       |   187.7 ns |   862.2 ns |  47.26 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   218.0 ns | 1,220.7 ns |  66.91 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   276.7 ns |   862.2 ns |  47.26 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,558.2 ns | 3,797.7 ns | 208.17 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,483.7 ns | 3,229.9 ns | 177.04 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,571.3 ns | 8,885.1 ns | 487.02 ns |      96 B |

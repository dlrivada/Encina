```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,008.0 ns |   795.2 ns |  43.59 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   203.8 ns |   640.7 ns |  35.12 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,796.8 ns | 3,806.8 ns | 208.66 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   174.0 ns |   766.9 ns |  42.04 ns |         - |
| &#39;Read HasIntent&#39;                       |   234.8 ns | 1,136.4 ns |  62.29 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   204.3 ns |   822.7 ns |  45.09 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   189.7 ns |   489.6 ns |  26.84 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,515.7 ns | 1,600.9 ns |  87.75 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,611.5 ns | 5,019.5 ns | 275.14 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,280.0 ns | 1,582.8 ns |  86.76 ns |      96 B |

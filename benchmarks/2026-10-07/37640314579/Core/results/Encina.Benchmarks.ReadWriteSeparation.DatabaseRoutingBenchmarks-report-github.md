```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  4,111.2 ns |   1,324.4 ns |     72.60 ns | 4,148.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    212.7 ns |     416.2 ns |     22.81 ns |   200.0 ns |         - |
| DatabaseRoutingScope.ForRead()         |  3,758.3 ns |   1,697.8 ns |     93.06 ns | 3,731.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    217.2 ns |   1,393.4 ns |     76.38 ns |   200.5 ns |         - |
| &#39;Read HasIntent&#39;                       |    213.0 ns |   1,424.5 ns |     78.08 ns |   190.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    228.0 ns |   1,582.8 ns |     86.76 ns |   211.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    273.8 ns |     862.2 ns |     47.26 ns |   290.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 11,605.3 ns | 254,004.8 ns | 13,922.86 ns | 3,607.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  3,633.0 ns |   2,655.5 ns |    145.56 ns | 3,587.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,060.8 ns |   4,468.8 ns |    244.95 ns | 2,114.5 ns |      96 B |

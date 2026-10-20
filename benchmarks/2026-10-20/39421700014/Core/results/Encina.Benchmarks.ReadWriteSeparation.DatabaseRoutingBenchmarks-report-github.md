```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,281.83 ns | 2,747.50 ns | 150.60 ns | 3,275.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   105.67 ns |   493.03 ns |  27.02 ns |   116.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,745.00 ns | 1,740.34 ns |  95.39 ns | 2,795.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    63.17 ns | 1,079.36 ns |  59.16 ns |    39.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   190.17 ns | 1,746.10 ns |  95.71 ns |   140.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   239.83 ns | 3,329.68 ns | 182.51 ns |   139.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   133.50 ns |   649.38 ns |  35.59 ns |   130.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,038.00 ns | 3,045.50 ns | 166.93 ns | 2,985.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,912.50 ns |   909.81 ns |  49.87 ns | 2,919.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,753.00 ns | 5,201.86 ns | 285.13 ns | 1,743.00 ns |      96 B |

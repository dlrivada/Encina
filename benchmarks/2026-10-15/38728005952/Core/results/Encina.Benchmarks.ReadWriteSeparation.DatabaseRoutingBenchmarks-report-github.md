```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,226.67 ns | 20,462.61 ns | 1,121.62 ns | 1,863.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    73.67 ns |  1,377.41 ns |    75.50 ns |    41.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,784.33 ns | 17,564.46 ns |   962.77 ns | 1,717.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   163.17 ns |  1,391.83 ns |    76.29 ns |   129.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   113.33 ns |  1,593.93 ns |    87.37 ns |    90.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   925.33 ns | 13,873.80 ns |   760.47 ns | 1,292.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   100.33 ns |  1,682.02 ns |    92.20 ns |    80.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,843.33 ns | 16,997.06 ns |   931.67 ns | 1,823.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,843.00 ns | 18,079.53 ns |   991.00 ns | 1,843.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,066.67 ns |  7,865.59 ns |   431.14 ns | 2,174.00 ns |      96 B |

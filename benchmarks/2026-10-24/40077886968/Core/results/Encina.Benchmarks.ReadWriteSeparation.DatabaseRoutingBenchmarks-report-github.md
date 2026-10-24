```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,274.67 ns | 2,710.30 ns | 148.56 ns | 3,345.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   107.33 ns |   216.12 ns |  11.85 ns |   101.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,021.67 ns | 5,495.40 ns | 301.22 ns | 3,035.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    89.83 ns |   321.39 ns |  17.62 ns |    99.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   103.67 ns |   431.85 ns |  23.67 ns |    90.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   146.67 ns |   379.77 ns |  20.82 ns |   140.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   117.33 ns | 1,009.25 ns |  55.32 ns |    90.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,937.17 ns | 6,022.92 ns | 330.14 ns | 2,873.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,636.83 ns | 4,587.61 ns | 251.46 ns | 2,723.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,703.00 ns | 1,904.70 ns | 104.40 ns | 1,653.00 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,118.33 ns | 16,988.64 ns | 931.20 ns | 1,928.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   112.83 ns |  1,695.13 ns |  92.92 ns |    69.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,827.50 ns | 14,732.76 ns | 807.55 ns | 1,407.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   189.67 ns |  2,281.58 ns | 125.06 ns |   149.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   123.33 ns |  1,214.73 ns |  66.58 ns |    90.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    83.33 ns |  1,685.28 ns |  92.38 ns |    30.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   145.00 ns |  1,109.72 ns |  60.83 ns |   115.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,682.67 ns | 16,996.87 ns | 931.66 ns | 1,663.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,732.67 ns | 13,585.16 ns | 744.65 ns | 1,602.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   906.67 ns | 10,865.21 ns | 595.56 ns |   897.00 ns |      96 B |

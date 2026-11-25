```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,151.67 ns | 21,147.48 ns | 1,159.16 ns | 1,998.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   465.67 ns | 13,150.19 ns |   720.81 ns |    85.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,029.83 ns | 17,484.90 ns |   958.41 ns | 2,353.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   133.33 ns |  2,034.26 ns |   111.50 ns |    90.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   474.00 ns | 12,924.34 ns |   708.43 ns |    70.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    93.33 ns |  1,655.39 ns |    90.74 ns |    80.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   107.00 ns |  1,153.40 ns |    63.22 ns |    71.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,744.00 ns |  3,180.90 ns |   174.36 ns | 2,664.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,609.33 ns | 17,754.82 ns |   973.20 ns | 1,282.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,086.50 ns |  5,726.23 ns |   313.87 ns | 2,033.50 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,731.00 ns | 31,502.09 ns | 1,726.74 ns | 2,083.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   150.33 ns |  3,514.95 ns |   192.67 ns |    71.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,133.33 ns | 13,774.36 ns |   755.02 ns | 2,840.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   621.00 ns | 18,043.98 ns |   989.05 ns |    60.00 ns |         - |
| &#39;Read HasIntent&#39;                       |    88.67 ns |  1,856.05 ns |   101.74 ns |    35.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    98.67 ns |  1,561.98 ns |    85.62 ns |    65.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   136.67 ns |  1,799.88 ns |    98.66 ns |    90.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,275.00 ns | 12,306.62 ns |   674.57 ns | 3,195.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,292.00 ns | 10,689.12 ns |   585.91 ns | 3,115.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   985.83 ns | 13,370.45 ns |   732.88 ns |   705.50 ns |      96 B |

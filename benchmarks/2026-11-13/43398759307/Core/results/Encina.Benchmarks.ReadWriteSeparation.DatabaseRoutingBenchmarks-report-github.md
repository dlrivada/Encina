```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,042.5 ns |  4,349.0 ns |   238.38 ns | 5,148.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   216.7 ns |    690.7 ns |    37.86 ns |   200.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,400.0 ns |  3,445.9 ns |   188.88 ns | 3,356.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   233.7 ns |    895.7 ns |    49.10 ns |   211.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   264.3 ns |    459.1 ns |    25.17 ns |   261.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   261.0 ns |    729.7 ns |    40.00 ns |   261.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   | 1,046.0 ns | 26,227.4 ns | 1,437.61 ns |   221.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,191.3 ns |  5,862.3 ns |   321.33 ns | 4,098.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,610.3 ns |  4,275.9 ns |   234.38 ns | 3,517.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,261.3 ns |  1,176.7 ns |    64.50 ns | 2,234.0 ns |      96 B |

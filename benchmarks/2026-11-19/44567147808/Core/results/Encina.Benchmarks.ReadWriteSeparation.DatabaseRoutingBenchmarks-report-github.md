```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,267.7 ns |  2,695.5 ns |   147.75 ns | 4,238.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   191.5 ns |    823.4 ns |    45.13 ns |   195.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,497.5 ns |  3,549.7 ns |   194.57 ns | 3,400.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,965.5 ns | 27,242.6 ns | 1,493.26 ns | 2,720.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   189.7 ns |    738.9 ns |    40.50 ns |   190.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   217.3 ns |    926.7 ns |    50.80 ns |   211.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   263.3 ns |    557.4 ns |    30.55 ns |   270.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,493.0 ns |  2,773.2 ns |   152.01 ns | 3,426.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 5,016.3 ns | 22,137.9 ns | 1,213.46 ns | 4,559.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,218.0 ns |  3,234.7 ns |   177.30 ns | 2,305.0 ns |      96 B |

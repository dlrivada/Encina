```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,707.3 ns |  5,143.8 ns |   281.95 ns | 4,845.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   213.3 ns |    899.9 ns |    49.33 ns |   190.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,628.5 ns |  4,906.7 ns |   268.96 ns | 3,531.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   329.2 ns |  1,472.0 ns |    80.69 ns |   335.5 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,023.2 ns | 26,959.4 ns | 1,477.74 ns |   175.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   202.2 ns |    862.2 ns |    47.26 ns |   185.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   300.5 ns |    836.0 ns |    45.83 ns |   310.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,169.2 ns | 12,105.0 ns |   663.52 ns | 4,102.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,927.2 ns |  9,353.9 ns |   512.72 ns | 3,656.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,392.8 ns |  1,655.4 ns |    90.74 ns | 2,429.5 ns |      96 B |

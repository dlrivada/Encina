```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median        | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|--------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,579.50 ns | 34,920.92 ns | 1,914.13 ns | 2,952.5000 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   177.67 ns |  4,003.92 ns |   219.47 ns |   110.0000 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,319.17 ns | 27,005.42 ns | 1,480.26 ns | 2,082.5000 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   228.17 ns |  3,562.45 ns |   195.27 ns |   149.5000 ns |         - |
| &#39;Read HasIntent&#39;                       |    95.50 ns |  3,017.71 ns |   165.41 ns |     0.0000 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   233.33 ns |  2,708.28 ns |   148.45 ns |   220.0000 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   170.17 ns |  3,394.82 ns |   186.08 ns |    92.5000 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,950.67 ns | 23,349.33 ns | 1,279.86 ns | 1,495.0000 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,860.50 ns | 71,303.23 ns | 3,908.37 ns | 3,196.5000 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 4,294.67 ns | 38,033.32 ns | 2,084.73 ns | 4,488.0000 ns |      96 B |

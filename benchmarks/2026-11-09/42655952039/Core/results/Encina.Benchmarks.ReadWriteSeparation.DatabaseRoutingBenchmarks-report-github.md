```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,248.33 ns |  1,840.39 ns |   100.88 ns | 4,233.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    92.00 ns |  2,112.18 ns |   115.78 ns |    54.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,556.50 ns | 14,367.36 ns |   787.52 ns | 4,212.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,198.17 ns | 19,948.96 ns | 1,093.47 ns | 1,340.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   230.17 ns |  1,232.81 ns |    67.57 ns |   200.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   289.50 ns |  3,476.09 ns |   190.54 ns |   181.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   258.33 ns |    283.41 ns |    15.53 ns |   263.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,991.83 ns | 21,342.93 ns | 1,169.88 ns | 2,522.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,862.00 ns | 23,426.50 ns | 1,284.09 ns | 2,356.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,632.00 ns | 16,218.44 ns |   888.99 ns | 1,194.00 ns |      96 B |

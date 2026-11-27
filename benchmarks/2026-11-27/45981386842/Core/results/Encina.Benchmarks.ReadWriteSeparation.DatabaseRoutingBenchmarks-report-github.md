```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,220.00 ns | 19,080.34 ns | 1,045.86 ns | 1,913.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    60.00 ns |  1,664.58 ns |    91.24 ns |    15.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,166.67 ns |  7,987.42 ns |   437.82 ns | 3,050.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   110.00 ns |  1,315.57 ns |    72.11 ns |   130.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   929.67 ns | 13,198.19 ns |   723.44 ns | 1,177.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   881.17 ns | 13,195.27 ns |   723.28 ns | 1,221.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   845.00 ns | 10,909.79 ns |   598.00 ns | 1,052.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,627.67 ns | 17,702.45 ns |   970.33 ns | 1,408.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,817.17 ns | 10,059.73 ns |   551.41 ns | 2,643.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,013.33 ns |  3,554.97 ns |   194.86 ns | 2,084.00 ns |      96 B |

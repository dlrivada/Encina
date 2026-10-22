```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,714.2 ns | 26,794.8 ns | 1,468.71 ns | 3,963.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   149.7 ns |  1,460.1 ns |    80.03 ns |   147.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,094.3 ns | 19,034.2 ns | 1,043.33 ns | 4,279.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   218.5 ns |    833.8 ns |    45.71 ns |   213.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   134.7 ns |  2,394.2 ns |   131.23 ns |    76.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   327.8 ns |  1,524.2 ns |    83.55 ns |   357.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   183.8 ns |  1,400.9 ns |    76.79 ns |   196.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,787.5 ns |  8,420.4 ns |   461.55 ns | 4,883.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,327.2 ns | 26,973.7 ns | 1,478.52 ns | 2,661.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,094.7 ns | 12,557.9 ns |   688.34 ns | 1,753.00 ns |      96 B |

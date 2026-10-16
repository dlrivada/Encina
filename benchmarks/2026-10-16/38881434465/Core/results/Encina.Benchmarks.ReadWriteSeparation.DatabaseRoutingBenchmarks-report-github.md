```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,467.00 ns | 19,634.85 ns | 1,076.25 ns | 3,230.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    97.00 ns |  1,651.74 ns |    90.54 ns |    61.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,680.67 ns | 18,651.05 ns | 1,022.33 ns | 1,417.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   130.67 ns |  3,165.19 ns |   173.49 ns |    31.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   131.17 ns |  1,004.79 ns |    55.08 ns |   134.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   135.33 ns |  3,024.85 ns |   165.80 ns |    55.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   442.33 ns | 11,973.52 ns |   656.31 ns |   125.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,106.67 ns | 11,154.87 ns |   611.44 ns | 3,080.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,140.00 ns | 17,863.03 ns |   979.13 ns | 2,504.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,180.00 ns |  4,360.48 ns |   239.01 ns | 2,253.00 ns |      96 B |

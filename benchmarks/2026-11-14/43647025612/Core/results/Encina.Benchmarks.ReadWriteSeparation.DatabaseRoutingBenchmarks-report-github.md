```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median        | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|--------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,135.33 ns | 72,117.89 ns | 3,953.02 ns | 3,838.0000 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   114.33 ns |  2,212.24 ns |   121.26 ns |   101.5000 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,173.50 ns | 38,710.23 ns | 2,121.84 ns | 3,056.5000 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   106.33 ns |  2,500.28 ns |   137.05 ns |    58.0000 ns |         - |
| &#39;Read HasIntent&#39;                       |    56.83 ns |  1,795.88 ns |    98.44 ns |     0.0000 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   202.33 ns |  2,312.74 ns |   126.77 ns |   142.0000 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   145.50 ns |  3,229.65 ns |   177.03 ns |    90.5000 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,505.00 ns | 50,887.56 ns | 2,789.32 ns | 3,295.0000 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 6,028.33 ns | 46,950.20 ns | 2,573.50 ns | 6,973.0000 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,130.33 ns | 27,013.05 ns | 1,480.68 ns | 2,289.0000 ns |      96 B |

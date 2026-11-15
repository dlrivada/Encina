```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean         | Error      | StdDev     | Median        | Allocated |
|--------------------------------------- |-------------:|-----------:|-----------:|--------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 1,878.926 ns | 590.522 ns | 827.830 ns | 2,352.5000 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |     6.852 ns |  11.794 ns |  16.534 ns |     0.0000 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,543.769 ns | 597.308 ns | 817.602 ns | 1,554.5000 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |     6.481 ns |  10.687 ns |  14.629 ns |     0.0000 ns |         - |
| &#39;Read HasIntent&#39;                       |    26.120 ns |  15.334 ns |  20.470 ns |    20.0000 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    23.815 ns |  19.172 ns |  26.876 ns |    20.0000 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    40.907 ns |  14.378 ns |  20.156 ns |    44.5000 ns |         - |
| DatabaseRoutingScope.ForWrite()        |   798.393 ns | 173.204 ns | 248.404 ns |   706.0000 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |   800.480 ns | 161.525 ns | 215.631 ns |   742.0000 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   394.556 ns |  73.909 ns | 103.610 ns |   361.0000 ns |      96 B |

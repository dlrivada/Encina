```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,495.67 ns | 8,536.84 ns | 467.93 ns | 3,315.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   141.00 ns |   315.99 ns |  17.32 ns |   151.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,911.33 ns | 4,018.61 ns | 220.27 ns | 2,924.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   263.50 ns | 3,125.33 ns | 171.31 ns |   180.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   124.00 ns |   205.60 ns |  11.27 ns |   130.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    66.50 ns |   467.62 ns |  25.63 ns |    69.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   110.33 ns |   972.30 ns |  53.29 ns |   130.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,720.33 ns | 3,418.46 ns | 187.38 ns | 2,643.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,721.00 ns | 1,387.84 ns |  76.07 ns | 2,754.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,766.00 ns | 4,287.74 ns | 235.03 ns | 1,762.00 ns |      96 B |

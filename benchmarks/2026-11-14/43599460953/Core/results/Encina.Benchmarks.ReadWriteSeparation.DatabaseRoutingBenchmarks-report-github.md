```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,185.0 ns | 3,227.64 ns | 176.92 ns | 3,155.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   124.8 ns | 1,458.40 ns |  79.94 ns |    94.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,807.2 ns | 4,575.15 ns | 250.78 ns | 2,793.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   213.2 ns | 3,912.01 ns | 214.43 ns |    99.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   147.3 ns |   100.48 ns |   5.51 ns |   150.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   259.3 ns | 2,909.48 ns | 159.48 ns |   216.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   160.3 ns |    10.53 ns |   0.58 ns |   160.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,784.0 ns | 2,103.97 ns | 115.33 ns | 2,794.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,873.7 ns | 4,468.05 ns | 244.91 ns | 2,984.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,589.7 ns | 4,659.60 ns | 255.41 ns | 1,573.00 ns |      96 B |

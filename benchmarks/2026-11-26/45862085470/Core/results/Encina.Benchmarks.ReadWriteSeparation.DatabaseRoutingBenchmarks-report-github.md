```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,515.67 ns | 2,710.30 ns | 148.56 ns | 3,586.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   134.00 ns |   390.01 ns |  21.38 ns |   141.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,764.33 ns | 6,199.12 ns | 339.79 ns | 2,925.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   194.00 ns | 2,328.99 ns | 127.66 ns |   130.00 ns |         - |
| &#39;Read HasIntent&#39;                       |    89.83 ns |   961.97 ns |  52.73 ns |    70.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   155.67 ns |   326.52 ns |  17.90 ns |   166.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   100.33 ns | 1,136.44 ns |  62.29 ns |    81.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,811.00 ns | 6,661.51 ns | 365.14 ns | 2,744.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,847.67 ns | 4,676.24 ns | 256.32 ns | 2,824.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,626.00 ns | 4,184.82 ns | 229.38 ns | 1,553.00 ns |      96 B |

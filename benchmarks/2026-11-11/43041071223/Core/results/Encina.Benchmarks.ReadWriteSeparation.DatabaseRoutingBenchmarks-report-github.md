```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,098.0 ns |    729.7 ns |    40.00 ns | 4,098.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   182.7 ns |    288.7 ns |    15.82 ns |   179.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,776.0 ns | 12,205.4 ns |   669.02 ns | 3,505.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   234.3 ns |  1,069.0 ns |    58.59 ns |   211.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   210.2 ns |    729.8 ns |    40.00 ns |   209.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    | 1,082.0 ns | 28,976.5 ns | 1,588.30 ns |   170.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   258.8 ns |  1,004.8 ns |    55.08 ns |   255.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,657.3 ns |  2,423.8 ns |   132.85 ns | 3,607.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,767.0 ns |    316.0 ns |    17.32 ns | 3,777.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,392.5 ns |  2,749.1 ns |   150.69 ns | 2,479.5 ns |      96 B |

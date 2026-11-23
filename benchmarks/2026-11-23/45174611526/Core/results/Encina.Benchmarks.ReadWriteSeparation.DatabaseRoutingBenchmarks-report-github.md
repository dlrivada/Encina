```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,434.7 ns | 8,639.9 ns | 473.58 ns | 5,285.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   412.0 ns | 6,184.2 ns | 338.98 ns |   325.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,991.3 ns | 5,612.6 ns | 307.64 ns | 4,028.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   318.0 ns | 1,810.4 ns |  99.23 ns |   271.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   271.8 ns | 1,044.9 ns |  57.27 ns |   255.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   460.8 ns | 3,806.8 ns | 208.66 ns |   350.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   211.8 ns | 1,027.2 ns |  56.31 ns |   195.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,404.3 ns | 5,905.7 ns | 323.71 ns | 4,488.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,610.5 ns | 7,533.1 ns | 412.91 ns | 4,473.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,788.3 ns | 4,369.9 ns | 239.53 ns | 2,895.0 ns |      96 B |

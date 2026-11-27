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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,318.0 ns |  6,061.0 ns |   332.22 ns | 4,217.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   209.8 ns |    789.0 ns |    43.25 ns |   189.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,633.2 ns |  4,807.3 ns |   263.50 ns | 3,556.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   261.8 ns |    200.1 ns |    10.97 ns |   255.5 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,098.3 ns | 27,912.6 ns | 1,529.99 ns |   220.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   159.7 ns |    556.5 ns |    30.50 ns |   160.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   210.7 ns |    637.3 ns |    34.93 ns |   191.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,564.0 ns |  3,701.6 ns |   202.90 ns | 3,457.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,396.2 ns |  3,129.8 ns |   171.55 ns | 4,312.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,314.7 ns |  1,435.4 ns |    78.68 ns | 2,355.0 ns |      96 B |

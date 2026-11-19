```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,688.7 ns | 16,820.0 ns |   921.96 ns | 3,325.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   501.3 ns | 14,888.3 ns |   816.08 ns |    61.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,887.5 ns | 18,791.2 ns | 1,030.01 ns | 1,637.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   148.5 ns |  3,429.5 ns |   187.98 ns |    44.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   540.7 ns | 14,085.3 ns |   772.06 ns |   110.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   144.8 ns |  1,270.6 ns |    69.64 ns |   114.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   | 1,020.8 ns | 15,077.3 ns |   826.44 ns | 1,431.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,774.3 ns | 19,486.1 ns | 1,068.10 ns | 1,597.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,820.0 ns | 20,443.0 ns | 1,120.55 ns | 1,423.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   908.0 ns | 10,256.4 ns |   562.19 ns |   751.00 ns |      96 B |

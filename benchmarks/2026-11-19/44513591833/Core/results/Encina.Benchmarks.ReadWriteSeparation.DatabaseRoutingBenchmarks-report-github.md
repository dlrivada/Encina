```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,275.3 ns |  8,518.1 ns |   466.91 ns | 4,068.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   221.3 ns |    869.3 ns |    47.65 ns |   205.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,861.0 ns |  6,967.5 ns |   381.91 ns | 3,968.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   192.2 ns |  1,463.3 ns |    80.21 ns |   185.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   326.7 ns |    899.9 ns |    49.33 ns |   350.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   196.7 ns |  1,393.4 ns |    76.38 ns |   180.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   224.0 ns |    938.6 ns |    51.45 ns |   210.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,724.0 ns |  6,973.0 ns |   382.22 ns | 3,697.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,078.0 ns | 10,354.7 ns |   567.57 ns | 3,818.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,962.5 ns | 57,400.6 ns | 3,146.32 ns | 2,449.5 ns |      96 B |

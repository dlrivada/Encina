```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  9,580.7 ns | 143,045.9 ns |  7,840.83 ns | 7,407.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    109.7 ns |   2,035.0 ns |    111.55 ns |   106.00 ns |         - |
| DatabaseRoutingScope.ForRead()         |  6,755.8 ns |   7,070.5 ns |    387.56 ns | 6,968.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    129.0 ns |   2,749.8 ns |    150.73 ns |    59.00 ns |         - |
| &#39;Read HasIntent&#39;                       |    100.3 ns |   1,895.9 ns |    103.92 ns |    93.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    116.3 ns |     890.5 ns |     48.81 ns |   143.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    261.7 ns |   2,790.9 ns |    152.98 ns |   230.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  2,047.8 ns |  25,968.4 ns |  1,423.42 ns | 1,506.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  3,940.8 ns |  14,059.2 ns |    770.63 ns | 3,511.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 10,106.3 ns | 237,289.2 ns | 13,006.62 ns | 2,641.00 ns |      96 B |

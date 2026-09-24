```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,485.7 ns |  7,279.3 ns | 399.00 ns | 4,369.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   213.0 ns |  1,219.3 ns |  66.84 ns |   179.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,837.7 ns |  5,710.0 ns | 312.99 ns | 3,998.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   192.3 ns |  1,492.2 ns |  81.79 ns |   156.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   307.2 ns |  2,266.4 ns | 124.23 ns |   240.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   265.0 ns |    482.7 ns |  26.46 ns |   255.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   303.7 ns |  1,108.3 ns |  60.75 ns |   310.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,739.3 ns |  2,732.5 ns | 149.78 ns | 3,696.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,473.7 ns |  2,194.1 ns | 120.27 ns | 3,437.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,614.3 ns | 12,195.2 ns | 668.46 ns | 2,244.0 ns |      96 B |

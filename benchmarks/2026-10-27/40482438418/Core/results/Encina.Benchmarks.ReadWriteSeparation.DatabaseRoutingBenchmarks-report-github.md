```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,633.5 ns |  5,795.1 ns |   317.65 ns | 4,553.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      | 1,004.3 ns | 26,541.7 ns | 1,454.84 ns |   189.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,568.7 ns | 21,071.9 ns | 1,155.02 ns | 4,058.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   247.2 ns |  3,240.7 ns |   177.64 ns |   160.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   337.5 ns |  1,187.5 ns |    65.09 ns |   341.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   211.7 ns |    737.3 ns |    40.41 ns |   205.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   284.0 ns |  1,219.3 ns |    66.84 ns |   250.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,926.8 ns |  7,977.2 ns |   437.26 ns | 3,956.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,666.3 ns |  5,744.9 ns |   314.90 ns | 3,526.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,710.5 ns |  7,758.0 ns |   425.24 ns | 2,469.5 ns |      96 B |

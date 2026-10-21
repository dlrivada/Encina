```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,119.7 ns | 5,466.1 ns | 299.62 ns | 4,257.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   175.2 ns |   826.1 ns |  45.28 ns |   165.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,732.7 ns | 9,356.6 ns | 512.87 ns | 3,606.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   375.3 ns | 6,333.0 ns | 347.13 ns |   185.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   218.5 ns | 1,851.3 ns | 101.47 ns |   165.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   477.0 ns | 4,810.8 ns | 263.70 ns |   340.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   227.0 ns | 1,810.4 ns |  99.23 ns |   180.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,587.0 ns | 7,457.6 ns | 408.77 ns | 3,430.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,772.7 ns | 7,329.7 ns | 401.76 ns | 3,666.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,070.7 ns | 3,414.7 ns | 187.17 ns | 1,994.0 ns |      96 B |

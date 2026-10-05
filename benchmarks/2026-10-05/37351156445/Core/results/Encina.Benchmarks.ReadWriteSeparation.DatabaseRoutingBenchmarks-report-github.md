```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,123.7 ns |   278.7 ns |  15.28 ns | 4,127.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   247.2 ns |   459.1 ns |  25.17 ns |   250.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,430.3 ns | 2,374.0 ns | 130.13 ns | 3,437.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   237.7 ns |   822.7 ns |  45.09 ns |   241.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   200.3 ns |   173.4 ns |   9.50 ns |   200.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   256.7 ns |   459.1 ns |  25.17 ns |   260.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   364.0 ns | 4,716.2 ns | 258.51 ns |   230.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,314.3 ns | 5,472.5 ns | 299.97 ns | 4,228.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,696.0 ns | 3,165.2 ns | 173.49 ns | 3,786.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,263.3 ns | 2,583.3 ns | 141.60 ns | 2,243.0 ns |      96 B |

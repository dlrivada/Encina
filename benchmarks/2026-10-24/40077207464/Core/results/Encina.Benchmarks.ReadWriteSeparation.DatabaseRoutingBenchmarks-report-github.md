```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,465.0 ns | 9,022.6 ns | 494.56 ns | 4,308.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   146.3 ns | 1,163.9 ns |  63.80 ns |   110.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,794.3 ns | 3,182.5 ns | 174.44 ns | 3,858.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   254.0 ns | 2,015.7 ns | 110.49 ns |   201.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   244.3 ns | 1,381.4 ns |  75.72 ns |   211.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   215.5 ns | 1,139.3 ns |  62.45 ns |   195.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   241.0 ns | 1,264.0 ns |  69.28 ns |   201.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,433.2 ns | 9,761.7 ns | 535.07 ns | 4,663.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,205.0 ns | 7,136.9 ns | 391.20 ns | 4,178.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,278.0 ns | 2,166.9 ns | 118.77 ns | 2,215.0 ns |      96 B |

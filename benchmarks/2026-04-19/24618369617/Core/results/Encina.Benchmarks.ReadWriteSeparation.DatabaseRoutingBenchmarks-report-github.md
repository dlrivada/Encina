```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,114.2 ns | 103.33 ns | 141.44 ns | 4,098.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   239.5 ns |  36.87 ns |  50.46 ns |   220.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,777.1 ns | 101.32 ns | 138.68 ns | 3,762.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   198.4 ns |  23.32 ns |  32.69 ns |   195.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   205.0 ns |  20.81 ns |  27.78 ns |   205.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   287.2 ns | 103.40 ns | 151.57 ns |   225.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   254.0 ns |  15.30 ns |  21.94 ns |   250.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,732.4 ns | 132.19 ns | 185.31 ns | 3,686.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,295.6 ns | 228.14 ns | 304.56 ns | 4,388.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,327.5 ns |  96.31 ns | 135.01 ns | 2,289.5 ns |      96 B |

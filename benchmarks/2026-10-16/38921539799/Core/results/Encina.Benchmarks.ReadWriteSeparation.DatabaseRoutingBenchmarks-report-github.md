```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,437.7 ns | 9,801.3 ns | 537.24 ns | 3,284.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   400.7 ns | 5,880.6 ns | 322.34 ns |   361.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,850.8 ns | 1,846.2 ns | 101.19 ns | 2,864.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   159.8 ns | 1,287.5 ns |  70.57 ns |   129.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   131.2 ns |   105.3 ns |   5.77 ns |   134.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   147.3 ns | 1,490.0 ns |  81.67 ns |   110.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   190.3 ns |   655.3 ns |  35.92 ns |   181.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,834.0 ns | 4,898.7 ns | 268.51 ns | 2,884.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,961.3 ns | 6,126.2 ns | 335.80 ns | 2,945.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,640.2 ns | 3,394.0 ns | 186.04 ns | 1,656.5 ns |      96 B |

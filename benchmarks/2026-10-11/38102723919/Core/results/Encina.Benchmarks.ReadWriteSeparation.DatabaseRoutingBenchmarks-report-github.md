```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean          | Error       | StdDev      | Median        | Allocated |
|--------------------------------------- |--------------:|------------:|------------:|--------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,271.9038 ns | 226.1879 ns | 309.6086 ns | 2,297.7500 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    47.3800 ns |  46.2513 ns |  61.7441 ns |    25.0000 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,547.2308 ns | 451.6206 ns | 618.1834 ns | 1,570.0000 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |     0.2000 ns |   0.7491 ns |   1.0000 ns |     0.0000 ns |         - |
| &#39;Read HasIntent&#39;                       |    28.0000 ns |  17.8707 ns |  25.0523 ns |    21.0000 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    31.3125 ns |  11.6440 ns |  15.1405 ns |    30.0000 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    60.3462 ns |  28.0238 ns |  38.3593 ns |    55.0000 ns |         - |
| DatabaseRoutingScope.ForWrite()        |   863.9259 ns | 141.0560 ns | 197.7409 ns |   832.0000 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |   637.6154 ns |  81.4601 ns | 111.5036 ns |   606.0000 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   441.5769 ns |  97.0498 ns | 132.8428 ns |   420.5000 ns |      96 B |

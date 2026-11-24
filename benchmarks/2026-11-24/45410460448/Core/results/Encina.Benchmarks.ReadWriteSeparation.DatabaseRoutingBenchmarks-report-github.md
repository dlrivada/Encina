```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,352.33 ns | 3,750.86 ns | 205.60 ns | 3,466.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    86.17 ns |   459.12 ns |  25.17 ns |    89.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,186.17 ns | 2,381.02 ns | 130.51 ns | 3,199.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   147.83 ns | 1,381.39 ns |  75.72 ns |   114.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   201.83 ns | 2,009.32 ns | 110.14 ns |   215.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   337.67 ns | 1,068.98 ns |  58.59 ns |   361.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   130.33 ns |   321.39 ns |  17.62 ns |   140.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,835.00 ns | 2,554.12 ns | 140.00 ns | 2,895.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,761.00 ns | 4,205.45 ns | 230.51 ns | 2,665.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,809.00 ns | 4,675.41 ns | 256.28 ns | 1,832.00 ns |      96 B |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,181.3 ns |  8,955.4 ns |   490.88 ns | 3,265.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   131.2 ns |  2,588.6 ns |   141.89 ns |   104.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,924.7 ns | 10,509.7 ns |   576.07 ns | 2,895.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   154.0 ns |    906.1 ns |    49.67 ns |   131.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   161.7 ns |  1,369.3 ns |    75.06 ns |   165.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   120.3 ns |  2,698.0 ns |   147.89 ns |    40.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   515.5 ns | 13,777.5 ns |   755.19 ns |    84.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,764.0 ns |  1,094.6 ns |    60.00 ns | 2,764.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,924.7 ns | 18,759.5 ns | 1,028.27 ns | 2,855.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   798.0 ns | 12,128.9 ns |   664.83 ns |   481.00 ns |      96 B |

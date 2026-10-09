```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,822.7 ns | 28,279.3 ns | 1,550.09 ns | 4,525.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   124.7 ns |  1,742.3 ns |    95.50 ns |   125.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,165.2 ns | 19,251.1 ns | 1,055.22 ns | 2,710.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   186.2 ns |  2,202.6 ns |   120.73 ns |   120.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   658.5 ns | 18,299.9 ns | 1,003.08 ns |   100.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   570.0 ns | 11,457.2 ns |   628.01 ns |   384.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   609.8 ns | 17,489.8 ns |   958.68 ns |    79.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,824.5 ns | 25,579.2 ns | 1,402.08 ns | 2,303.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,191.3 ns | 19,914.4 ns | 1,091.58 ns | 2,747.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,732.2 ns | 17,110.1 ns |   937.86 ns | 1,279.50 ns |      96 B |

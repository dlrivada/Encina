```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,234.7 ns |  1,004.8 ns |    55.08 ns | 4,208.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   197.0 ns |    426.7 ns |    23.39 ns |   210.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,877.5 ns | 40,179.9 ns | 2,202.40 ns | 3,671.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   238.5 ns |    927.9 ns |    50.86 ns |   245.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   235.5 ns |    795.2 ns |    43.59 ns |   215.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   267.7 ns |  1,174.5 ns |    64.38 ns |   231.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   216.0 ns |    649.4 ns |    35.59 ns |   219.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,612.7 ns |    459.1 ns |    25.17 ns | 3,616.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,791.0 ns |  2,696.6 ns |   147.81 ns | 3,738.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,389.8 ns |  2,229.9 ns |   122.23 ns | 2,329.5 ns |      96 B |

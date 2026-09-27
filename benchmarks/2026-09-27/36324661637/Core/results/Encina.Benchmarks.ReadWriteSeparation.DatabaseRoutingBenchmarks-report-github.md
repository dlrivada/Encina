```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,130.8 ns | 14,713.9 ns | 806.52 ns | 1,857.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   102.0 ns |  1,826.8 ns | 100.13 ns |    96.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,772.7 ns | 13,342.2 ns | 731.33 ns | 1,532.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   590.7 ns | 18,033.4 ns | 988.47 ns |    30.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   504.0 ns | 11,538.2 ns | 632.45 ns |   190.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   112.0 ns |  1,169.2 ns |  64.09 ns |    86.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   846.3 ns | 12,344.7 ns | 676.65 ns | 1,237.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,588.0 ns | 16,971.3 ns | 930.25 ns | 1,441.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,787.3 ns | 12,479.0 ns | 684.01 ns | 2,634.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   848.8 ns | 10,771.1 ns | 590.40 ns |   675.50 ns |      96 B |

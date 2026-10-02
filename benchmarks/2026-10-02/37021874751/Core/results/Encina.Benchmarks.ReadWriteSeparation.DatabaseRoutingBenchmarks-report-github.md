```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,344.2 ns |  5,606.5 ns |   307.31 ns | 4,223.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      | 1,105.8 ns | 17,444.9 ns |   956.21 ns | 1,448.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 5,418.8 ns | 29,459.0 ns | 1,614.75 ns | 5,320.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   951.7 ns | 15,442.8 ns |   846.47 ns |   716.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   652.3 ns | 15,386.4 ns |   843.38 ns |   182.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   322.0 ns |  2,318.4 ns |   127.08 ns |   365.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   180.8 ns |  1,798.7 ns |    98.59 ns |   129.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,549.0 ns | 36,762.2 ns | 2,015.06 ns | 3,565.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,385.3 ns | 40,834.0 ns | 2,238.25 ns | 4,780.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,252.8 ns | 21,513.6 ns | 1,179.23 ns | 2,685.5 ns |      96 B |

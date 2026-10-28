```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 12,106.2 ns | 248,154.0 ns | 13,602.16 ns | 4,297.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    202.7 ns |     639.9 ns |     35.08 ns |   200.0 ns |         - |
| DatabaseRoutingScope.ForRead()         |  3,672.2 ns |     834.1 ns |     45.72 ns | 3,681.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    237.0 ns |   1,302.5 ns |     71.39 ns |   250.0 ns |         - |
| &#39;Read HasIntent&#39;                       |    220.0 ns |     482.7 ns |     26.46 ns |   210.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    261.0 ns |     482.7 ns |     26.46 ns |   251.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    331.0 ns |   1,139.3 ns |     62.45 ns |   311.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  4,408.5 ns |   3,288.9 ns |    180.28 ns | 4,358.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  3,549.0 ns |   4,530.1 ns |    248.31 ns | 3,506.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,301.7 ns |   1,530.0 ns |     83.86 ns | 2,345.0 ns |      96 B |

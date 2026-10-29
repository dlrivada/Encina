```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |-----------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,207.5 ns |   1,015.8 ns |     55.68 ns | 4,217.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   348.0 ns |   3,715.1 ns |    203.63 ns |   251.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,108.0 ns |   2,371.7 ns |    130.00 ns | 4,108.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   209.3 ns |     918.2 ns |     50.33 ns |   216.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 9,003.7 ns | 277,539.8 ns | 15,212.89 ns |   230.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   250.7 ns |     173.4 ns |      9.50 ns |   251.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   240.5 ns |     948.0 ns |     51.96 ns |   210.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,609.5 ns |   3,694.8 ns |    202.53 ns | 3,576.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,807.7 ns |   5,839.7 ns |    320.09 ns | 3,918.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,294.0 ns |   1,315.6 ns |     72.11 ns | 2,314.0 ns |      96 B |

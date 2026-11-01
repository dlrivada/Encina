```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,358.2 ns | 17.85 ns | 26.17 ns |  1.00 |    0.03 |    2 | 0.0324 | 0.0305 |     600 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   708.7 ns | 17.16 ns | 25.69 ns |  0.52 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   715.5 ns | 14.61 ns | 21.87 ns |  0.53 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |

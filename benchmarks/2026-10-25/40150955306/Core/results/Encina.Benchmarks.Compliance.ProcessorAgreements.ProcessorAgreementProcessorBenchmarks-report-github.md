```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,352.2 ns | 16.47 ns | 24.14 ns |  1.00 |    0.02 |    2 | 0.0324 | 0.0305 |     600 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   685.8 ns | 11.14 ns | 16.32 ns |  0.51 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   671.1 ns |  8.39 ns | 11.20 ns |  0.50 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |

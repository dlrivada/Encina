```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,606.6 ns |  62.19 ns |  3.41 ns |  1.00 |    0.00 |    2 | 0.0324 | 0.0305 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   867.5 ns | 525.15 ns | 28.79 ns |  0.54 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   824.4 ns | 285.17 ns | 15.63 ns |  0.51 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |

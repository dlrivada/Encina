```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,155.1 ns | 539.8 ns | 29.59 ns |  1.00 |    0.03 |    2 | 0.0324 | 0.0305 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   598.7 ns | 314.0 ns | 17.21 ns |  0.52 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   596.8 ns | 423.3 ns | 23.20 ns |  0.52 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |

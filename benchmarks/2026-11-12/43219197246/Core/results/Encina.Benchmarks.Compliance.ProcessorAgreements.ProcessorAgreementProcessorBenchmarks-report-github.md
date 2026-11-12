```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,198.0 ns | 236.4 ns | 12.96 ns |  1.00 |    0.01 |    2 | 0.0324 | 0.0305 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   644.6 ns | 563.8 ns | 30.90 ns |  0.54 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   618.2 ns | 189.9 ns | 10.41 ns |  0.52 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |

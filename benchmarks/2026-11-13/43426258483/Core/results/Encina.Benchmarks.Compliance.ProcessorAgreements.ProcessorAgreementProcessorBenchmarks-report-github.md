```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2,027.4 ns | 637.6 ns | 34.95 ns |  1.00 |    0.02 |    2 | 0.0305 | 0.0267 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,030.1 ns | 550.9 ns | 30.20 ns |  0.51 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   988.0 ns | 639.7 ns | 35.06 ns |  0.49 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |

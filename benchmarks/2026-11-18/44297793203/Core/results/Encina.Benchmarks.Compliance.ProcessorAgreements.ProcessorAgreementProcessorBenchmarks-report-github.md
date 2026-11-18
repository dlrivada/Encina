```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,406.7 ns | 150.8 ns |  8.27 ns |  1.00 |    0.01 |    2 | 0.0057 | 0.0038 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   944.0 ns | 920.8 ns | 50.47 ns |  0.67 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        0.77 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   948.4 ns | 973.7 ns | 53.37 ns |  0.67 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        0.77 |
